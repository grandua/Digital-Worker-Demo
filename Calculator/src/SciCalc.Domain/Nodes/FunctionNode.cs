using static SciCalc.Domain.CalculationResult;
using static SciCalc.Domain.FunctionKind;

namespace SciCalc.Domain;

public sealed class FunctionNode(FunctionKind kind, Node argument) : Node
{
    public override CalculationResult EvaluateNode(AngleMode mode)
    {
        CalculationResult arg = argument.EvaluateNode(mode);
        return arg.HasError ? arg : Apply(arg.Value!.Value, mode);
    }

    private CalculationResult Apply(double x, AngleMode mode) => kind switch
    {
        Sin or Cos or Tan => Trigonometric(x, mode),
        Asin or Acos or Atan => Inverse(x, mode),
        Sinh or Cosh or Tanh => Hyperbolic(x),
        Log10 or Ln => Logarithm(x),
        Sqrt or Reciprocal or Factorial => Guarded(x),
        _ => Plain(x),
    };

    private CalculationResult Trigonometric(double x, AngleMode mode)
    {
        double input = mode == AngleMode.Degrees ? mode.ToRadians(x) : x;
        return Ok(kind switch
        {
            Sin => Math.Sin(input),
            Cos => Math.Cos(input),
            _ => Math.Tan(input),
        });
    }

    private CalculationResult Inverse(double x, AngleMode mode)
    {
        if (kind != Atan && Math.Abs(x) > 1) return Fail(CalcError.AsinAcosOutOfRange);
        double radians = kind switch
        {
            Asin => Math.Asin(x),
            Acos => Math.Acos(x),
            _ => Math.Atan(x),
        };
        return Ok(mode == AngleMode.Degrees ? mode.ToDegrees(radians) : radians);
    }

    private CalculationResult Hyperbolic(double x) => Ok(kind switch
    {
        Sinh => Math.Sinh(x),
        Cosh => Math.Cosh(x),
        _ => Math.Tanh(x),
    });

    private CalculationResult Logarithm(double x) => x <= 0
        ? Fail(CalcError.NonPositiveLog)
        : Ok(kind == Log10 ? Math.Log10(x) : Math.Log(x));

    private CalculationResult Guarded(double x) => kind switch
    {
        Sqrt => x < 0 ? Fail(CalcError.NegativeSqrt) : Ok(Math.Sqrt(x)),
        Reciprocal => x == 0 ? Fail(CalcError.DivisionByZero) : Ok(1 / x),
        _ => FactorialOf(x),
    };

    private CalculationResult FactorialOf(double x)
    {
        if (IsInvalidFactorial(x)) return Fail(CalcError.InvalidFactorial);
        if (x > 170) return Fail(CalcError.Overflow);
        return Ok(Enumerable.Range(2, Math.Max(0, (int)x - 1)).Aggregate(1.0, (acc, n) => acc * n));
    }

    private bool IsInvalidFactorial(double x) => x < 0 || x != Math.Floor(x);

    private CalculationResult Plain(double x) => Ok(kind switch
    {
        Exp => Math.Exp(x),
        TenPow => Math.Pow(10, x),
        Square => x * x,
        Cube => x * x * x,
        Cbrt => Math.Cbrt(x),
        _ => Math.Abs(x),
    });
}
