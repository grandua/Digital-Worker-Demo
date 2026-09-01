namespace SciCalc.Domain;

public sealed class BinaryNode(OperatorKind op, Node left, Node right) : Node
{
    public override CalculationResult EvaluateNode(EvaluationContext context)
    {
        CalculationResult leftResult = left.EvaluateNode(context);
        if (leftResult.HasError) return leftResult;
        CalculationResult rightResult = right.EvaluateNode(context);
        if (rightResult.HasError) return rightResult;
        return Apply(leftResult.Value!.Value, rightResult.Value!.Value);
    }

    private CalculationResult Apply(double leftValue, double rightValue) => op switch
    {
        OperatorKind.Add => CalculationResult.Ok(leftValue + rightValue),
        OperatorKind.Sub => CalculationResult.Ok(leftValue - rightValue),
        OperatorKind.Mul => CalculationResult.Ok(leftValue * rightValue),
        OperatorKind.Div => CheckedZero(rightValue, () => leftValue / rightValue),
        OperatorKind.Mod => CheckedZero(rightValue, () => leftValue % rightValue),
        _ => CalculationResult.Ok(Math.Pow(leftValue, rightValue)),
    };

    private static CalculationResult CheckedZero(double divisor, Func<double> compute) =>
        divisor == 0 ? CalculationResult.Fail(CalcError.DivisionByZero) : CalculationResult.Ok(compute());
}
