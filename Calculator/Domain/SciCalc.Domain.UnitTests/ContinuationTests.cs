using SciCalc.Domain;
using Xunit;

namespace SciCalc.Domain.UnitTests;

public class ContinuationTests
{
    [Theory]
    [InlineData(InputKey.Add, "5+")]
    [InlineData(InputKey.Subtract, "5-")]
    [InlineData(InputKey.Multiply, "5*")]
    [InlineData(InputKey.Divide, "5/")]
    [InlineData(InputKey.Power, "5^")]
    [InlineData(InputKey.Modulo, "5mod")]
    public void OperatorAfterEqualsContinuesFromAnswer(InputKey key, string expected)
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        calculator.Press(key);

        Assert.Equal(expected, calculator.Buffer.Text());
        Assert.Null(calculator.Preview);
    }

    [Fact]
    public void ContinuedExpressionEvaluatesResultPlusOperand()
    {
        Calculator calculator = new Calculator().PressAll("2+3=+4=");

        Assert.Equal(9.0, calculator.LastAnswer);
        Assert.Equal(9.0, calculator.Preview);
        Assert.False(calculator.Locked);
    }

    [Fact]
    public void ContinuedExpressionEvaluatesResultTimesOperand()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");
        calculator.Press(InputKey.Multiply);
        calculator.PressAll("2=");

        Assert.Equal(10.0, calculator.LastAnswer);
        Assert.Equal(10.0, calculator.Preview);
    }

    [Fact]
    public void SquareAfterEqualsWrapsAnswer()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        calculator.Press(InputKey.Square);

        Assert.Equal("sqr(5)", calculator.Buffer.Text());
        AssertPreview(calculator, 25);
    }

    [Fact]
    public void SqrtAfterEqualsWrapsAnswer()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        calculator.Press(InputKey.Sqrt);

        Assert.Equal("sqrt(5)", calculator.Buffer.Text());
        AssertPreview(calculator, 2.23606797749979);
    }

    [Fact]
    public void PrefixFunctionAfterEqualsWrapsAnswer()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        calculator.Press(InputKey.Sin);

        Assert.Equal("sin(5)", calculator.Buffer.Text());
        AssertPreview(calculator, Math.Sin(5));
        Assert.False(calculator.Locked);
        Assert.Null(calculator.ActiveError);
    }

    [Theory]
    [InlineData(InputKey.Cos, "cos(1)", 0.5403023058681398)]
    [InlineData(InputKey.Tan, "tan(1)", 1.5574077246549023)]
    [InlineData(InputKey.Asin, "asin(1)", 1.5707963267948966)]
    [InlineData(InputKey.Acos, "acos(1)", 0.0)]
    [InlineData(InputKey.Atan, "atan(1)", 0.7853981633974483)]
    [InlineData(InputKey.Sinh, "sinh(1)", 1.1752011936438014)]
    [InlineData(InputKey.Cosh, "cosh(1)", 1.5430806348152437)]
    [InlineData(InputKey.Tanh, "tanh(1)", 0.7615941559557649)]
    [InlineData(InputKey.Log10, "log(1)", 0.0)]
    [InlineData(InputKey.Ln, "ln(1)", 0.0)]
    public void PrefixFunctionAfterEqualsProducesCompleteCalculatedExpression(InputKey key, string expected, double expectedPreview)
    {
        Calculator calculator = new Calculator().PressAll("0.5+0.5=");

        calculator.Press(key);

        Assert.Equal(expected, calculator.Buffer.Text());
        AssertPreview(calculator, expectedPreview);
        Assert.False(calculator.Locked);
        Assert.Null(calculator.ActiveError);
    }

    [Fact]
    public void PrefixFunctionAfterEqualsThenEqualsCalculatesResult()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        calculator.Press(InputKey.Sin);
        calculator.Press(InputKey.Eq);

        Assert.Equal(Math.Sin(5), calculator.LastAnswer!.Value, precision: 10);
        Assert.Equal(Math.Sin(5), calculator.Preview!.Value, precision: 10);
        Assert.False(calculator.Locked);
    }

    [Fact]
    public void PrefixFunctionSeededFromAnswerCanBeExtended()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        calculator.Press(InputKey.Sin);
        calculator.PressAll("+1");

        Assert.Equal("sin(5)+1", calculator.Buffer.Text());
        AssertPreview(calculator, Math.Sin(5) + 1);
        Assert.False(calculator.Locked);
    }

    [Fact]
    public void PercentAfterEqualsContinuesFromAnswer()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        calculator.Press(InputKey.Percent);

        Assert.Equal("5%", calculator.Buffer.Text());
        AssertPreview(calculator, 0.05);
    }

    [Theory]
    [InlineData("7", "7")]
    [InlineData(".", "0.")]
    [InlineData("π", "π")]
    [InlineData("(", "(")]
    [InlineData("(2+3", "(2+3")]
    public void ValueKeysAfterEqualsStartFreshExpression(string keys, string expected)
    {
        Calculator calculator = new Calculator().PressAll("2+3=");

        calculator.PressAll(keys);

        Assert.Equal(expected, calculator.Buffer.Text());
    }

    [Fact]
    public void OperatorAfterAllClearDoesNotSeedStaleAnswer()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");
        calculator.Press(InputKey.AllClear);

        calculator.Press(InputKey.Add);

        Assert.Equal("+", calculator.Buffer.Text());
    }

    [Fact]
    public void OperatorAfterHistoryRestoreDoesNotSeedAnswer()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");
        calculator.RestoreHistory(calculator.History[0]);

        calculator.Press(InputKey.Multiply);

        Assert.Equal("2+3*", calculator.Buffer.Text());
    }

    [Fact]
    public void AnswerSeedingWorksWithMemoryRecall()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");
        calculator.Press(InputKey.StoreM1);
        calculator.Press(InputKey.AllClear);
        calculator.Press(InputKey.RecallM1);

        calculator.Press(InputKey.Multiply);

        Assert.Equal("5*", calculator.Buffer.Text());
        Assert.Null(calculator.Preview);
    }

    [Fact]
    public void MemoryRecallAfterEqualsClearsPendingAnswerSeed()
    {
        Calculator calculator = new Calculator().PressAll("7");
        calculator.Press(InputKey.StoreM1);
        calculator.Press(InputKey.AllClear);
        calculator.PressAll("2+3=");
        calculator.Press(InputKey.RecallM1);
        calculator.Press(InputKey.Delete);

        calculator.Press(InputKey.Add);

        Assert.Equal("+", calculator.Buffer.Text());
    }

    private static void AssertPreview(Calculator calculator, double expected) =>
        Assert.Equal(expected, calculator.Preview!.Value, precision: 10);
}
