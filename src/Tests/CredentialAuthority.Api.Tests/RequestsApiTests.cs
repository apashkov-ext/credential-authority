using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CredentialAuthority.Api.Tests;

public sealed class RequestsApiTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "code-review-task-api-tests", Guid.NewGuid().ToString("N"));
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new ApiFactory(Path.Combine(_directory, "api.db"));
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        Directory.Delete(_directory, recursive: true);
    }

    private Task<HttpResponseMessage> Create(string owner = "Ivan", DateTimeOffset? until = null) =>
        _client.PostAsJsonAsync("/requests", new
        {
            ownerName = owner,
            validUntil = until ?? _factory.Time.GetUtcNow().AddHours(1)
        });

    [Fact]
    public async Task FullWorkflow_PublishesMatchingDocumentAndOmitsInapplicableFields()
    {
        var response = await Create("Иван.Petrov");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"/requests/{id:D}", response.Headers.Location!.OriginalString);

        var pending = await _client.GetFromJsonAsync<JsonElement>($"/requests/{id:D}");
        Assert.Equal("Pending", pending.GetProperty("status").GetString());
        Assert.Equal("Иван.Petrov", pending.GetProperty("ownerName").GetString());
        Assert.Equal("Demo Authority", pending.GetProperty("issuer").GetString());
        Assert.False(pending.TryGetProperty("credentialUrl", out _));
        Assert.False(pending.TryGetProperty("rejectionReason", out _));

        await _factory.ProcessPendingRequests();
        var issued = await _client.GetFromJsonAsync<JsonElement>($"/requests/{id:D}");
        Assert.Equal("Issued", issued.GetProperty("status").GetString());
        Assert.False(issued.TryGetProperty("rejectionReason", out _));
        var url = issued.GetProperty("credentialUrl").GetString()!;
        var document = await _client.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal("Иван.Petrov", document.GetProperty("ownerName").GetString());
        Assert.Equal("Demo Authority", document.GetProperty("issuer").GetString());
        Assert.Equal($"/credentials/{document.GetProperty("serialNumber").GetGuid():D}", url);
        Assert.Equal(_factory.Time.GetUtcNow(), document.GetProperty("issuedAt").GetDateTimeOffset());
        Assert.Equal(pending.GetProperty("validUntil").GetString(), document.GetProperty("validUntil").GetString());
        Assert.EndsWith("Z", document.GetProperty("issuedAt").GetString());
        Assert.EndsWith("Z", document.GetProperty("validUntil").GetString());

        _factory.Time.Advance(TimeSpan.FromDays(1));
        await _factory.ProcessPendingRequests();
        var expired = await _client.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal(document.GetRawText(), expired.GetRawText());
        Assert.Equal(HttpStatusCode.Conflict, (await Create("ИВАН.PETROV")).StatusCode);
    }

    [Theory]
    [InlineData("Ivan", "IVAN")]
    [InlineData("Иван", "ИВАН")]
    public async Task PendingOwner_WithDifferentCase_ReturnsConflict(string name, string repeated)
    {
        Assert.Equal(HttpStatusCode.Accepted, (await Create(name)).StatusCode);
        var response = await Create(repeated);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task ConcurrentRequests_ReturnOneAcceptedAndRemainingConflicts()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(index =>
            Create(index % 2 == 0 ? "Иван" : "ИВАН")));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Accepted));
        Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task ExpiryBeforeProcessing_RejectsRequestAndAllowsNewApplication()
    {
        var response = await Create(until: _factory.Time.GetUtcNow().AddSeconds(5));
        var originalUrl = response.Headers.Location!.OriginalString;
        _factory.Time.Advance(TimeSpan.FromSeconds(5));

        await _factory.ProcessPendingRequests();

        var rejected = await _client.GetFromJsonAsync<JsonElement>(originalUrl);
        Assert.Equal("Rejected", rejected.GetProperty("status").GetString());
        Assert.Equal("ValidityExpired", rejected.GetProperty("rejectionReason").GetString());
        Assert.False(rejected.TryGetProperty("credentialUrl", out _));
        var retried = await Create("IVAN");
        Assert.Equal(HttpStatusCode.Accepted, retried.StatusCode);
        Assert.NotEqual(originalUrl, retried.Headers.Location!.OriginalString);
        Assert.Equal(rejected.GetRawText(),
            (await _client.GetFromJsonAsync<JsonElement>(originalUrl)).GetRawText());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"ownerName\":\"Ivan\"}")]
    [InlineData("{\"ownerName\":\"Ivan\",\"validUntil\":\"2030-01-02T00:00:00\"}")]
    [InlineData("{\"ownerName\":\"Ivan\",\"validUntil\":\"invalid\"}")]
    [InlineData("{\"ownerName\":\"Ivan\",\"validUntil\":123}")]
    [InlineData("{\"ownerName\":123,\"validUntil\":\"2030-01-02T00:00:00Z\"}")]
    public async Task InvalidBody_ReturnsProblemDetailsAndDoesNotReserveOwner(string body)
    {
        var response = await _client.PostAsync("/requests", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.Accepted, (await Create()).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Abcdefghijklmnopqrstu")]
    [InlineData("1Ivan")]
    [InlineData(" Ivan")]
    [InlineData("Ivan ")]
    [InlineData("Алёна")]
    [InlineData("Ivan\\Petrov")]
    public async Task InvalidName_ReturnsBadRequest(string name)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Create(name)).StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task DeadlineNotInFuture_ReturnsBadRequest(int seconds)
    {
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Create(until: _factory.Time.GetUtcNow().AddSeconds(seconds))).StatusCode);
    }

    [Fact]
    public async Task ExplicitOffset_IsConvertedToUtc()
    {
        var expected = _factory.Time.GetUtcNow().AddHours(1);
        var response = await Create(until: expected.ToOffset(TimeSpan.FromHours(3)));
        var card = await _client.GetFromJsonAsync<JsonElement>(response.Headers.Location);

        Assert.Equal(expected, card.GetProperty("validUntil").GetDateTimeOffset());
        Assert.EndsWith("Z", card.GetProperty("validUntil").GetString());
    }

    [Theory]
    [InlineData("/requests/not-a-guid")]
    [InlineData("/requests/00000000-0000-0000-0000-000000000000")]
    [InlineData("/credentials/not-a-guid")]
    [InlineData("/credentials/00000000-0000-0000-0000-000000000000")]
    public async Task UnknownIdentifier_ReturnsProblemDetails(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }
}
