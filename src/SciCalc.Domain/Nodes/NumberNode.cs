namespace SciCalc.Domain;

public sealed class NumberNode(double value) : Node
{
    public override CalculationResult EvaluateNode(EvaluationContext context) =>
        CalculationResult.Ok(value);
}
