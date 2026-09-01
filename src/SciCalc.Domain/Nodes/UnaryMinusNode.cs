namespace SciCalc.Domain;

public sealed class UnaryMinusNode(Node inner) : Node
{
    public override CalculationResult EvaluateNode(EvaluationContext context)
    {
        CalculationResult innerResult = inner.EvaluateNode(context);
        return innerResult.HasError ? innerResult : CalculationResult.Ok(-innerResult.Value!.Value);
    }
}
