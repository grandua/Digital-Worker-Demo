using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Encapsulate.Domain;

/// <summary>
/// One analysis unit: a method, accessor, initializer, local function or the file's top-level statements.
/// It walks the body's operations (lambdas included, local functions excluded) and yields its findings.
/// </summary>
public sealed class CodeUnit(SemanticModel model, SyntaxNode scope)
{
    public IReadOnlyList<SmellFinding> CollectFindings()
    {
        string path = scope.SyntaxTree.FilePath;
        var collector = new AccessCollector();
        foreach (IOperation operation in RootOperations)
        {
            collector.Visit(operation);
        }
        return collector.ReportFindings(path);
    }

    private IEnumerable<IOperation> RootOperations => scope switch
    {
        CompilationUnitSyntax unit => unit.Members.OfType<GlobalStatementSyntax>().Select(node => model.GetOperation(node.Statement)).OfType<IOperation>(),
        _ => SingleOperation(model.GetOperation(InitializerExpression(scope) ?? scope)),
    };

    private IEnumerable<IOperation> SingleOperation(IOperation? operation) =>
        operation is null ? [] : [Body(operation)];

    private ExpressionSyntax? InitializerExpression(SyntaxNode node) => node switch
    {
        PropertyDeclarationSyntax property => property.Initializer?.Value ?? property.ExpressionBody?.Expression,
        IndexerDeclarationSyntax indexer => indexer.ExpressionBody?.Expression,
        VariableDeclaratorSyntax declarator => declarator.Initializer?.Value,
        _ => null,
    };

    private IOperation Body(IOperation operation) =>
        operation is ILocalFunctionOperation local && local.Body is { } body ? body : operation;

    private sealed class AccessCollector : OperationWalker
    {
        // Visitor accumulator: mutable by design and confined to this collector instance.
        private readonly List<MemberAccess> accesses = [];
        private readonly List<CallSite> callSites = [];

        public IReadOnlyList<SmellFinding> ReportFindings(string path)
        {
            var receivers = accesses
                .Where(access => access.IsCountable)
                .GroupBy(access => access.ReceiverKey)
                .Select(group => new Receiver(group.Key, group))
                .Where(receiver => receiver.IsEnvied)
                .Select(receiver => receiver.CreateFinding(path, SmellFinding.FeatureEnvy));
            return receivers.Concat(callSites.SelectMany(site => site.CollectFindings(path))).ToList();
        }

        public override void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            accesses.Add(new MemberAccess(operation));
            base.VisitPropertyReference(operation);
        }

        public override void VisitFieldReference(IFieldReferenceOperation operation)
        {
            accesses.Add(new MemberAccess(operation));
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
