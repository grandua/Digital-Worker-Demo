using Encapsulate.Domain;
using Encapsulate.Presentation;

namespace Encapsulate.UnitTests.Presentation;

public class ReportTests
{
    [Fact]
    public void GroupsLinesUnderOneHeaderPerFile()
    {
        var findings = new[]
        {
            new SmellFinding("a.cs", 2, 9, SmellKind.FeatureEnvy),
            new SmellFinding("a.cs", 5, 1, SmellKind.PrimitiveObsession),
            new SmellFinding("b.cs", 2, 3, SmellKind.FeatureEnvy),
        };

        var lines = new Report(findings).Render();

        Assert.Equal(
            ["a.cs:", "  Feature Envy (2, 9)", "  Primitive Obsession (5, 1)", "b.cs:", "  Feature Envy (2, 3)"],
            lines);
    }

    [Fact]
    public void NoFindingsRendersNoLines()
    {
        Assert.Empty(new Report([]).Render());
    }
}
