using Microsoft.CodeAnalysis;

namespace Encapsulate.Data;

/// <summary>A member access whose receiver is another member access (`o.B.Total` reads through `o.B`).</summary>
public sealed class ChainedMemberAccess(IOperation operation) : MemberAccess(operation)
{
    protected override SyntaxNode ReceiverStart
    {
        get
        {
            MemberAccess instance = For(Member.Instance!);
            return instance.JoinsConditionally || IsConditionalBinding(instance.MemberSyntax)
                ? ConditionalOf(instance.Source).Operation.Syntax
                : instance.MemberSyntax;
        }
    }

    // The receiver key of a chained access is the key of its receiver (plan R1), with the indexer shape checked first.
    public override string ReceiverKey
    {
        get
        {
            MemberAccess instance = For(Member.Instance!);
            return instance.IsIndexerAccess || !(instance.JoinsConditionally || IsConditionalBinding(instance.MemberSyntax))
                ? NormalizedTextOf(instance.MemberSyntax)
                : $"{instance.ReceiverKey}{(instance.JoinsConditionally ? "?" : string.Empty)}.{instance.MemberName}";
        }
    }
}
