using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

using Encapsulate.Domain;

namespace Encapsulate.Data;

/// <summary>
/// One field or property access wrapped from a Roslyn operation. It owns the receiver-key rules
/// (plan R1-R3, R7) and reports at the start of its receiver expression (plan R15).
/// The receiver kind is selected once by <see cref="For"/>; each kind overrides only the behavior that differs.
/// </summary>
public abstract class MemberAccess(IOperation operation)
{
    /// <summary>Selects the receiver kind of an access; the only place that reads the receiver-kind discriminators.</summary>
    public static MemberAccess For(IOperation operation) => operation switch
    {
        IMemberReferenceOperation { Instance: IConditionalAccessInstanceOperation } => new ConditionalMemberAccess(operation),
        IMemberReferenceOperation { Instance: IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } } => new ContainingInstanceMemberAccess(operation),
        IMemberReferenceOperation { Instance: IMemberReferenceOperation } => new ChainedMemberAccess(operation),
        _ => new PlainMemberAccess(operation),
    };

    public bool IsCountable => operation switch
    {
        IPropertyReferenceOperation { Property.IsIndexer: true } => false,
        IMemberReferenceOperation member => HasExplicitInstance(member.Instance) && !IsInsideNameOf,
        _ => false,
    };

    /// <summary>Whether the access carries a written receiver: not absent and not an implicit receiver or pattern input.</summary>
    private bool HasExplicitInstance(IOperation? instance) =>
        instance is not (null or IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ImplicitReceiver or InstanceReferenceKind.PatternInput });

    /// <summary>The domain-side view of this access: only the zero-based receiver start position.</summary>
    public ReceiverAccess ToReceiverAccess() => new(Start.Line, Start.Character);

    public int Line => Start.Line + 1;

    public int Column => Start.Character + 1;

    public virtual string ReceiverKey => NormalizedTextOf(Member.Instance!.Syntax);

    protected IMemberReferenceOperation Member => (IMemberReferenceOperation)operation;

    // The GetLocation().GetLineSpan().StartLinePosition chain is the canonical Roslyn position idiom (M-8 decision:
    // accepted); a helper would only wrap this framework call, used twice with no logic between the links.
    private LinePosition Start => ReceiverStart.GetLocation().GetLineSpan().StartLinePosition;

    protected virtual SyntaxNode ReceiverStart => Member.Instance!.Syntax;

    /// <summary>Whether this access joins its receiver with `?.` (only the conditional kind does).</summary>
    internal virtual bool JoinsConditionally => false;

    internal SyntaxNode MemberSyntax => Member.Syntax;

    internal string MemberName => Member.Member.Name;

    internal bool IsIndexerAccess => Member is IPropertyReferenceOperation { Property.IsIndexer: true };

    internal IOperation Source => operation;

    // The operation-level walks are kept on purpose: a syntax-ancestor ConditionalAccessExpressionSyntax lookup was
    // attempted for P3-a and reverted (Roslyn nests IConditionalAccessOperations without mirroring the nesting in
    // ConditionalAccessExpressionSyntax spans, so the syntax ancestor differs from the operation ancestor), and a
    // syntax-level nameof test would misclassify bare verbatim `@nameof` user methods.
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

    protected bool IsConditionalBinding(SyntaxNode node) => node switch
    {
        MemberBindingExpressionSyntax => true,
        MemberAccessExpressionSyntax access => IsConditionalBinding(access.Expression),
        _ => false,
    };

    /// <summary>The conditional access operation enclosing <paramref name="owner"/>'s expression.</summary>
    protected IConditionalAccessOperation ConditionalOf(IOperation owner)
    {
        IOperation current = owner;
        while (current is not IConditionalAccessOperation)
        {
            current = current.Parent ?? throw new InvalidOperationException("Conditional access instance outside a conditional access.");
        }
        return (IConditionalAccessOperation)current;
    }

    // NormalizedTextOf and TokenTextOf stay here on purpose (plan P6-c): Roslyn has no public parenthesis-walking
    // API, a static syntax utility class is banned by the audit's static rule, and MemberAccess is their only Data-layer caller.
    protected string NormalizedTextOf(SyntaxNode node)
    {
        SyntaxNode unwrapped = UnwrapParentheses(node);
        return unwrapped switch
        {
            PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } forgiven => NormalizedTextOf(forgiven.Operand),
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } ownMember => TokenTextOf(ownMember.Name),
            MemberAccessExpressionSyntax member => $"{NormalizedTextOf(member.Expression)}.{TokenTextOf(member.Name)}",
            _ => TokenTextOf(unwrapped),
        };
    }

    /// <summary>The expression inside the outermost chain of parentheses, or the node itself.</summary>
    private SyntaxNode UnwrapParentheses(SyntaxNode node)
    {
        SyntaxNode current = node;
        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }
        return current;
    }

    private string TokenTextOf(SyntaxNode node) => string.Concat(node.DescendantTokens().Select(token => token.Text));
}
