using Encapsulate.Domain;
using Encapsulate.UnitTests.Support;

namespace Encapsulate.UnitTests.Domain;

public class AnalysisUnitTests
{
    [Theory]
    [InlineData("public class Target : Base { public static int operator +(Target a, Target b) { return Util.Take(a.Total, a.Tax); } }")]
    [InlineData("public class Target : Base { public static implicit operator int(Target t) => Util.Take(t.Total, t.Tax); }")]
    [InlineData("public class Target : Base { ~Target() { Util.Take(Total, Tax); } }")]
    [InlineData("public class Target : Base { public event System.Action E { add { Util.Take(Total, Tax); } remove { } } }")]
    [InlineData("public class Target : Base { public int this[int i] { get { return Util.Take(Total, Tax); } } }")]
    [InlineData("public class Target : Base { public int this[int i] => Util.Take(Total, Tax); }")]
    [InlineData("public class Target : Base { public int P => Util.Take(Total, Tax); }")]
    [InlineData("public class Target : Base { public int P { get; } = Util.Take(Total, Tax); }")]
    [InlineData("public class Target { Order o = new Order(); int v = Util.Take(o.Total, o.Tax); }")]
    [InlineData("public class Target { public int E(Order o) => Util.Take(o.Total, o.Tax); }")]
    public void EveryMemberShapeIsAnalysed(string source)
    {
        var findings = Analysis.Scan(source);

        Assert.Single(Analysis.OfKind(findings, SmellFinding.PrimitiveObsession));
    }

    [Fact]
    public void EventFieldInitializerReportsFeatureEnvyAndPrimitiveObsessionForSameReceiver()
    {
        const string source = """
            public class Target
            {
                static Order S = new Order();
                static System.Action Make(int a, int b) => () => { };
                public static event System.Action E = Make(S.Total, S.Tax);
            }
            """;

        var findings = Analysis.Scan(source);

        Assert.Contains(Analysis.Locate(source, "S.Total", SmellFinding.FeatureEnvy), findings);
        Assert.Contains(Analysis.Locate(source, "S.Total", SmellFinding.PrimitiveObsession), findings);
    }

    [Fact]
    public void LocalFunctionBodyIsCountedOnceInItsOwnUnit()
    {
        var findings = Analysis.ScanMethod("void L() { Util.Take(o.Total, o.Tax); } L();");

        Assert.Single(Analysis.OfKind(findings, SmellFinding.PrimitiveObsession));
    }

    [Fact]
    public void LocalFunctionAndEnclosingMethodEachCountTheirOwnSites()
    {
        var findings = Analysis.ScanMethod("Util.Take(o.Total, o.Tax); void L() { Util.Take(o.Total, o.Tax); } L();");

        Assert.Equal(2, Analysis.OfKind(findings, SmellFinding.PrimitiveObsession).Count);
    }

    [Fact]
    public void LambdaBodyIsCountedOnceInTheEnclosingMethod()
    {
        var findings = Analysis.ScanMethod("System.Action a = () => Util.Take(o.Total, o.Tax); a();");

        Assert.Single(Analysis.OfKind(findings, SmellFinding.PrimitiveObsession));
    }

    [Fact]
    public void TopLevelStatementsFormOneUnit()
    {
        string source = "var o = new Order();\nvar a = o.Total;\nvar b = o.Tax;\n";

        var findings = Analysis.Scan(source);

        Assert.Single(Analysis.OfKind(findings, SmellFinding.FeatureEnvy));
    }

    [Fact]
    public void TopLevelCallSiteIsReported()
    {
        string source = "var o = new Order();\nUtil.Take(o.Total, o.Tax);\n";

        var findings = Analysis.Scan(source);

        Assert.Single(Analysis.OfKind(findings, SmellFinding.PrimitiveObsession));
    }

    [Fact]
    public void MalformedSourceIsStillAnalysed()
    {
        string source = "public class Target : Base { public void M(Order o) { Util.Take(o.Total, o.Tax) } }";

        var findings = Analysis.Scan(source);

        Assert.Single(Analysis.OfKind(findings, SmellFinding.PrimitiveObsession));
    }

    [Fact]
    public void UnresolvedInvocationIsSkippedWithoutFailing()
    {
        string source = "public class Target { public void M(Order o) { Missing.Take(o.Total, o.Tax); } }";

        var findings = Analysis.Scan(source);

        Assert.Empty(Analysis.OfKind(findings, SmellFinding.PrimitiveObsession));
    }

    [Fact]
    public void DuplicateTypeNamesInTwoFilesDoNotStopAnalysis()
    {
        var sources = new[]
        {
            new SourceFile("Prelude.cs", Analysis.Prelude),
            new SourceFile("A.cs", "public class Dup { }"),
            new SourceFile(Analysis.TargetPath, "public class Dup { }\npublic class Target { public void M(Order o) { var a = o.Total + o.Tax; } }"),
        };

        var findings = new Codebase(sources).FindSmells();

        Assert.Single(findings, f => f.FilePath == Analysis.TargetPath && f.Kind == SmellFinding.FeatureEnvy);
    }

    [Fact]
    public void TopLevelStatementsInTwoFilesEachFormTheirOwnUnit()
    {
        var sources = new[]
        {
            new SourceFile("Prelude.cs", Analysis.Prelude),
            new SourceFile("A.cs", "var o = new Order();\nvar a = o.Total + o.Tax;\n"),
            new SourceFile(Analysis.TargetPath, "var o = new Order();\nvar b = o.Total + o.Tax;\n"),
        };

        var findings = new Codebase(sources).FindSmells();

        Assert.Equal(2, findings.Count(f => f.Kind == SmellFinding.FeatureEnvy));
    }
}
