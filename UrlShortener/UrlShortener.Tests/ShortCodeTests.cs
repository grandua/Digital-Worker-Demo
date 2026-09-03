using UrlShortener.Domain;

namespace UrlShortener.Tests;

public class ShortCodeTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(9, "9")]
    [InlineData(10, "a")]
    [InlineData(35, "z")]
    [InlineData(36, "A")]
    [InlineData(61, "Z")]
    [InlineData(62, "10")]
    [InlineData(63, "11")]
    [InlineData(3843, "ZZ")]
    [InlineData(3844, "100")]
    public void FromId_encodes_boundaries(long id, string expected) => Assert.Equal(expected, ShortCode.FromId(id).Value);

    [Fact]
    public void FromId_max_value_is_valid() => Assert.Equal(ShortCode.FromId(long.MaxValue), ShortCode.Parse(ShortCode.FromId(long.MaxValue).Value));

    [Fact] public void FromId_negative_throws() => Assert.Throws<ArgumentOutOfRangeException>(() => ShortCode.FromId(-1));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("!")]
    [InlineData("abc!@#")]
    [InlineData("abc def")]
    public void Parse_rejects_invalid_values(string value) => Assert.Throws<ArgumentException>(() => ShortCode.Parse(value));

    [Fact] public void Parse_null_throws() => Assert.Throws<ArgumentException>(() => ShortCode.Parse(null!));

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(61)] [InlineData(62)] [InlineData(63)] [InlineData(3843)] [InlineData(3844)] [InlineData(100000)] [InlineData(long.MaxValue)]
    public void Round_trip(long id) => Assert.Equal(ShortCode.FromId(id), ShortCode.Parse(ShortCode.FromId(id).Value));

    [Fact]
    public void Equality_and_inequality_are_value_based()
    {
        var first = ShortCode.FromId(1); var same = ShortCode.Parse("1"); var other = ShortCode.FromId(2);
        Assert.True(first.Equals(same)); Assert.True(first == same); Assert.False(first != same); Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, other); Assert.True(first != other); Assert.NotEqual(first.GetHashCode(), other.GetHashCode());
    }
}
