using static SciCalc.Domain.CalculationResult;
using static SciCalc.Domain.FunctionKind;

namespace SciCalc.Domain;

public sealed class FunctionNode(FunctionKind kind, Node argument) : Node
{
    public override CalculationResult EvaluateNode(EvaluationContext context)
    {
        CalculationResult arg = argument.EvaluateNode(context);
        return arg.HasError ? arg : Apply(arg.Value!.Value, context);
    }

    private CalculationResult Apply(double x, EvaluationContext context) => kind switch
    {
        Sin or Cos or Tan => Trigonometric(x, context),
        Asin or Acos or Atan => Inverse(x, context),
        Sinh or Cosh or Tanh => Hyperbolic(x),
        Log10 or Ln => Logarithm(x),
        Sqrt or Reciprocal or Factorial => Guarded(x),
        _ => Plain(x),
    };

    private CalculationResult Trigonometric(double x, EvaluationContext context)
    {
        double input = context.Mode == AngleMode.Degrees ? context.ToRadians(x) : x;
        return Ok(kind switch
        {
            Sin => Math.Sin(input),
            Cos => Math.Cos(input),
            _ => Math.Tan(input),
        });
    }

    private CalculationResult Inverse(double x, EvaluationContext context)
    {
        if (kind != Atan && Math.Abs(x) > 1) return Fail(CalcError.AsinAcosOutOfRange);
        double radians = kind switch
        {
            Asin => Math.Asin(x),
            Acos => Math.Acos(x),
            _ => Math.Atan(x),
        };
        return Ok(context.Mode == AngleMode.Degrees ? context.ToDegrees(radians) : radians);
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
        if (x < 0 || x != Math.Floor(x)) return Fail(CalcError.InvalidFactorial);
        if (x > 170) return Fail(CalcError.Overflow);
        double acc = 1;
        for (int n = 2; n <= (int)x; n++) acc *= n;
        return Ok(acc);
    }

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
