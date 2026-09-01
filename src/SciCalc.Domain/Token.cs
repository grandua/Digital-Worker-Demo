namespace SciCalc.Domain;

public readonly record struct Token(
    TokenKind Kind,
    double? NumericValue,
    OperatorKind? OperatorKind,
    FunctionKind? FunctionKind,
    ConstantKind? ConstantKind)
{
    public static Token Number(double value) => new(TokenKind.Number, value, null, null, null);

    public static Token Operator(OperatorKind kind) => new(TokenKind.Operator, null, kind, null, null);

    public static Token Function(FunctionKind kind) => new(TokenKind.Function, null, null, kind, null);

    public static Token OpenParen() => new(TokenKind.OpenParen, null, null, null, null);

    public static Token CloseParen() => new(TokenKind.CloseParen, null, null, null, null);

    public static Token Percent() => new(TokenKind.Percent, null, null, null, null);

    public static Token Constant(ConstantKind kind, double value) => new(TokenKind.Constant, value, null, null, kind);
}
