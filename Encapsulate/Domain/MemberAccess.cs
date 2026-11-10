using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Encapsulate.Domain;

/// <summary>
/// One field or property access wrapped from a Roslyn operation. It owns the receiver-key rules
/// (plan R1-R3, R7) and reports at the start of its receiver expression (plan R15).
/// </summary>
public sealed class MemberAccess(IOperation operation)
{
    public bool IsCountable => operation switch
    {
        IPropertyReferenceOperation { Property.IsIndexer: true } => false,
        IMemberReferenceOperation { Instance: not (null or IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ImplicitReceiver or InstanceReferenceKind.PatternInput }) } => !IsInsideNameOf,
        _ => false,
    };

    private bool IsInsideNameOf
    {
        get
        {
            for (IOperation? current = Member; current is not null; current = current.Parent)
            {
                if (current is INameOfOperation) return true;
            }
            return false;
        }
    }

    public string ReceiverKey => ReceiverKeyOf(Member);

    public int Line => Start.Line + 1;

    public int Column => Start.Character + 1;

    public bool IsBefore(MemberAccess other) => Line == other.Line ? Column < other.Column : Line < other.Line;

    private IMemberReferenceOperation Member => (IMemberReferenceOperation)operation;

    private LinePosition Start => ReceiverStart.GetLocation().GetLineSpan().StartLinePosition;

    private SyntaxNode ReceiverStart => Member.Instance switch
    {
        IConditionalAccessInstanceOperation => EnclosingConditionalAccess().Operation.Syntax,
        IMemberReferenceOperation instance => ExpressionStart(instance),
        _ => Member.Instance!.Syntax,
    };

    private string ReceiverKeyOf(IMemberReferenceOperation member) => member.Instance switch
    {
        IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } self => ContainingInstanceKey(self),
        IConditionalAccessInstanceOperation => NormalizedTextOf(ConditionalOf(member).Operation.Syntax),
        IMemberReferenceOperation instance => ExpressionKey(instance),
        _ => NormalizedTextOf(member.Instance!.Syntax),
    };

    private string ExpressionKey(IMemberReferenceOperation member) => member switch
    {
        IPropertyReferenceOperation { Property.IsIndexer: true } => NormalizedTextOf(member.Syntax),
        _ when member.Instance is IConditionalAccessInstanceOperation => $"{ReceiverKeyOf(member)}?.{member.Member.Name}",
        _ when IsConditionalBinding(member.Syntax) => $"{ReceiverKeyOf(member)}.{member.Member.Name}",
        _ => NormalizedTextOf(member.Syntax),
    };

    private SyntaxNode ExpressionStart(IMemberReferenceOperation member) =>
        member.Instance is IConditionalAccessInstanceOperation || IsConditionalBinding(member.Syntax)
            ? ConditionalOf(member).Operation.Syntax
            : member.Syntax;

    private bool IsConditionalBinding(SyntaxNode node) => node switch
    {
        MemberBindingExpressionSyntax => true,
        MemberAccessExpressionSyntax access => IsConditionalBinding(access.Expression),
        _ => false,
    };

    private string ContainingInstanceKey(IInstanceReferenceOperation self) => self.Syntax is BaseExpressionSyntax ? "base" : "this";

    private IConditionalAccessOperation EnclosingConditionalAccess() => ConditionalOf(Member);

    private IConditionalAccessOperation ConditionalOf(IOperation owner)
    {
        IOperation current = owner;
        while (current is not IConditionalAccessOperation)
        {
            current = current.Parent ?? throw new InvalidOperationException("Conditional access instance outside a conditional access.");
        }
        return (IConditionalAccessOperation)current;
    }

    private string NormalizedTextOf(SyntaxNode node) => node switch
    {
        ParenthesizedExpressionSyntax parenthesized => NormalizedTextOf(parenthesized.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } forgiven => NormalizedTextOf(forgiven.Operand),
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } ownMember => TokenTextOf(ownMember.Name),
        MemberAccessExpressionSyntax member => $"{NormalizedTextOf(member.Expression)}.{TokenTextOf(member.Name)}",
        _ => TokenTextOf(node),
    };

    private string TokenTextOf(SyntaxNode node) => string.Concat(node.DescendantTokens().Select(token => token.Text));
}
