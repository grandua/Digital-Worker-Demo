using Encapsulate.Domain;
using Encapsulate.Presentation;

namespace Encapsulate.UnitTests.Presentation;

public class ReportTests
{
    [Fact]
    public void GroupsLinesUnderOneHeaderPerFileInOrdinalOrder()
    {
        var findings = new[]
        {
            new SmellFinding("b.cs", 2, 3, SmellFinding.FeatureEnvy),
            new SmellFinding("a.cs", 5, 1, SmellFinding.PrimitiveObsession),
            new SmellFinding("a.cs", 2, 9, SmellFinding.FeatureEnvy),
        };

        var lines = new Report(findings).Render();

        Assert.Equal(
            ["a.cs", "  Feature Envy (2, 9)", "  Primitive Obsession (5, 1)", "b.cs", "  Feature Envy (2, 3)"],
            lines);
    }

    [Fact]
    public void CollapsesIdenticalFindings()
    {
        var finding = new SmellFinding("a.cs", 4, 7, SmellFinding.FeatureEnvy);

        var lines = new Report([finding, finding]).Render();

        Assert.Equal(["a.cs", "  Feature Envy (4, 7)"], lines);
    }

    [Fact]
    public void SameFileOrdersLinesByLineThenColumnThenSmell()
    {
        var findings = new[]
        {
            new SmellFinding("a.cs", 3, 1, SmellFinding.PrimitiveObsession),
            new SmellFinding("a.cs", 3, 1, SmellFinding.FeatureEnvy),
        };

        var lines = new Report(findings).Render();

        Assert.Equal(["a.cs", "  Feature Envy (3, 1)", "  Primitive Obsession (3, 1)"], lines);
    }

    [Fact]
    public void PathsAreSortedByOrdinalComparison()
    {
        var findings = new[]
        {
            new SmellFinding("a.cs", 1, 1, SmellFinding.FeatureEnvy),
            new SmellFinding("B.cs", 1, 1, SmellFinding.FeatureEnvy),
        };

        var headers = new Report(findings).Render().Where(line => !line.StartsWith(' ')).ToList();

        Assert.Equal(["B.cs", "a.cs"], headers);
    }

    [Fact]
    public void NoFindingsRendersNoLines()
    {
        Assert.Empty(new Report([]).Render());
    }
}
