using Encapsulate.Domain;

namespace Encapsulate.UnitTests.Support;

/// <summary>Analyses in-memory C# snippets. The prelude declares the sample types; only Target.cs findings are returned.</summary>
internal static class Analysis
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
    public static string Method(string body) => $$"""
        public class Target : Base
        {
            public Order Make() => new Order();
            public void M(Order o, Order p)
            {
                {{body}}
            }
        }
        """;

    public static IReadOnlyList<SmellFinding> Scan(string targetSource) =>
        Run(targetSource).Where(f => f.FilePath == TargetPath).Distinct().ToList();

    public static IReadOnlyList<SmellFinding> ScanMethod(string body) => Scan(Method(body));

    /// <summary>1-based (line, column) of the first occurrence of <paramref name="text"/>; a tab counts as one column.</summary>
    public static SmellFinding Locate(string source, string text, string kind)
    {
        int index = source.IndexOf(text, StringComparison.Ordinal);
        if (index < 0) throw new ArgumentException($"'{text}' not found", nameof(text));
        int lineStart = source.LastIndexOf('\n', index) + 1;
        int line = source.Take(index).Count(c => c == '\n') + 1;
        return new SmellFinding(TargetPath, line, index - lineStart + 1, kind);
    }

    public static IReadOnlyList<SmellFinding> OfKind(IEnumerable<SmellFinding> findings, string kind) =>
        findings.Where(f => f.Kind == kind).ToList();

    public static IReadOnlyList<SmellFinding> Run(string targetSource) =>
        new Codebase([new SourceFile("Prelude.cs", Prelude), new SourceFile(TargetPath, targetSource)]).FindSmells();
}
