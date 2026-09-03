namespace SciCalc.Domain;

public sealed class BinaryNode(OperatorKind op, Node left, Node right) : Node
{
    public override CalculationResult EvaluateNode(AngleMode mode)
    {
        CalculationResult leftResult = left.EvaluateNode(mode);
        if (leftResult.HasError) return leftResult;
        CalculationResult rightResult = right.EvaluateNode(mode);
        if (rightResult.HasError) return rightResult;
        return Apply(leftResult.Value!.Value, rightResult.Value!.Value);
    }

    private CalculationResult Apply(double leftValue, double rightValue) => op switch
    {
        OperatorKind.Add => CalculationResult.Ok(leftValue + rightValue),
        OperatorKind.Subtract => CalculationResult.Ok(leftValue - rightValue),
        OperatorKind.Multiply => CalculationResult.Ok(leftValue * rightValue),
        OperatorKind.Divide => CheckedZero(rightValue, () => leftValue / rightValue),
        OperatorKind.Modulo => CheckedZero(rightValue, () => leftValue % rightValue),
        _ => CalculationResult.Ok(Math.Pow(leftValue, rightValue)),
    };

    private CalculationResult CheckedZero(double divisor, Func<double> compute) =>
        divisor == 0 ? CalculationResult.Fail(CalcError.DivisionByZero) : CalculationResult.Ok(compute());
}
