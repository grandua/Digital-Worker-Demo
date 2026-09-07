namespace SciCalc.Domain;

public sealed class UnaryMinusNode(Node inner) : Node
{
    public override CalculationResult EvaluateNode(AngleMode mode)
    {
        CalculationResult innerResult = inner.EvaluateNode(mode);
        return innerResult.HasError ? innerResult : CalculationResult.Ok(-innerResult.Value!.Value);
    }
}
