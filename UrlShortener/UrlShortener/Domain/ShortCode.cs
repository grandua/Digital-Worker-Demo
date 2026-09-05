namespace UrlShortener.Domain;

public sealed class ShortCode : IEquatable<ShortCode>
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int CodeBase = 62;
    private const int BufferLength = 11;
    public string Value { get; }
    private ShortCode(string value) => Value = value;
    public static ShortCode FromId(long id)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id); Span<char> buffer = stackalloc char[BufferLength]; var index = buffer.Length;
        do { buffer[--index] = Alphabet[(int)(id % CodeBase)]; id /= CodeBase; } while (id > 0);
        return new ShortCode(new string(buffer[index..]));
    }
    public static ShortCode Parse(string value)
    {
        if (!IsValidCode(value)) throw new ArgumentException("Invalid short code.", nameof(value));
        return new ShortCode(value);
    }
    private static bool IsValidCode(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(c => Alphabet.Contains(c));
    public override string ToString() => Value;
    public bool Equals(ShortCode? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as ShortCode);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public static bool operator ==(ShortCode? left, ShortCode? right) => Equals(left, right);
    public static bool operator !=(ShortCode? left, ShortCode? right) => !Equals(left, right);
}
