using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class CalculatorTests
{
    [Theory]
    [InlineData("12+3", "12+3")]
    [InlineData("5*3", "5*3")]
    [InlineData("8-2", "8-2")]
    [InlineData("9/3", "9/3")]
    [InlineData("2^3", "2^3")]
    [InlineData("(1+2)", "(1+2)")]
    [InlineData("50%", "50%")]
    [InlineData("π*2", "π*2")]
    public void RendersPressedKeysAsDisplayText(string keys, string expected)
    {
        Calculator calculator = new Calculator().PressAll(keys);

        Assert.Equal(expected, calculator.Buffer.Text());
    }

    [Fact]
    public void FunctionKeyInsertsFunctionAndOpenParen()
    {
        Calculator calculator = new Calculator();
        calculator.Press(InputKey.Sin);

        Assert.Equal("sin(", calculator.Buffer.Text());
    }

    [Fact]
    public void FactorialKeyInsertsPostfixOnly()
    {
        Calculator calculator = new Calculator().PressAll("5");
        calculator.Press(InputKey.Factorial);

        Assert.Equal("5!", calculator.Buffer.Text());
    }

    [Theory]
    [InlineData(InputKey.Square, "5", 25)]
    [InlineData(InputKey.Cube, "2", 8)]
    [InlineData(InputKey.Sqrt, "9", 3)]
    [InlineData(InputKey.Cbrt, "8", 2)]
    [InlineData(InputKey.Reciprocal, "4", 0.25)]
    [InlineData(InputKey.Exp, "2", 7.38905609893065)]
    [InlineData(InputKey.TenPow, "2", 100)]
    public void PostfixKeysEvaluateByWrappingBufferIntoFunctionCall(InputKey key, string digits, double expected)
    {
        Calculator calculator = new Calculator().PressAll(digits);

        calculator.Press(key);

        AssertPreview(calculator, expected);
    }

    [Fact]
    public void PostfixKeyWrapsWholeBufferExpression()
    {
        Calculator calculator = new Calculator().PressAll("2+3");

        calculator.Press(InputKey.Square);

        AssertPreview(calculator, 25);
    }

    [Fact]
    public void RepeatedPostfixKeyNestsFunctions()
    {
        Calculator calculator = new Calculator().PressAll("5");

        calculator.Press(InputKey.Square);
        calculator.Press(InputKey.Square);

        AssertPreview(calculator, 625);
    }

    [Fact]
    public void SquareKeyWrapsBufferInFunctionCall()
    {
        Calculator calculator = new Calculator().PressAll("5");

        calculator.Press(InputKey.Square);

        Assert.Equal("sqr(5)", calculator.Buffer.Text());
        AssertPreview(calculator, 25);
    }

    [Fact]
    public void DigitPressesMergeIntoSingleNumber()
    {
        Calculator calculator = new Calculator().PressAll("12");

        Assert.Equal("12", calculator.Buffer.Text());
        calculator.PressAll("3");
        Assert.Equal("123", calculator.Buffer.Text());
        AssertPreview(calculator, 123);
    }

    [Theory]
    [InlineData("2+3", 5)]
    [InlineData("2+3*4", 14)]
    public void PreviewShowsLiveResultWhileTyping(string keys, double expected)
    {
        Calculator calculator = new Calculator().PressAll(keys);

        AssertPreview(calculator, expected);
    }

    [Fact]
    public void IncompleteExpressionLeavesPreviewBlankWithoutLockout()
    {
        Calculator calculator = new Calculator().PressAll("2+");

        Assert.Null(calculator.Preview);
        Assert.False(calculator.Locked);
        Assert.Null(calculator.ActiveError);
    }

    [Fact]
    public void EqualsStoresAnswerHistoryAndClearsBuffer()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        Assert.Equal(5.0, calculator.LastAnswer);
        Assert.Equal(5.0, calculator.Preview);
        Assert.Empty(calculator.Buffer.Tokens);
        Assert.False(calculator.Locked);
        HistoryEntry entry = Assert.Single(calculator.History);
        Assert.Equal("2+3", entry.ExpressionText);
        Assert.Equal(5.0, entry.ResultValue);
    }

    [Fact]
    public void AnsKeyInsertsLastResultIntoBuffer()
    {
        Calculator calculator = new Calculator().PressAll("5=");
        calculator.PressAll("2+");
        calculator.Press(InputKey.Ans);

        AssertPreview(calculator, 7);
    }

    [Fact]
    public void AnsKeyInsertsZeroBeforeFirstAnswer()
    {
        Calculator calculator = new Calculator();
        calculator.Press(InputKey.Ans);

        Assert.Equal("0", calculator.Buffer.Text());
        Assert.Equal(0.0, calculator.Preview);
    }

    [Fact]
    public void DivisionByZeroLocksCalculatorUntilAllClear()
    {
        Calculator calculator = new Calculator().PressAll("1/0=");

        Assert.True(calculator.Locked);
        Assert.Equal(CalcError.DivisionByZero, calculator.ActiveError);
        calculator.PressAll("2+3");
        Assert.Equal("1/0", calculator.Buffer.Text());
        Assert.True(calculator.Locked);
        Assert.Equal(CalcError.DivisionByZero, calculator.ActiveError);
        calculator.Press(InputKey.AllClear);
        Assert.False(calculator.Locked);
        Assert.Null(calculator.ActiveError);
        Assert.Empty(calculator.Buffer.Tokens);
    }

    [Fact]
    public void EqualsOnEmptyBufferLocksWithMalformedError()
    {
        Calculator calculator = new Calculator();
        calculator.Press(InputKey.Eq);

        Assert.True(calculator.Locked);
        Assert.Equal(CalcError.Malformed, calculator.ActiveError);
    }

    [Fact]
    public void EqualsOnMalformedBufferLocksWithMalformedError()
    {
        Calculator calculator = new Calculator().PressAll("2+=");

        Assert.True(calculator.Locked);
        Assert.Equal(CalcError.Malformed, calculator.ActiveError);
    }

    [Theory]
    [InlineData("12+34", "12+")]
    [InlineData("12", "")]
    [InlineData("12+3", "12+")]
    public void DeleteRemovesLastToken(string keys, string expected)
    {
        Calculator calculator = new Calculator().PressAll(keys);

        calculator.Press(InputKey.Delete);

        Assert.Equal(expected, calculator.Buffer.Text());
    }

    [Fact]
    public void DeleteOnEmptyBufferIsNoOp()
    {
        Calculator calculator = new Calculator();

        calculator.Press(InputKey.Delete);

        Assert.Empty(calculator.Buffer.Tokens);
    }

    [Fact]
    public void AllClearEmptiesBufferAndPreview()
    {
        Calculator calculator = new Calculator().PressAll("2+3");

        calculator.Press(InputKey.AllClear);

        Assert.Empty(calculator.Buffer.Tokens);
        Assert.Null(calculator.Preview);
        Assert.False(calculator.Locked);
    }

    [Theory]
    [InlineData("1.5", "1.5")]
    [InlineData("1..5", "1.5")]
    [InlineData(".", "0.")]
    [InlineData("1.", "1.")]
    [InlineData("1+2.5", "1+2.5")]
    public void SecondDotInSameNumberIsIgnored(string keys, string expected)
    {
        Calculator calculator = new Calculator().PressAll(keys);

        Assert.Equal(expected, calculator.Buffer.Text());
    }

    [Fact]
    public void AngleToggleFlipsModeAndAffectsPreview()
    {
        Calculator calculator = new Calculator();
        calculator.Press(InputKey.DegRadToggle);

        Assert.Equal(AngleMode.Degrees, calculator.Mode);
        calculator.Press(InputKey.Sin);
        calculator.PressAll("90");
        calculator.Press(InputKey.CloseParen);
        AssertPreview(calculator, 1);
        calculator.Press(InputKey.DegRadToggle);
        Assert.Equal(AngleMode.Radians, calculator.Mode);
        TestTokens.AssertClose(CalculationResult.Ok(calculator.Preview!.Value), Math.Sin(90));
    }

    [Fact]
    public void AbsKeyWrapsBufferedOperand()
    {
        Calculator calculator = new Calculator().PressAll("5");

        calculator.Press(InputKey.Abs);

        Assert.Equal("abs(5)", calculator.Buffer.Text());
        AssertPreview(calculator, 5);
    }

    [Fact]
    public void DeleteOperatorThenTypingContinuesNumberEntry()
    {
        Calculator calculator = new Calculator().PressAll("12+3");

        calculator.Press(InputKey.Delete);
        calculator.Press(InputKey.Delete);
        calculator.PressAll("3");

        Assert.Equal("123", calculator.Buffer.Text());
        AssertPreview(calculator, 123);
    }

    [Fact]
    public void OversizedLiteralLocksWithOverflow()
    {
        Calculator calculator = new Calculator().PressAll(new string('9', 309));

        Assert.True(calculator.Locked);
        Assert.Equal(CalcError.Overflow, calculator.ActiveError);
        Assert.Null(calculator.Preview);
    }

    [Fact]
    public void OversizedLiteralRecoversOnlyViaAllClear()
    {
        Calculator calculator = new Calculator().PressAll(new string('9', 309));

        calculator.Press(InputKey.Digit5);

        Assert.True(calculator.Locked);
        calculator.Press(InputKey.AllClear);
        Assert.False(calculator.Locked);
        Assert.Null(calculator.ActiveError);
        Assert.Empty(calculator.Buffer.Tokens);
    }

    [Fact]
    public void Boundary308DigitLiteralStaysEditable()
    {
        Calculator calculator = new Calculator().PressAll(new string('9', 308));

        Assert.False(calculator.Locked);
        Assert.Null(calculator.ActiveError);
        Assert.NotNull(calculator.Preview);
    }

    [Theory]
    [InlineData("2", 2.0)]
    [InlineData("2+", null)]
    [InlineData("2+3", 5.0)]
    [InlineData("2+3*", null)]
    [InlineData("2+3*4", 14.0)]
    [InlineData("2+3*4=", 14.0)]
    [InlineData("(", null)]
    [InlineData("(2+3)", 5.0)]
    public void PreviewTracksEveryKeypress(string keys, double? expected)
    {
        Calculator calculator = new Calculator().PressAll(keys);

        if (expected is { } value)
            AssertPreview(calculator, value);
        else
            Assert.Null(calculator.Preview);
    }

    [Fact]
    public void AngleRoundTripDegRadDegKeepsTrigConsistent()
    {
        Calculator calculator = new Calculator();
        Assert.Equal(AngleMode.Radians, calculator.Mode);

        calculator.Press(InputKey.DegRadToggle);
        Assert.Equal(AngleMode.Degrees, calculator.Mode);
        AssertPreview(SinOf(calculator, "90"), 1);

        calculator.Press(InputKey.AllClear);
        calculator.Press(InputKey.DegRadToggle);
        Assert.Equal(AngleMode.Radians, calculator.Mode);
        calculator.Press(InputKey.Sin);
        calculator.Press(InputKey.Pi);
        calculator.PressAll("/2");
        calculator.Press(InputKey.CloseParen);
        AssertPreview(calculator, 1);

        calculator.Press(InputKey.AllClear);
        calculator.Press(InputKey.DegRadToggle);
        Assert.Equal(AngleMode.Degrees, calculator.Mode);
        AssertPreview(SinOf(calculator, "90"), 1);
    }

    private static Calculator SinOf(Calculator calculator, string angle)
    {
        calculator.Press(InputKey.Sin);
        calculator.PressAll(angle);
        calculator.Press(InputKey.CloseParen);
        return calculator;
    }

    private static void AssertPreview(Calculator calculator, double expected)
    {
        Assert.NotNull(calculator.Preview);
        TestTokens.AssertClose(CalculationResult.Ok(calculator.Preview.Value), expected);
    }
}
