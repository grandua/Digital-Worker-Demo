using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class HistoryTests
{
    [Fact]
    public void HistoryCapsAtTenEntriesOldestEvictedFirst()
    {
        Calculator calculator = new Calculator();

        for (int round = 1; round <= 12; round++)
            calculator.PressAll($"{round}+1=");

        Assert.Equal(10, calculator.History.Count);
        Assert.Equal("3+1", calculator.History[0].ExpressionText);
        Assert.Equal("12+1", calculator.History[^1].ExpressionText);
    }

    [Fact]
    public void RestoreHistoryReplacesBufferWithStoredTokens()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");
        HistoryEntry entry = Assert.Single(calculator.History);
        calculator.PressAll("9*9");

        calculator.RestoreHistory(entry);

        Assert.Equal("2+3", calculator.Buffer.Text());
        Assert.Equal(
            new Token[] { Token.Number(2), Token.Operator(OperatorKind.Add), Token.Number(3) },
            calculator.Buffer.Tokens);
        Assert.Equal(5.0, calculator.Preview);
    }

    [Fact]
    public void RestoreHistoryIsIgnoredWhileLocked()
    {
        Calculator calculator = new Calculator().PressAll("2+3=");
        HistoryEntry entry = Assert.Single(calculator.History);
        calculator.PressAll("1/0=");

        calculator.RestoreHistory(entry);

        Assert.Equal("1÷0", calculator.Buffer.Text());
        Assert.True(calculator.Locked);
    }

    [Fact]
    public void HistoryEntryRecordsEvaluationTimestamp()
    {
        DateTime before = DateTime.UtcNow;

        Calculator calculator = new Calculator().PressAll("1+1=");

        Assert.InRange(calculator.History[0].At, before, DateTime.UtcNow);
    }

    [Fact]
    public void FailedEqualsDoesNotAddHistory()
    {
        Calculator calculator = new Calculator().PressAll("1/0=");

        Assert.Empty(calculator.History);
    }
}
