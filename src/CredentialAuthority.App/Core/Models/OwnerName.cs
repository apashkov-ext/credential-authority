using System.Diagnostics.CodeAnalysis;

namespace CredentialAuthority.App.Core.Models;

public sealed record OwnerName
{
    public string Value { get; }
    public string Key { get; }

    private OwnerName(string value)
    {
        Value = value;
        Key = value.ToUpperInvariant();
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out OwnerName? name)
    {
        name = null;
        if (value is null || value.Length is < 1 or > 20 || !IsLetter(value[0]))
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!IsLetter(character) && character is not (>= '0' and <= '9' or '.' or '-' or '_' or '@'))
            {
                return false;
            }
        }

        name = new OwnerName(value);
        return true;
    }

    public static OwnerName Create(string value)
    {
        return TryCreate(value, out var name)
            ? name
            : throw new ArgumentException("Invalid owner name.", nameof(value));
    }

    public bool Equals(OwnerName? other) => other is not null && Key == other.Key;

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Key);

    public override string ToString() => Value;

    private static bool IsLetter(char value) =>
        value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= 'а' and <= 'я' or >= 'А' and <= 'Я';
}
