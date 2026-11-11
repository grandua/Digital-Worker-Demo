using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Encapsulate.Domain;

namespace Encapsulate.Data;

/// <summary>
/// One compilation over all scanned sources (plus the SDK implicit global usings, plan R11).
/// Each source tree contributes its analysis units; the findings are gathered unsorted.
/// </summary>
public sealed class Codebase
{
    private const string ImplicitGlobalUsings = """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.IO;
        global using global::System.Linq;
        global using global::System.Net.Http;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        """;

    private readonly CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14);
    private readonly IReadOnlyList<SyntaxTree> sourceTrees;
    private readonly CSharpCompilation compilation;

    public Codebase(IReadOnlyList<SourceFile> sources)
    {
        sourceTrees = sources.Select(ParseSource).ToList();
        compilation = CSharpCompilation.Create(
            "Encapsulate.Analysis",
            sourceTrees.Append(ParseText(ImplicitGlobalUsings, "ImplicitGlobalUsings.cs")),
            TrustedReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>Gathers the findings of every analysis unit, de-duplicated and in report order (the sort key lives on SmellFinding).</summary>
    public IReadOnlyList<SmellFinding> FindSmells() => sourceTrees
        .SelectMany(Units)
        .SelectMany(unit => unit.CollectFindings())
        .Distinct()
        .Order()
        .ToList();

    private IEnumerable<CodeUnit> Units(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodesAndSelf()
            .Select(node => CodeUnit.For(compilation.GetSemanticModel(tree), node))
            .OfType<CodeUnit>();

    // Trusted-reference loading lives in the Data layer: reading the runtime assembly paths is infrastructure I/O.
    private IReadOnlyList<MetadataReference> TrustedReferences() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToList();

    private SyntaxTree ParseText(string text, string path) =>
        CSharpSyntaxTree.ParseText(text, parseOptions, path);

    private SyntaxTree ParseSource(SourceFile source) => ParseText(source.Text, source.HeaderPath);
}
