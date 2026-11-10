using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Encapsulate.Domain;

/// <summary>
/// The separate syntactic arguments of one invocation, object creation or constructor initializer.
/// Each argument contributes its outermost member access (plan R5); reduced extension receivers,
/// default values and implicit arguments are not separate arguments (plan R6).
/// </summary>
public sealed class CallSite(IEnumerable<IArgumentOperation> arguments)
{
    public IReadOnlyList<SmellFinding> CollectFindings(string path) => CountedArguments()
        .GroupBy(counted => counted.Access.ReceiverKey)
        .Where(group => group.Count() >= 2)
        .Select(group => Finding(group, path))
        .ToList();

    private IEnumerable<Counted> CountedArguments() => arguments
        .Where(IsSeparateArgument)
        .SelectMany(ValuesOf)
        .Select(value => new Counted(Unwrap(value), value))
        .Where(counted => counted.Access.IsCountable);

    private SmellFinding Finding(IGrouping<string, Counted> group, string path)
    {
        Counted first = group.Aggregate((earliest, next) => next.Start.CompareTo(earliest.Start) < 0 ? next : earliest);
        return new SmellFinding(path, first.Start.Line + 1, first.Start.Character + 1, SmellFinding.PrimitiveObsession);
    }

    private bool IsSeparateArgument(IArgumentOperation argument) =>
        argument.ArgumentKind is not ArgumentKind.DefaultValue
        && (IsExpandedParams(argument) || !argument.IsImplicit || IsWrittenArgument(argument.Syntax));

    private bool IsExpandedParams(IArgumentOperation argument) =>
        argument.ArgumentKind is ArgumentKind.ParamArray or ArgumentKind.ParamCollection;

    private bool IsWrittenArgument(SyntaxNode syntax) =>
        syntax is ArgumentSyntax || syntax.Ancestors().SkipWhile(node => node is ParenthesizedExpressionSyntax).FirstOrDefault() is ArgumentSyntax;

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
    private sealed class Counted(IOperation member, IOperation value)
    {
        public MemberAccess Access { get; } = new(member);

        public LinePosition Start => Origin.GetLocation().GetLineSpan().StartLinePosition;

        private SyntaxNode Origin
        {
            get
            {
                SyntaxNode current = value.Syntax;
                while (current.Parent is ParenthesizedExpressionSyntax outermost)
                {
                    current = outermost;
                }
                return current;
            }
        }
    }
}
