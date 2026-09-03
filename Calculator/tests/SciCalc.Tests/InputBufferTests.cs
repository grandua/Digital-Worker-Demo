using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class InputBufferTests
{
    [Fact]
    public void LiteralOverflowFlagsAtDigitBoundary()
    {
        InputBuffer buffer = new();

        AddDigits(buffer, 308);
        Assert.False(buffer.HasLiteralOverflow);

        buffer.Add(Token.Digit(9));
        Assert.True(buffer.HasLiteralOverflow);
    }

    [Fact]
    public void OverflowFlagClearsAfterRemoveLastToken()
    {
        InputBuffer buffer = new();
        AddDigits(buffer, 309);
        Assert.True(buffer.HasLiteralOverflow);

        buffer.RemoveLastToken();

        Assert.False(buffer.HasLiteralOverflow);
    }

    private static void AddDigits(InputBuffer buffer, int count)
    {
        foreach (int digit in Enumerable.Repeat(9, count))
            buffer.Add(Token.Digit(digit));
    }
}