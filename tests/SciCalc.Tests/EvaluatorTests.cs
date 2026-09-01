using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class EvaluatorTests
{
    [Theory]
    [InlineData("10-3-2", 5)]
    [InlineData("16/4/2", 2)]
    [InlineData("2+3-4", 1)]
    [InlineData("1+2*3-4/2", 5)]
    [InlineData("-5", -5)]
    [InlineData("2*-3", -6)]
    [InlineData("-(-3)", 3)]
    [InlineData("-(2+3)", -5)]
    [InlineData("2^-3", 0.125)]
    [InlineData("(-2)^2", 4)]
    [InlineData("10 mod 3", 1)]
    [InlineData("-10 mod 3", -1)]
    [InlineData("7 mod 2 * 3", 3)]
    [InlineData("0^0", 1)]
    [InlineData("2*(1.5+0.5)", 4)]
    [InlineData("π", Math.PI)]
    [InlineData("e", Math.E)]
    [InlineData("π*2", 2 * Math.PI)]
    [InlineData("π+e", Math.PI + Math.E)]
    [InlineData("e^2", Math.E * Math.E)]
    public void EvaluatesArithmeticSemantics(string source, double expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(new EvaluationContext());

        AssertClose(result, expected);
    }

    [Theory]
    [InlineData("1/0", CalcError.DivisionByZero)]
    [InlineData("5/(3-3)", CalcError.DivisionByZero)]
    [InlineData("1/(0)", CalcError.DivisionByZero)]
    [InlineData("10 mod 0", CalcError.DivisionByZero)]
    [InlineData("2^10000", CalcError.Overflow)]
    [InlineData("10^1000", CalcError.Overflow)]
    [InlineData("e^1000", CalcError.Overflow)]
    [InlineData("9^9^9", CalcError.Overflow)]
    public void ReportsDomainErrors(string source, CalcError expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(new EvaluationContext());

        Assert.Equal(expected, result.Error);
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
