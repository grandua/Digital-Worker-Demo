using Microsoft.CodeAnalysis;

namespace Encapsulate.Data;

/// <summary>A member access whose receiver is a conditional access (`o?.B.Total` reads through `o?.B`).</summary>
public sealed class ConditionalMemberAccess(IOperation operation) : MemberAccess(operation)
{
    protected override SyntaxNode ReceiverStart => ConditionalOf(Member).Operation.Syntax;

    public override string ReceiverKey => NormalizedTextOf(ConditionalOf(Member).Operation.Syntax);

    internal override bool JoinsConditionally => true;
}
