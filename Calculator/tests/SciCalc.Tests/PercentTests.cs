using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class PercentTests
{
    [Theory]
    [InlineData("50%", 0.5)]
    [InlineData("10%", 0.1)]
    [InlineData("200%", 2)]
    [InlineData("(50%)", 0.5)]
    [InlineData("200+10%", 220)]
    [InlineData("200-10%", 180)]
    [InlineData("100+200+10%", 330)]
    [InlineData("200*10%", 20)]
    [InlineData("200/10%", 2000)]
    [InlineData("200+10%*2", 200.2)]
    public void EvaluatesPercentSemantics(string source, double expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(AngleMode.Degrees);

        TestTokens.AssertClose(result, expected);
    }

    [Theory]
    [InlineData("%", CalcError.Malformed)]
    [InlineData("200+%", CalcError.Malformed)]
    [InlineData("200/%", CalcError.Malformed)]
    [InlineData("(%)", CalcError.Malformed)]
    public void ReportsMalformedPercent(string source, CalcError expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(AngleMode.Radians);

        Assert.Equal(expected, result.Error);
        Assert.Null(result.Value);
    }
}
