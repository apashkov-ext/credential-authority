using System.Globalization;
using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.App.Tests;

public sealed class OwnerNameTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("я")]
    [InlineData("Abcdefghijklmnopqrst")]
    [InlineData("Иван.Петров-1_@")]
    [InlineData("Z9._-@")]
    public void AllowedName_PreservesOriginalSpelling(string value)
    {
        Assert.True(OwnerName.TryCreate(value, out var name));
        Assert.Equal(value, name.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Abcdefghijklmnopqrstu")]
    [InlineData("1Ivan")]
    [InlineData(".Ivan")]
    [InlineData("-Ivan")]
    [InlineData("_Ivan")]
    [InlineData("@Ivan")]
    [InlineData(" Ivan")]
    [InlineData("Ivan ")]
    [InlineData("Ivan Petrov")]
    [InlineData("Алёна")]
    [InlineData("Ёж")]
    [InlineData("Ivan\\Petrov")]
    [InlineData("Ivan\n")]
    [InlineData("Ivan😀")]
    [InlineData("Iván")]
    public void InvalidName_IsRejected(string? value)
    {
        Assert.False(OwnerName.TryCreate(value, out var name));
        Assert.Null(name);
    }

    [Theory]
    [InlineData("Ivan", "IVAN")]
    [InlineData("Иван", "ИВАН")]
    public void Equality_IgnoresCaseIndependentlyOfCulture(string first, string second)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var left = OwnerName.Create(first);
            var right = OwnerName.Create(second);

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
            Assert.Equal(left.Key, right.Key);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void SimilarLatinAndCyrillicLetters_AreDifferentOwners()
    {
        Assert.NotEqual(OwnerName.Create("A"), OwnerName.Create("А"));
    }
}
