using Encapsulate.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Xunit.Abstractions;

namespace Encapsulate.UnitTests;

public class ZzProbeTests(ITestOutputHelper output)
{
    [Fact]
    public void Dump()
    {
        const string source = """
            public class Order { public Order B; public int? Total; public int? Tax; }
            public static class U { public static void Take(int? a, int? b) { } }
            public class C { public void M(Order o, Order p) { var a = o?.B.Total + p?.B.Tax; U.Take(o?.B.Total, p?.B.Tax); } }
            """;
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create("P", [tree], References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M");
        var collector = new Collector();
        collector.Visit(model.GetOperation(method)!);
        foreach (var access in collector.Accesses)
        {
            output.WriteLine($"ACCESS key={access.ReceiverKey} line={access.Line} col={access.Column} countable={access.IsCountable}");
        }
        foreach (var call in collector.Calls)
        {
            foreach (var finding in call.CollectFindings("probe"))
            {
                output.WriteLine($"PO {finding.Kind} ({finding.Line}, {finding.Column})");
            }
        }
    }

    private sealed class Collector : OperationWalker
    {
        public List<MemberAccess> Accesses { get; } = [];
        public List<CallSite> Calls { get; } = [];

        public override void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            Accesses.Add(new MemberAccess(operation));
            base.VisitPropertyReference(operation);
        }

        public override void VisitFieldReference(IFieldReferenceOperation operation)
        {
            Accesses.Add(new MemberAccess(operation));
            base.VisitFieldReference(operation);
        }

        public override void VisitInvocation(IInvocationOperation operation)
        {
            Calls.Add(new CallSite(operation.Arguments));
            base.VisitInvocation(operation);
        }
    }

    private static IEnumerable<MetadataReference> References() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));
}
