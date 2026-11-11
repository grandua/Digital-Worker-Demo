namespace Encapsulate.Domain;

/// <summary>The member accesses that share one receiver key. Feature Envy excludes the containing instance.</summary>
public sealed class Receiver(string key, IEnumerable<ReceiverAccess> accesses)
{
    /// <summary>The rule threshold both smell rules share: a receiver or argument must occur at least twice.</summary>
    public const int RepetitionThreshold = 2;

    /// <summary>Receiver key of an access on the containing instance, written implicitly or as `this`.</summary>
    public const string ThisKey = "this";

    /// <summary>Receiver key of an access on the base instance.</summary>
    public const string BaseKey = "base";

    private readonly IReadOnlyList<ReceiverAccess> occurrences = accesses.ToList();

    /// <summary>Selects the envied receivers from accesses grouped by receiver key; the envy rules live here.</summary>
    public static IEnumerable<SmellFinding> EnviedFrom(IEnumerable<IGrouping<string, ReceiverAccess>> grouped, string path) =>
        grouped.Select(group => new Receiver(group.Key, group))
            .Where(receiver => receiver.IsEnvied)
            .Select(receiver => receiver.CreateFinding(path, SmellKind.FeatureEnvy));

    private bool HasRepeatedAccess => occurrences.Count >= RepetitionThreshold;

    public bool IsEnvied => HasRepeatedAccess && !key.IsContainingInstance();

    public SmellFinding CreateFinding(string path, SmellKind kind)
    {
        ReceiverAccess first = occurrences.MinBy(access => (access.Line, access.Column))!;
        return SmellFinding.At(path, (first.Line, first.Column), kind);
    }
}
