using Encapsulate.Domain;

namespace Encapsulate.Presentation;

/// <summary>Groups the (already de-duplicated and ordered) findings into one header per file plus smell lines.</summary>
public sealed class Report(IReadOnlyList<SmellFinding> findings)
{
    // H-4 remainder accepted at P4-b: grouping and the sort key live on SmellFinding; reading the three fields of a
    // data record to format its line is presentation, not envy.
    public IReadOnlyList<string> Render() => findings
        .GroupBy(finding => finding.FilePath)
        .SelectMany(group => new[] { $"{group.Key}:" }.Concat(group.Select(FormatLine)))
        .ToList();

    private string FormatLine(SmellFinding finding) =>
        FormattableString.Invariant($"  {DisplayName(finding.Kind)} ({finding.Line}, {finding.Column})");

    private string DisplayName(SmellKind kind) => kind switch
    {
        SmellKind.FeatureEnvy => "Feature Envy",
        SmellKind.PrimitiveObsession => "Primitive Obsession",
        _ => throw new InvalidOperationException($"Unknown smell kind: {kind}"),
    };
}
