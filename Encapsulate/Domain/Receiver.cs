namespace Encapsulate.Domain;

/// <summary>The member accesses that share one receiver key. Feature Envy excludes the containing instance.</summary>
public sealed class Receiver(string key, IEnumerable<MemberAccess> accesses)
{
    private readonly IReadOnlyList<MemberAccess> occurrences = accesses.ToList();

    public string Key { get; } = key;

    public bool HasRepeatedAccess => occurrences.Count >= 2;

    public bool IsEnvied => HasRepeatedAccess && !IsContainingInstanceKey;

    private bool IsContainingInstanceKey => Key is "this" or "base";

    public SmellFinding CreateFinding(string path, string kind)
    {
        MemberAccess first = occurrences.Aggregate((earliest, next) => next.IsBefore(earliest) ? next : earliest);
        return new SmellFinding(path, first.Line, first.Column, kind);
    }
}
