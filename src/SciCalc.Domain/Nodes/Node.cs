namespace SciCalc.Domain;

public abstract class Node
{
    public abstract CalculationResult EvaluateNode(EvaluationContext context);
}
