namespace SciCalc.Domain;

public sealed record HistoryEntry
{
    public HistoryEntry(string expressionText, double resultValue, IEnumerable<Token> tokens)
    {
        ExpressionText = expressionText;
        ResultValue = resultValue;
        Tokens = [.. tokens];
    }

    public string ExpressionText { get; }

    public double ResultValue { get; }

    public IReadOnlyList<Token> Tokens { get; }
}
