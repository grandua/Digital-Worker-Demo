using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Encapsulate.Domain;

namespace Encapsulate.Data;

/// <summary>A member access on the containing instance: an explicit or implicit `this` receiver, or `base`.</summary>
public sealed class ContainingInstanceMemberAccess(IOperation operation) : MemberAccess(operation)
{
    public override string ReceiverKey =>
        Member.Instance!.Syntax is BaseExpressionSyntax ? Receiver.BaseKey : Receiver.ThisKey;
}
