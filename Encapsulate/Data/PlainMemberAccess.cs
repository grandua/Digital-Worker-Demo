using Microsoft.CodeAnalysis;
namespace Encapsulate.Data;

/// <summary>A member access with any other receiver kind: a local, parameter, invocation result or implicit initializer instance.</summary>
public sealed class PlainMemberAccess(IOperation operation) : MemberAccess(operation);
