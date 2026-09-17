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

        Assert.Equal("sin(5", calculator.Buffer.Text());
        Assert.Null(calculator.Preview);
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
