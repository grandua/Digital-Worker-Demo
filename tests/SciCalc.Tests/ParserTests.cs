using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class ParserTests
{
    [Theory]
    [InlineData("2+3*4", 14)]
    [InlineData("(2+3)*4", 20)]
    [InlineData("(((1+2)))", 3)]
    [InlineData("2^3^2", 512)]
    [InlineData("-2^2", -4)]
    [InlineData("2*3^2", 18)]
    [InlineData("2^3*2", 16)]
    [InlineData("2*((3+4)*(5-3))", 28)]
    [InlineData("2*(3+4*(5-3))", 22)]
    [InlineData("((2+3))*((4))", 20)]
    public void ParsesWithPrecedenceAndNesting(string source, double expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(new EvaluationContext());

        AssertClose(result, expected);
    }

    [Theory]
    [InlineData("(1+2", CalcError.Malformed)]
    [InlineData("1+", CalcError.Malformed)]
    [InlineData(")(", CalcError.Malformed)]
    [InlineData(")", CalcError.Malformed)]
    [InlineData("++1", CalcError.Malformed)]
    [InlineData("2**3", CalcError.Malformed)]
    [InlineData("2 3", CalcError.Malformed)]
    [InlineData("2*", CalcError.Malformed)]
    [InlineData("", CalcError.Malformed)]
    public void ReportsMalformedForBrokenStructure(string source, CalcError expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(new EvaluationContext());

        Assert.Equal(expected, result.Error);
        Assert.Null(result.Value);
    }

    private static void AssertClose(CalculationResult result, double expected)
    {
        Assert.False(result.HasError, $"expected {expected} but got error {result.Error}");
        Assert.True(result.Value is not null, "expected a value");
        Assert.True(
            Math.Abs(result.Value!.Value - expected) < 1e-9,
            $"expected {expected} but got {result.Value!.Value}");
    }
}
