using Encapsulate.Domain;

namespace Encapsulate.UnitTests.Domain;

public class SmellFindingTests
{
    [Fact]
    public void OrdersByPathThenLineThenColumnThenKind()
    {
        var findings = new[]
        {
            new SmellFinding("a.cs", 3, 1, SmellKind.PrimitiveObsession),
            new SmellFinding("a.cs", 3, 1, SmellKind.FeatureEnvy),
            new SmellFinding("a.cs", 2, 9, SmellKind.FeatureEnvy),
            new SmellFinding("B.cs", 1, 1, SmellKind.FeatureEnvy),
            new SmellFinding("a.cs", 5, 1, SmellKind.PrimitiveObsession),
        };

        var ordered = findings.Order().ToList();

        var expected = new[]
        {
            new SmellFinding("B.cs", 1, 1, SmellKind.FeatureEnvy),
            new SmellFinding("a.cs", 2, 9, SmellKind.FeatureEnvy),
            new SmellFinding("a.cs", 3, 1, SmellKind.FeatureEnvy),
            new SmellFinding("a.cs", 3, 1, SmellKind.PrimitiveObsession),
            new SmellFinding("a.cs", 5, 1, SmellKind.PrimitiveObsession),
        };
        Assert.Equal(expected, ordered);
    }

    [Fact]
    public void IdenticalFindingsCollapseAsDuplicates()
    {
        var finding = new SmellFinding("a.cs", 4, 7, SmellKind.FeatureEnvy);

        var distinct = new[] { finding, finding }.Distinct().ToList();

        Assert.Equal([finding], distinct);
    }
}
