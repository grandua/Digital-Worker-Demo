using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Encapsulate.Domain;

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
        sourceTrees = sources.Select(source => ParseSource(source)).ToList();
        compilation = CSharpCompilation.Create(
            "Encapsulate.Analysis",
            sourceTrees.Append(ParseText(ImplicitGlobalUsings, "ImplicitGlobalUsings.cs")),
            TrustedReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    public IReadOnlyList<SmellFinding> FindSmells() => sourceTrees
        .SelectMany(tree => UnitScopes(tree).SelectMany(scope => new CodeUnit(compilation.GetSemanticModel(tree), scope).CollectFindings()))
        .ToList();

    private IEnumerable<SyntaxNode> UnitScopes(SyntaxTree tree) =>
        tree.GetRoot().DescendantNodesAndSelf().Where(IsUnitScope);

    private bool IsUnitScope(SyntaxNode node) => node switch
    {
        CompilationUnitSyntax unit => unit.Members.OfType<GlobalStatementSyntax>().Any(),
        BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax => true,
        PrimaryConstructorBaseTypeSyntax => true,
        PropertyDeclarationSyntax { Initializer: not null } or PropertyDeclarationSyntax { ExpressionBody: not null } => true,
        IndexerDeclarationSyntax { ExpressionBody: not null } => true,
        VariableDeclaratorSyntax { Initializer: not null, Parent.Parent: FieldDeclarationSyntax or EventFieldDeclarationSyntax } => true,
        _ => false,
    };

    private IReadOnlyList<MetadataReference> TrustedReferences() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToList();

    private SyntaxTree ParseText(string text, string path) =>
        CSharpSyntaxTree.ParseText(text, parseOptions, path);

    private SyntaxTree ParseSource(SourceFile source) => ParseText(source.Text, source.HeaderPath);
}
