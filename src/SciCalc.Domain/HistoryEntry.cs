namespace SciCalc.Domain;

public sealed record HistoryEntry(
    string ExpressionText,
    double ResultValue,
    IReadOnlyList<Token> Tokens,
    DateTime At);
