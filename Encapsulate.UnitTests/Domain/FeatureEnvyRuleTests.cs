using Encapsulate.Domain;
using Encapsulate.UnitTests.Support;

namespace Encapsulate.UnitTests.Domain;

public class FeatureEnvyRuleTests
{
    private readonly Analysis analysis = new();
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
        var findings = analysis.ScanMethod(body);

        Assert.Equal(expected, analysis.CountOf(findings, SmellKind.FeatureEnvy));
    }

    [Fact]
    public void EscapedMethodNamedNameofDoesNotSuppressAccesses()
    {
        string source = """
            public class Target
            {
                public void M(Order o)
                {
                    var a = Ops.@nameof(o.Total, o.Tax);
                }
            }
            public static class Ops
            {
                public static int @nameof(int a, int b) => a;
            }
            """;

        var findings = analysis.Scan(source);

        analysis.SingleOf(findings, SmellKind.FeatureEnvy);
        analysis.SingleOf(findings, SmellKind.PrimitiveObsession);
    }

    [Fact]
    public void ReportsFirstReceiverAccessPosition()
    {
        string source = analysis.Method("var a = o.Total + o.Tax;");

        var findings = analysis.Scan(source);

        Assert.Equal([analysis.Locate(source, "o.Total + o.Tax", SmellKind.FeatureEnvy)], findings);
    }

    [Fact]
    public void NestedReceiversReportOnceAtTheSharedStart()
    {
        string source = analysis.Method("var a = o.B.Total + o.B.Tax;");

        var findings = analysis.Scan(source);

        Assert.Equal([analysis.Locate(source, "o.B.Total", SmellKind.FeatureEnvy)], findings);
    }

    [Fact]
    public void NestedConditionalReceiversKeepTheirRootExpression()
    {
        var findings = analysis.ScanMethod("var a = o?.B.Total + p?.B.Tax;");

        Assert.Equal(0, analysis.CountOf(findings, SmellKind.FeatureEnvy));
    }

    [Fact]
    public void NestedConditionalReceiverReportsOnceAtTheRootStart()
    {
        string source = analysis.Method("var a = o?.B.Total + o?.B.Tax;");

        var findings = analysis.Scan(source);

        Assert.Equal([analysis.Locate(source, "o?.B.Total", SmellKind.FeatureEnvy)], findings);
    }

    [Fact]
    public void ColumnCountsTabAsOneCharacter()
    {
        string source = "public class Target : Base\n{\n\tpublic void M(Order o)\n\t{\n\t\tvar a = o.Total + o.Tax;\n\t}\n}\n";

        var findings = analysis.Scan(source);

        Assert.Equal([new SmellFinding(Analysis.TargetPath, 5, 11, SmellKind.FeatureEnvy)], findings);
    }

    [Fact]
    public void WithExpressionInitializerIsNotASyntacticAccess()
    {
        string source = "public record Rec(int Total, int Tax);\npublic class Target\n{\n    public Rec M(Rec r) => r with { Total = 1, Tax = 2 };\n}\n";

        var findings = analysis.Scan(source);

        Assert.Equal(0, analysis.CountOf(findings, SmellKind.FeatureEnvy));
    }

    [Fact]
    public void LocalFunctionIsItsOwnUnitAndIsNotMergedIntoTheEnclosingMethod()
    {
        var findings = analysis.ScanMethod("var a = o.Total; int L() => o.Total + o.Tax; var b = L();");

        analysis.SingleOf(findings, SmellKind.FeatureEnvy);
    }

    [Fact]
    public void LocalFunctionAndEnclosingMethodEachReportTheirOwnAccesses()
    {
        var findings = analysis.ScanMethod("var a = o.Total + o.Tax; int L() => o.Total + o.Tax; var b = L();");

        Assert.Equal(2, analysis.CountOf(findings, SmellKind.FeatureEnvy));
    }

    [Fact]
    public void FieldInitializerCountsRepeatedAccessesAtTheThreshold()
    {
        string source = "public class Target\n{\n    int one = new Order().Total;\n    int two = new Order().Total + new Order().Tax;\n}\n";

        var findings = analysis.Scan(source);

        Assert.Equal([new SmellFinding(Analysis.TargetPath, 4, 15, SmellKind.FeatureEnvy)], findings);
    }

    [Fact]
    public void ExpressionBodiedMethodIsAUnit()
    {
        var findings = analysis.Scan("public class Target\n{\n    public int E(Order o) => o.Total + o.Tax;\n}\n");

        analysis.SingleOf(findings, SmellKind.FeatureEnvy);
    }

    [Fact]
    public void UnresolvedReceiverIsSkippedWithoutFailing()
    {
        var findings = analysis.ScanMethod("var a = Unknown.Total + Unknown.Tax;");

        Assert.Equal(0, analysis.CountOf(findings, SmellKind.FeatureEnvy));
    }

    // Characterization (refactoring plan P1-a): a nameof access and a conditional access must not share a receiver key.
    [Fact]
    public void NameofAccessDoesNotMergeIntoAConditionalReceiverKey()
    {
        var findings = analysis.ScanMethod("var a = o?.B.Total is null ? nameof(o.B) : \"\";");

        Assert.Equal(0, analysis.CountOf(findings, SmellKind.FeatureEnvy));
        Assert.Equal(0, analysis.CountOf(findings, SmellKind.PrimitiveObsession));
    }

    // Characterization (refactoring plan P1-a): a parenthesized conditional receiver groups under its own key and reports at the inner conditional access start.
    [Fact]
    public void ParenthesizedConditionalReceiverGroupsAndReportsAtTheConditionalAccessStart()
    {
        string source = analysis.Method("var a = (o?.B).Total + (o?.B).Tax;");

        var findings = analysis.Scan(source);

        Assert.Equal([analysis.Locate(source, "o?.B", SmellKind.FeatureEnvy)], findings);
    }
}
