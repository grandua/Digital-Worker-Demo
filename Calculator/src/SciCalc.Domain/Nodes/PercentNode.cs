namespace SciCalc.Domain;

public sealed class PercentNode(Node inner, Node? previous) : Node
{
    public Node Inner => inner;

    public Node? Previous => previous;

    public override CalculationResult EvaluateNode(AngleMode mode)
    {
        CalculationResult innerResult = inner.EvaluateNode(mode);
        if (innerResult.HasError) return innerResult;
        return previous is null
            ? CalculationResult.Ok(innerResult.Value!.Value / 100)
            : Relative(previous.EvaluateNode(mode), innerResult.Value!.Value);
    }

    private CalculationResult Relative(CalculationResult previousResult, double innerValue)
    {
        if (previousResult.HasError) return previousResult;
        return CalculationResult.Ok(previousResult.Value!.Value * innerValue / 100);
    }
}
