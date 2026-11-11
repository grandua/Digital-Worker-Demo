namespace Encapsulate.Domain;

/// <summary>A reported smell: file, 1-based position and smell kind. Value equality allows duplicate collapsing; the natural order owns the report sort key (path, line, column, kind).</summary>
public sealed record SmellFinding(string FilePath, int Line, int Column, SmellKind Kind) : IComparable<SmellFinding>
{
    /// <summary>Creates a finding at a zero-based position, applying the one-based conversion once.</summary>
    public static SmellFinding At(string filePath, (int Line, int Column) zeroBasedStart, SmellKind kind) =>
        new(filePath, zeroBasedStart.Line + 1, zeroBasedStart.Column + 1, kind);

    public int CompareTo(SmellFinding? other)
    {
        if (other is null) return 1;
        int byPath = string.CompareOrdinal(FilePath, other.FilePath);
        if (byPath != 0) return byPath;
        int byPosition = (Line, Column).CompareTo((other.Line, other.Column));
        return byPosition != 0 ? byPosition : Kind.CompareTo(other.Kind);
    }
}
