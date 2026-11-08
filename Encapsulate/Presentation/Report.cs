using Encapsulate.Domain;

namespace Encapsulate.Presentation;

/// <summary>Collapses duplicate findings, orders them and renders one header per file plus smell lines.</summary>
public sealed class Report(IReadOnlyList<SmellFinding> findings)
{
    public IReadOnlyList<string> Render() => findings
        .Distinct()
        .OrderBy(finding => finding.FilePath, StringComparer.Ordinal)
        .ThenBy(finding => finding.Line)
        .ThenBy(finding => finding.Column)
        .ThenBy(finding => finding.Kind, StringComparer.Ordinal)
        .GroupBy(finding => finding.FilePath)
        .SelectMany(group => new[] { group.Key }.Concat(group.Select(FormatLine)))
        .ToList();

    private string FormatLine(SmellFinding finding) =>
        FormattableString.Invariant($"  {finding.Kind} ({finding.Line}, {finding.Column})");
}
