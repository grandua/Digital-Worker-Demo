namespace Encapsulate.Domain;

/// <summary>One counted member access as a plain position fact: the zero-based line and column of its receiver start.</summary>
public sealed record ReceiverAccess(int Line, int Column);
