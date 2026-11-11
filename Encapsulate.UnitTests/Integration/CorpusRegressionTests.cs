using Encapsulate.Data;
using Encapsulate.Domain;

namespace Encapsulate.UnitTests.Integration;

/// <summary>Regression over real repository sources (plan §6 corpus). Reads files; no mocks.</summary>
//TODO: Framework Design -- Sealing: test fixture is sealed without a documented reason; decide project-wide (same convention as all production types).
public sealed class CorpusRegressionTests
{
    private const string LinkEndpoints = "Api/LinkEndpoints.cs";
    private const string Calculator = "Calculator.cs";

    private readonly string repositoryRoot;

    public CorpusRegressionTests() => repositoryRoot = FindRepositoryRoot();

    private IReadOnlyList<SmellFinding> ScanDirectory(string relativeDirectory) =>
        new Codebase(new SourceTree(Path.Combine(repositoryRoot, relativeDirectory)).ReadSources()).FindSmells();

    [Fact]
    public void LinkEndpointsReportsOneFeatureEnvyAndOnePrimitiveObsessionLine()
    {
        var findings = ScanDirectory("UrlShortener/Presentation/UrlShortener.Api")
            .Where(finding => finding.FilePath == LinkEndpoints)
            .Distinct()
            .ToList();

        Assert.Equal(
            [
                new SmellFinding(LinkEndpoints, 50, 13, SmellKind.FeatureEnvy),
                new SmellFinding(LinkEndpoints, 50, 31, SmellKind.PrimitiveObsession),
            ],
            findings.OrderBy(finding => finding.Line).ThenBy(finding => finding.Column).ToList());
    }

    [Theory]
    [InlineData(225)]
    [InlineData(237)]
    public void CalculatorBufferUnitsWithOneAccessAreNotFeatureEnvy(int line)
    {
        var findings = ScanDirectory("Calculator/Domain/SciCalc.Domain")
            .Where(finding => finding.FilePath == Calculator && finding.Line == line)
            .ToList();

        Assert.Empty(findings);
    }

    private string FindRepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory, "UrlShortener")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new DirectoryNotFoundException("Repository root with UrlShortener/ not found above the test binary.");
    }
}
