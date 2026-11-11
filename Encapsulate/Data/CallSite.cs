using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

using Encapsulate.Domain;

namespace Encapsulate.Data;

/// <summary>
/// The separate syntactic arguments of one invocation, object creation or constructor initializer.
/// Each argument contributes its outermost member access; reduced extension receivers, default values
/// and implicit arguments are not separate arguments.
/// </summary>
public sealed class CallSite(IEnumerable<IArgumentOperation> arguments)
{
    public IReadOnlyList<SmellFinding> CollectFindings(string path) => CountedArguments()
        .GroupBy(counted => counted.Access.ReceiverKey)
        .Where(group => group.Count() >= Receiver.RepetitionThreshold)
        .Select(group => ToFinding(group, path))
        .ToList();

    private IEnumerable<Counted> CountedArguments() => arguments
        .Where(IsSeparateArgument)
        .SelectMany(ValuesOf)
        .Select(value => new Counted(this, Unwrap(value), value))
        .Where(counted => counted.Access.IsCountable);

    private SmellFinding ToFinding(IGrouping<string, Counted> group, string path)
    {
        Counted first = group.MinBy(counted => counted.Start)!;
        return SmellFinding.At(path, (first.Start.Line, first.Start.Character), SmellKind.PrimitiveObsession);
    }

    private bool IsSeparateArgument(IArgumentOperation argument) =>
        IsExplicitArgument(argument) && AppearsInSource(argument);

    /// <summary>Whether the argument carries an explicitly written value rather than a filled-in default.</summary>
    private bool IsExplicitArgument(IArgumentOperation argument) =>
        argument.ArgumentKind is not ArgumentKind.DefaultValue;

    /// <summary>Whether the argument appears in source as a separate written argument (expanded params, explicit, or written syntax).</summary>
    private bool AppearsInSource(IArgumentOperation argument) =>
        IsExpandedParams(argument) || !argument.IsImplicit || IsWrittenArgument(argument.Syntax);

    private bool IsExpandedParams(IArgumentOperation argument) =>
        argument.ArgumentKind is ArgumentKind.ParamArray or ArgumentKind.ParamCollection;

    private bool IsWrittenArgument(SyntaxNode syntax) =>
        IsWrittenOrAncestorArgument(OutermostParenthesisOrSelf(syntax));

    /// <summary>Whether the outermost node is itself an argument or sits directly inside one.</summary>
    private bool IsWrittenOrAncestorArgument(SyntaxNode outermost) =>
        outermost is ArgumentSyntax || outermost.Parent is ArgumentSyntax;

    /// <summary>The outermost parenthesis around <paramref name="node"/>, or the node itself when it has none.</summary>
    private SyntaxNode OutermostParenthesisOrSelf(SyntaxNode node)
    {
        SyntaxNode current = node;
        while (current.Parent is ParenthesizedExpressionSyntax)
        {
            current = current.Parent;
        }
        return current;
    }

    private IEnumerable<IOperation> ValuesOf(IArgumentOperation argument) =>
        IsExpandedParams(argument)
            ? ExpandedElementsOf(Unwrap(argument.Value))
            : [argument.Value];

    private IEnumerable<IOperation> ExpandedElementsOf(IOperation value) => value switch
    {
        IArrayCreationOperation { Initializer: not null } array => array.Initializer!.ElementValues,
        ICollectionExpressionOperation collection => collection.Elements,
        _ => [value],
    };

    // Unwrap stays a single 4-arm switch on purpose (plan P6-c / M-3 decision): it is the one operation-level
    // wrapper-peeling point (P2-b kept it as such — Roslyn has no public equivalent), its arms are disjoint wrap
    // kinds, and further extraction would only relocate the same three branches.
    private IOperation Unwrap(IOperation value) => value switch
    {
        IConversionOperation conversion => Unwrap(conversion.Operand),
        IParenthesizedOperation parenthesized => Unwrap(parenthesized.Operand),
        IConditionalAccessOperation { WhenNotNull: not null } conditional => Unwrap(conditional.WhenNotNull),
        _ => value,
    };

    /// <summary>
    /// One countable argument: the unwrapped member classifies the receiver, while the position
    /// reports at the start of the original argument value (plan R15), before casts and parentheses.
    /// </summary>
    private sealed class Counted(CallSite site, IOperation member, IOperation value)
    {
        public MemberAccess Access { get; } = MemberAccess.For(member);

        private readonly CallSite owner = site;

        public LinePosition Start => Origin.GetLocation().GetLineSpan().StartLinePosition;

        private SyntaxNode Origin => owner.OutermostParenthesisOrSelf(value.Syntax);
    }
}
