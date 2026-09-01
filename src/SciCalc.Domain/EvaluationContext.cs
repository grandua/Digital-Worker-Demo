namespace SciCalc.Domain;

public readonly record struct EvaluationContext(AngleMode Mode, double? Ans)
{
    public double ToRadians(double degrees) => degrees * Math.PI / 180;

    public double ToDegrees(double radians) => radians * 180 / Math.PI;
}
