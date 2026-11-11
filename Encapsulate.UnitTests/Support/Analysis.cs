using Encapsulate.Data;
using Encapsulate.Domain;


namespace Encapsulate.UnitTests.Support;

/// <summary>Analyses in-memory C# snippets as a per-test fixture. The prelude declares the sample types; only Target.cs findings are returned.</summary>
internal sealed class Analysis
{
    public const string TargetPath = "Target.cs";

    public const string Prelude = """
        public class Order
        {
            public int Total;
            public int Tax { get; set; }
            public static int S;
            public Order B = null!;
            public Order() { }
            public Order(int total, int tax) { Total = total; Tax = tax; }
            public int this[int i] => i;
        }

        public class Base
        {
            public int Total;
            public int Tax;
        }

        public class Pair
        {
            public Pair(int a, int b) { }
        }

        public static class Util
        {
            public static int Take(int a, int b) => a;
            public static int Four(int a, int b, int c, int d) => a;
            public static long TakeL(long a, long b) => a;
            public static int TakeN(int? a, int? b) => 0;
            public static int One(int a) => a;
            public static void Many(params int[] xs) { }
            public static void ManyS(params ReadOnlySpan<int> xs) { }
            public static int Opt(int a, int b = 0) => a;
            public static void Ext(this int x, int y) { }
        }
        """;

    /// <summary>Wraps a body in Target.M(Order o, Order p); Target derives from Base and owns Total and Tax.</summary>
    public string Method(string body) => $$"""
        public class Target : Base
        {
            public Order Make() => new Order();
            public void M(Order o, Order p)
            {
                {{body}}
            }
        }
        """;

    public IReadOnlyList<SmellFinding> Scan(string targetSource) =>
        WithPrelude((TargetPath, targetSource)).Where(finding => finding.FilePath == TargetPath).Distinct().ToList();

    public IReadOnlyList<SmellFinding> ScanMethod(string body) => Scan(Method(body));

    /// <summary>1-based (line, column) of the first occurrence of <paramref name="text"/>; a tab counts as one column.</summary>
    public SmellFinding Locate(string source, string text, SmellKind kind)
    {
        int index = source.IndexOf(text, StringComparison.Ordinal);
        if (index < 0) throw new ArgumentException($"'{text}' not found", nameof(text));
        int lineStart = source.LastIndexOf('\n', index) + 1;
        int line = source.Take(index).Count(c => c == '\n') + 1;
        return new SmellFinding(TargetPath, line, index - lineStart + 1, kind);
    }

    public int CountOf(IReadOnlyList<SmellFinding> findings, SmellKind kind) => OfKind(findings, kind).Count;

    public SmellFinding SingleOf(IReadOnlyList<SmellFinding> findings, SmellKind kind) => Assert.Single(OfKind(findings, kind));

    public IReadOnlyList<SmellFinding> OfKind(IEnumerable<SmellFinding> findings, SmellKind kind) =>
        findings.Where(finding => finding.Kind == kind).ToList();

    /// <summary>Runs a full scan over the prelude plus the given target files; the findings of all files are returned.</summary>
    public IReadOnlyList<SmellFinding> WithPrelude(params (string Path, string Text)[] files) =>
        new Codebase(files
            .Select(file => new SourceFile(file.Path, file.Text))
            .Prepend(new SourceFile("Prelude.cs", Prelude))
            .ToList())
        .FindSmells();
}
