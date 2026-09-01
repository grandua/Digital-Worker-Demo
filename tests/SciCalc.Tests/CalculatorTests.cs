using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class CalculatorTests
{
    [Theory]
    [InlineData("12+3", "12+3")]
    [InlineData("5*3", "5×3")]
    [InlineData("8-2", "8−2")]
    [InlineData("9/3", "9÷3")]
    [InlineData("2^3", "2^3")]
    [InlineData("(1+2)", "(1+2)")]
    [InlineData("50%", "50%")]
    [InlineData("π*2", "π×2")]
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

    [Fact]
    public void DigitPressesMergeIntoSingleNumberToken()
    {
        Calculator calculator = new Calculator().PressAll("12");

        Assert.Equal(new Token[] { Token.Number(12) }, calculator.Buffer.Tokens);
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
        Assert.Equal(
            new Token[] { Token.Number(2), Token.Operator(OperatorKind.Add), Token.Number(3) },
            entry.Tokens);
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
    public void AnsKeyIsIgnoredBeforeFirstAnswer()
    {
        Calculator calculator = new Calculator();
        calculator.Press(InputKey.Ans);

        Assert.Empty(calculator.Buffer.Tokens);
    }

    [Fact]
    public void DivisionByZeroLocksCalculatorUntilAllClear()
    {
        Calculator calculator = new Calculator().PressAll("1/0=");

        Assert.True(calculator.Locked);
        Assert.Equal(CalcError.DivisionByZero, calculator.ActiveError);
        calculator.PressAll("2+3");
        Assert.Equal("1÷0", calculator.Buffer.Text());
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

    private static void AssertPreview(Calculator calculator, double expected)
    {
        Assert.NotNull(calculator.Preview);
        TestTokens.AssertClose(CalculationResult.Ok(calculator.Preview.Value), expected);
    }
}
