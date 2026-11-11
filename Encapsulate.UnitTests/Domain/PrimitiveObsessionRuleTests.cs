using Encapsulate.Domain;
using Encapsulate.UnitTests.Support;

namespace Encapsulate.UnitTests.Domain;

public class PrimitiveObsessionRuleTests
{
    private readonly Analysis analysis = new();
    [Theory]
    [InlineData("Util.Take(o.Total, o.Tax);", 1)]
    [InlineData("Util.Take(o.Total, o.Total);", 1)]
    [InlineData("Util.One(o.Total);", 0)]
    [InlineData("Util.Take(o.Total, p.Tax);", 0)]
    [InlineData("Util.Take(o.Total + o.Tax, 1);", 0)]
    [InlineData("Util.Take(o, 1);", 0)]
    [InlineData("Util.Take(Make().Total, Make().Tax);", 1)]
    [InlineData("Util.Take(Util.One(o.Total), o.Tax);", 0)]
    [InlineData("Util.Take(this.Total, base.Tax);", 0)]
    [InlineData("Util.Take(this.Total, Tax);", 1)]
    [InlineData("Util.Take(base.Total, base.Tax);", 1)]
    [InlineData("Util.Take(this.Total, o.Tax);", 0)]
    [InlineData("Util.Take(o.B.Total, o.B.Tax);", 1)]
    [InlineData("Util.Take(o.B!.Total, o.B.Tax);", 1)]
    [InlineData("Util.Take(o.B.Total, o.Tax);", 0)]
    [InlineData("Util.Take((o.Total), o.Tax);", 1)]
    [InlineData("Util.TakeL(o.Total, o.Tax);", 1)]
    [InlineData("Util.Take(b: o.Tax, a: o.Total);", 1)]
    [InlineData("Util.Many(o.Total, o.Tax);", 1)]
    [InlineData("Util.Many(o.Total);", 0)]
    [InlineData("Util.ManyS(o.Total, o.Tax);", 1)]
    [InlineData("Util.Take(o[0], o[1]);", 0)]
    [InlineData("Util.Take(o.Total, Make().Tax);", 0)]
    [InlineData("Util.TakeN(o?.Total, o?.Tax);", 1)]
    [InlineData("Util.Opt(o.Total);", 0)]
    [InlineData("o.Total.Ext(p.Tax);", 0)]
    [InlineData("System.Math.Max(o.Total, o.Tax);", 1)]
    [InlineData("System.Func<int, int, int> f = Util.Take; f(o.Total, o.Tax);", 1)]
    [InlineData("Util.Take(o.Total, o.Tax); Util.Take(o.Total, p.Total);", 1)]
    [InlineData("Util.Take(o.Total, o.Tax); Util.Take(p.Total, p.Tax);", 2)]
    [InlineData("Util.Many(o.Total, o.Tax, o.Total);", 1)]
    [InlineData("Util.Four(o.Total, p.Total, o.Tax, p.Tax);", 2)]
    [InlineData("new Pair(o.Total, o.Tax);", 1)]
    [InlineData("Pair q = new(o.Total, o.Tax);", 1)]
    [InlineData("var q = new Order(o.Total, o.Tax);", 1)]
    [InlineData("var q = new Order(o.Total, p.Total);", 0)]
    [InlineData("Util.Take(nameof(o.Total), nameof(o.Tax));", 0)]
    public void CountsSeparateArgumentsSharingAReceiver(string body, int expected)
    {
        var findings = analysis.ScanMethod(body);

        Assert.Equal(expected, analysis.CountOf(findings, SmellKind.PrimitiveObsession));
    }

    [Fact]
    public void ConditionalArgumentsDoNotGroupDifferentRoots()
    {
        var findings = analysis.ScanMethod("Util.TakeN(o?.B.Total, p?.B.Tax);");

        Assert.Equal(0, analysis.CountOf(findings, SmellKind.PrimitiveObsession));
    }

    [Fact]
    public void ConditionalArgumentsReportAtTheRootStart()
    {
        string source = analysis.Method("Util.TakeN(o?.B.Total, o?.B.Tax);");

        var findings = analysis.Scan(source);

        Assert.Equal(analysis.Locate(source, "o?.B.Total", SmellKind.PrimitiveObsession), analysis.SingleOf(findings, SmellKind.PrimitiveObsession));
    }

    [Fact]
    public void ReportsFirstCountedArgumentValue()
    {
        string source = analysis.Method("Util.Take(b: o.Tax, a: o.Total);");

        var findings = analysis.Scan(source);

        Assert.Equal(analysis.Locate(source, "o.Tax", SmellKind.PrimitiveObsession), analysis.SingleOf(findings, SmellKind.PrimitiveObsession));
    }

    [Fact]
    public void CastArgumentsReportAtTheCastStart()
    {
        string source = analysis.Method("Util.TakeL((long)o.Total, (long)o.Tax);");

        var findings = analysis.Scan(source);

        Assert.Equal(analysis.Locate(source, "(long)o.Total", SmellKind.PrimitiveObsession), analysis.SingleOf(findings, SmellKind.PrimitiveObsession));
    }

    [Fact]
    public void ParenthesizedArgumentsReportAtTheParenthesisStart()
    {
        string source = analysis.Method("Util.Take((o.Total), o.Tax);");

        var findings = analysis.Scan(source);

        Assert.Equal(analysis.Locate(source, "(o.Total)", SmellKind.PrimitiveObsession), analysis.SingleOf(findings, SmellKind.PrimitiveObsession));
    }

    [Fact]
    public void ConstructorInitializerCallsAreSites()
    {
        string source = """
            public class Child : Pair
            {
                public Child(Order o) : base(o.Total, o.Tax) { }
                public Child(int a, int b) : this(a, b) { }
            }
            """;

        var findings = analysis.Scan(source);

        analysis.SingleOf(findings, SmellKind.PrimitiveObsession);
    }

    [Fact]
    public void PrimaryConstructorBaseCallIsASite()
    {
        string source = """
            public class Child(Order o) : Pair(o.Total, o.Tax);
            """;

        var findings = analysis.Scan(source);

        analysis.SingleOf(findings, SmellKind.PrimitiveObsession);
    }

    [Fact]
    public void ArgumentWithNoCountedMemberDoesNotCountTowardsTheRule()
    {
        var findings = analysis.ScanMethod("Util.Take(Make().Total, Util.One(o.Tax));");

        Assert.Equal(0, analysis.CountOf(findings, SmellKind.PrimitiveObsession));
    }

    [Fact]
    public void ObjectCreationWithUnresolvedTypeIsSkipped()
    {
        var findings = analysis.ScanMethod("var q = new Missing(o.Total, o.Tax);");

        Assert.Equal(0, analysis.CountOf(findings, SmellKind.PrimitiveObsession));
    }

    [Fact]
    public void BaseReceiverArgumentsReportAtTheBaseAccessStart()
    {
        string source = analysis.Method("Util.Take(base.Total, base.Tax);");

        var findings = analysis.Scan(source);

        Assert.Equal(analysis.Locate(source, "base.Total", SmellKind.PrimitiveObsession), analysis.SingleOf(findings, SmellKind.PrimitiveObsession));
    }
}
