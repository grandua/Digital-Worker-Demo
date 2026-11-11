using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using Encapsulate.Domain;

namespace Encapsulate.Data;

/// <summary>
/// One analysis unit: a method, accessor, initializer, local function or the file's top-level statements.
/// It walks the body's operations (lambdas included, local functions excluded) and yields its findings.
/// </summary>
public sealed class CodeUnit(SemanticModel model, SyntaxNode scope)
{
    /// <summary>
    /// Creates the analysis unit for a node that is one (method, accessor, local function, field/event/property/indexer
    /// initializer, primary constructor base or the file's top-level statements); null for any other node.
    /// The unit-scope and initializer declaration-kind rules live together here.
    /// </summary>
    public static CodeUnit? For(SemanticModel model, SyntaxNode node)
    {
        var unit = new CodeUnit(model, node);
        return unit.IsAnalysisUnit ? unit : null;
    }

    private bool IsAnalysisUnit => scope switch
    {
        CompilationUnitSyntax unit => unit.Members.OfType<GlobalStatementSyntax>().Any(),
        BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax => true,
        PrimaryConstructorBaseTypeSyntax => true,
        // A local declarator initializer belongs to its enclosing method's unit; only field and event initializers form their own unit.
        VariableDeclaratorSyntax { Initializer: not null, Parent.Parent: FieldDeclarationSyntax or EventFieldDeclarationSyntax } => true,
        _ => InitializerExpression(scope) is not null,
    };

    public IReadOnlyList<SmellFinding> CollectFindings()
    {
        string path = scope.SyntaxTree.FilePath;
        var collector = new AccessCollector();
        foreach (IOperation operation in RootOperations)
        {
            collector.Visit(operation);
        }
        return EnviedAccessFindings(collector.Accesses, path)
            .Concat(collector.CallSites.SelectMany(site => site.CollectFindings(path)))
            .ToList();
    }

    private IEnumerable<SmellFinding> EnviedAccessFindings(IEnumerable<MemberAccess> accesses, string path) =>
        Receiver.EnviedFrom(accesses
            .Where(access => access.IsCountable)
            .GroupBy(access => access.ReceiverKey, access => access.ToReceiverAccess()), path);

    private IEnumerable<IOperation> RootOperations => scope switch
    {
        CompilationUnitSyntax unit => unit.Members.OfType<GlobalStatementSyntax>().Select(node => model.GetOperation(node.Statement)).OfType<IOperation>(),
        _ => SingleOperation(model.GetOperation(InitializerExpression(scope) ?? scope)),
    };

    private IEnumerable<IOperation> SingleOperation(IOperation? operation) =>
        operation is null ? [] : [LocalFunctionBodyOrSelf(operation)];

    private ExpressionSyntax? InitializerExpression(SyntaxNode node) => node switch
    {
        PropertyDeclarationSyntax property => PropertyInitializer(property),
        IndexerDeclarationSyntax indexer => indexer.ExpressionBody?.Expression,
        VariableDeclaratorSyntax declarator => declarator.Initializer?.Value,
        _ => null,
    };

    private ExpressionSyntax? PropertyInitializer(PropertyDeclarationSyntax property) =>
        property.Initializer?.Value ?? property.ExpressionBody?.Expression;

    private IOperation LocalFunctionBodyOrSelf(IOperation operation) =>
        operation is ILocalFunctionOperation local && local.Body is { } body ? body : operation;

    private sealed class AccessCollector : OperationWalker
    {
        // Visitor accumulator: mutable by design and confined to this collector instance.
        private readonly List<MemberAccess> accesses = [];
        private readonly List<CallSite> callSites = [];

        public IReadOnlyList<MemberAccess> Accesses => accesses;

        public IReadOnlyList<CallSite> CallSites => callSites;

        public override void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            accesses.Add(MemberAccess.For(operation));
            base.VisitPropertyReference(operation);
        }

        public override void VisitFieldReference(IFieldReferenceOperation operation)
        {
            accesses.Add(MemberAccess.For(operation));
            base.VisitFieldReference(operation);
        }

        public override void VisitInvocation(IInvocationOperation operation)
        {
            callSites.Add(new CallSite(operation.Arguments));
            base.VisitInvocation(operation);
        }

        public override void VisitObjectCreation(IObjectCreationOperation operation)
        {
            callSites.Add(new CallSite(operation.Arguments));
            base.VisitObjectCreation(operation);
        }

        public override void VisitLocalFunction(ILocalFunctionOperation operation)
        {
            // A local function is its own analysis unit; its body is not counted here.
        }
    }
}
