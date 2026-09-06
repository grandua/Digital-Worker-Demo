namespace SciCalc.Domain;

public sealed class NumberNode(double value) : Node
{
    public override CalculationResult EvaluateNode(AngleMode mode) =>
        CalculationResult.Ok(value);
}
