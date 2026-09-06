namespace SciCalc.Domain;

public abstract class Node
{
    public abstract CalculationResult EvaluateNode(AngleMode mode);
}
