using Encapsulate.Domain;
using Encapsulate.UnitTests.Support;

namespace Encapsulate.UnitTests.Domain;

public class FeatureEnvyRuleTests
{
    [Theory]
    [InlineData("var a = o.Total;", 0)]
    [InlineData("var a = o.Total + o.Total;", 1)]
    [InlineData("var a = o.Total + o.Tax;", 1)]
    [InlineData("var a = o.Total + o.Tax + o.Total;", 1)]
    [InlineData("var a = o.Total; var b = o.Tax;", 1)]
    [InlineData("var a = o.Total + o.Tax + p.Total + p.Tax;", 2)]
    [InlineData("var a = o.Total; var b = p.Total;", 0)]
    [InlineData("var a = this.Total + this.Tax;", 0)]
    [InlineData("var a = Total + Tax;", 0)]
    [InlineData("var a = base.Total + base.Tax;", 0)]
    [InlineData("var a = this.Total + base.Tax;", 0)]
    [InlineData("var a = Order.S + Order.S;", 0)]
    [InlineData("var a = Order.S + o.Total;", 0)]
    [InlineData("var a = o.B.Total + o.B.Tax;", 1)]
    [InlineData("var a = Make().Total + Make().Tax;", 1)]
    [InlineData("var a = o?.Total + o?.Tax;", 1)]
    [InlineData("var a = (o).Total + o!.Total;", 1)]
    [InlineData("var a = nameof(o.Total) + nameof(o.Tax);", 0)]
    [InlineData("var x = new Order { Total = 1, Tax = 2 };", 0)]
    [InlineData("var a = o[0] + o[1];", 0)]
    [InlineData("if (o is { Total: 1, Tax: 2 }) { }", 0)]
    [InlineData("var t = (1, 2); var a = t.Item1 + t.Item2;", 1)]
    [InlineData("System.Func<int> f = () => o.Total + o.Tax;", 1)]
    public void CountsAccessesPerReceiverInMethod(string body, int expected)
    {
        var findings = Analysis.ScanMethod(body);

        Assert.Equal(expected, Analysis.OfKind(findings, SmellFinding.FeatureEnvy).Count);
    }

    [Fact]
    public void ReportsFirstReceiverAccessPosition()
    {
        string source = Analysis.Method("var a = o.Total + o.Tax;");

        var findings = Analysis.Scan(source);

        Assert.Equal([Analysis.Locate(source, "o.Total + o.Tax", SmellFinding.FeatureEnvy)], findings);
    }

    [Fact]
    public void NestedReceiversReportOnceAtTheSharedStart()
    {
        string source = Analysis.Method("var a = o.B.Total + o.B.Tax;");

        var findings = Analysis.Scan(source);

        Assert.Equal([Analysis.Locate(source, "o.B.Total", SmellFinding.FeatureEnvy)], findings);
    }

    [Fact]
    public void NestedConditionalReceiversKeepTheirRootExpression()
    {
        var findings = Analysis.ScanMethod("var a = o?.B.Total + p?.B.Tax;");

        Assert.Empty(Analysis.OfKind(findings, SmellFinding.FeatureEnvy));
    }

    [Fact]
    public void NestedConditionalReceiverReportsOnceAtTheRootStart()
    {
        string source = Analysis.Method("var a = o?.B.Total + o?.B.Tax;");

        var findings = Analysis.Scan(source);

        Assert.Equal([Analysis.Locate(source, "o?.B.Total", SmellFinding.FeatureEnvy)], findings);
    }

    [Fact]
    public void ColumnCountsTabAsOneCharacter()
    {
        string source = "public class Target : Base\n{\n\tpublic void M(Order o)\n\t{\n\t\tvar a = o.Total + o.Tax;\n\t}\n}\n";

        var findings = Analysis.Scan(source);

        Assert.Equal([new SmellFinding(Analysis.TargetPath, 5, 11, SmellFinding.FeatureEnvy)], findings);
    }

    [Fact]
    public void WithExpressionInitializerIsNotASyntacticAccess()
    {
        string source = "public record Rec(int Total, int Tax);\npublic class Target\n{\n    public Rec M(Rec r) => r with { Total = 1, Tax = 2 };\n}\n";

        var findings = Analysis.Scan(source);

        Assert.Empty(Analysis.OfKind(findings, SmellFinding.FeatureEnvy));
    }

    [Fact]
    public void LocalFunctionIsItsOwnUnitAndIsNotMergedIntoTheEnclosingMethod()
    {
        var findings = Analysis.ScanMethod("var a = o.Total; int L() => o.Total + o.Tax; var b = L();");

        Assert.Single(Analysis.OfKind(findings, SmellFinding.FeatureEnvy));
    }

    [Fact]
    public void LocalFunctionAndEnclosingMethodEachReportTheirOwnAccesses()
    {
        var findings = Analysis.ScanMethod("var a = o.Total + o.Tax; int L() => o.Total + o.Tax; var b = L();");

        Assert.Equal(2, Analysis.OfKind(findings, SmellFinding.FeatureEnvy).Count);
    }

    [Fact]
    public void FieldInitializerCountsRepeatedAccessesAtTheThreshold()
    {
        string source = "public class Target\n{\n    int one = new Order().Total;\n    int two = new Order().Total + new Order().Tax;\n}\n";

        var findings = Analysis.Scan(source);

        Assert.Equal([new SmellFinding(Analysis.TargetPath, 4, 15, SmellFinding.FeatureEnvy)], findings);
    }

    [Fact]
    public void ExpressionBodiedMethodIsAUnit()
    {
        var findings = Analysis.Scan("public class Target\n{\n    public int E(Order o) => o.Total + o.Tax;\n}\n");

        Assert.Single(Analysis.OfKind(findings, SmellFinding.FeatureEnvy));
    }

    [Fact]
    public void UnresolvedReceiverIsSkippedWithoutFailing()
    {
        var findings = Analysis.ScanMethod("var a = Unknown.Total + Unknown.Tax;");

        Assert.Empty(Analysis.OfKind(findings, SmellFinding.FeatureEnvy));
    }
}
