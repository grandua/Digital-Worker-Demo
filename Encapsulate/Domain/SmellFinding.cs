namespace Encapsulate.Domain;

/// <summary>A reported smell: file, 1-based position and smell kind. Value equality allows duplicate collapsing.</summary>
public sealed record SmellFinding(string FilePath, int Line, int Column, string Kind)
{
    public const string FeatureEnvy = "Feature Envy";
    public const string PrimitiveObsession = "Primitive Obsession";
}
