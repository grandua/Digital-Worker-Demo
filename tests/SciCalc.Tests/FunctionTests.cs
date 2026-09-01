using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class FunctionTests
{
    [Theory]
    [InlineData(AngleMode.Degrees, "sin(90)", 1)]
    [InlineData(AngleMode.Degrees, "sin(30)", 0.5)]
    [InlineData(AngleMode.Radians, "sin(π/6)", 0.5)]
    [InlineData(AngleMode.Radians, "sin(π/2)", 1)]
    [InlineData(AngleMode.Radians, "cos(0)", 1)]
    [InlineData(AngleMode.Radians, "cos(sin(0))", 1)]
    [InlineData(AngleMode.Radians, "cos(π)", -1)]
    [InlineData(AngleMode.Degrees, "tan(45)", 1)]
    [InlineData(AngleMode.Radians, "tan(0)", 0)]
    [InlineData(AngleMode.Degrees, "asin(0.5)", 30)]
    [InlineData(AngleMode.Degrees, "asin(1)", 90)]
    [InlineData(AngleMode.Radians, "asin(1)", Math.PI / 2)]
    [InlineData(AngleMode.Degrees, "acos(0)", 90)]
    [InlineData(AngleMode.Degrees, "acos(1)", 0)]
    [InlineData(AngleMode.Radians, "acos(1)", 0)]
    [InlineData(AngleMode.Radians, "asin(-1)", -Math.PI / 2)]
    [InlineData(AngleMode.Degrees, "asin(-1)", -90)]
    [InlineData(AngleMode.Degrees, "acos(-1)", 180)]
    [InlineData(AngleMode.Degrees, "atan(1)", 45)]
    [InlineData(AngleMode.Radians, "atan(1)", Math.PI / 4)]
    public void EvaluatesTrigRespectingAngleMode(AngleMode mode, string source, double expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(mode);

        TestTokens.AssertClose(result, expected);
    }

    [Theory]
    [InlineData("sinh(0)", 0)]
    [InlineData("cosh(0)", 1)]
    [InlineData("tanh(0)", 0)]
    [InlineData("sinh(1)", 1.1752011936438014)]
    [InlineData("cosh(1)", 1.5430806348152437)]
    [InlineData("tanh(1)", 0.7615941559557649)]
    [InlineData("log(100)", 2)]
    [InlineData("log(1)", 0)]
    [InlineData("ln(e)", 1)]
    [InlineData("ln(1)", 0)]
    [InlineData("exp(0)", 1)]
    [InlineData("exp(1)", 2.718281828459045)]
    [InlineData("tenpow(0)", 1)]
    [InlineData("tenpow(2)", 100)]
    [InlineData("e^1", 2.718281828459045)]
    [InlineData("10^2", 100)]
    [InlineData("2^10", 1024)]
    [InlineData("sqr(2)", 4)]
    [InlineData("sqr(3)", 9)]
    [InlineData("sqr(-3)", 9)]
    [InlineData("sqr(0)", 0)]
    [InlineData("cube(2)", 8)]
    [InlineData("cube(-2)", -8)]
    [InlineData("√(9)", 3)]
    [InlineData("√(0)", 0)]
    [InlineData("√(1)", 1)]
    [InlineData("∛(27)", 3)]
    [InlineData("∛(-8)", -2)]
    [InlineData("∛(0)", 0)]
    [InlineData("abs(-5)", 5)]
    [InlineData("abs(0)", 0)]
    [InlineData("abs(5)", 5)]
    [InlineData("recip(4)", 0.25)]
    [InlineData("recip(-2)", -0.5)]
    [InlineData("0!", 1)]
    [InlineData("1!", 1)]
    [InlineData("5!", 120)]
    public void EvaluatesFunctionsUnaffectedByAngleMode(string source, double expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(AngleMode.Degrees);

        TestTokens.AssertClose(result, expected);
    }

    [Theory]
    [InlineData("√(-1)", CalcError.NegativeSqrt)]
    [InlineData("√(-0.001)", CalcError.NegativeSqrt)]
    [InlineData("log(0)", CalcError.NonPositiveLog)]
    [InlineData("log(-1)", CalcError.NonPositiveLog)]
    [InlineData("ln(0)", CalcError.NonPositiveLog)]
    [InlineData("ln(-2)", CalcError.NonPositiveLog)]
    [InlineData("asin(2)", CalcError.AsinAcosOutOfRange)]
    [InlineData("asin(1.001)", CalcError.AsinAcosOutOfRange)]
    [InlineData("asin(-1.001)", CalcError.AsinAcosOutOfRange)]
    [InlineData("acos(1.001)", CalcError.AsinAcosOutOfRange)]
    [InlineData("acos(-2)", CalcError.AsinAcosOutOfRange)]
    [InlineData("(-1)!", CalcError.InvalidFactorial)]
    [InlineData("(-2)!", CalcError.InvalidFactorial)]
    [InlineData("0.5!", CalcError.InvalidFactorial)]
    [InlineData("171!", CalcError.Overflow)]
    [InlineData("200!", CalcError.Overflow)]
    [InlineData("exp(1000)", CalcError.Overflow)]
    [InlineData("tenpow(1000)", CalcError.Overflow)]
    [InlineData("recip(0)", CalcError.DivisionByZero)]
    [InlineData("sin", CalcError.Malformed)]
    [InlineData("sin(1", CalcError.Malformed)]
    public void ReportsFunctionErrors(string source, CalcError expected)
    {
        MathExpression expression = TestTokens.Parse(source);

        CalculationResult result = expression.Evaluate(AngleMode.Degrees);

        Assert.Equal(expected, result.Error);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(170)]
    public void EvaluatesFactorialWithinDoubleRange(int operand)
    {
        MathExpression expression = TestTokens.Parse($"{operand}!");

        CalculationResult result = expression.Evaluate(AngleMode.Degrees);

        Assert.False(result.HasError, $"unexpected error {result.Error}");
        if (operand == 170) Assert.True(result.Value! > 1e305);
    }

    [Theory]
    [InlineData(AngleMode.Degrees)]
    [InlineData(AngleMode.Radians)]
    public void EvaluatesHyperbolicIndependentlyOfAngleMode(AngleMode mode)
    {
        MathExpression expression = TestTokens.Parse("sinh(1)");

        CalculationResult result = expression.Evaluate(mode);

        TestTokens.AssertClose(result, 1.1752011936438014);
    }
}
