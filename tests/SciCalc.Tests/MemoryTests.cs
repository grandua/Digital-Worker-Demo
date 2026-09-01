using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public class MemoryTests
{
    [Theory]
    [InlineData(MemorySlotId.M1)]
    [InlineData(MemorySlotId.M2)]
    [InlineData(MemorySlotId.M3)]
    public void FreshSlotRecallsNothing(MemorySlotId slot)
    {
        MemoryBank memory = new();

        Assert.False(memory.IsNonEmpty(slot));
        Assert.Null(memory.Recall(slot));
    }

    [Theory]
    [InlineData(MemorySlotId.M1)]
    [InlineData(MemorySlotId.M2)]
    [InlineData(MemorySlotId.M3)]
    public void StoredValueIsRecalledUntilCleared(MemorySlotId slot)
    {
        MemoryBank memory = new();

        memory.Store(slot, 4.5);

        Assert.True(memory.IsNonEmpty(slot));
        Assert.Equal(4.5, memory.Recall(slot));
        memory.Clear(slot);
        Assert.False(memory.IsNonEmpty(slot));
        Assert.Null(memory.Recall(slot));
    }

    [Theory]
    [InlineData(InputKey.StoreM1, InputKey.RecallM1, InputKey.ClearM1, MemorySlotId.M1)]
    [InlineData(InputKey.StoreM2, InputKey.RecallM2, InputKey.ClearM2, MemorySlotId.M2)]
    [InlineData(InputKey.StoreM3, InputKey.RecallM3, InputKey.ClearM3, MemorySlotId.M3)]
    public void StoreRecallClearRoundTripPerSlot(
        InputKey store, InputKey recall, InputKey clear, MemorySlotId slot)
    {
        Calculator calculator = new Calculator().PressAll("8=");

        calculator.Press(store);
        Assert.True(calculator.Memory.IsNonEmpty(slot));
        calculator.Press(InputKey.AllClear);
        calculator.Press(recall);
        Assert.Equal("8", calculator.Buffer.Text());
        AssertPreview(calculator, 8);
        calculator.Press(clear);
        Assert.False(calculator.Memory.IsNonEmpty(slot));
        Assert.Null(calculator.Memory.Recall(slot));
    }

    [Fact]
    public void MemorySlotsAreIndependent()
    {
        Calculator calculator = new Calculator().PressAll("8=");
        calculator.Press(InputKey.StoreM1);
        calculator.PressAll("9=");
        calculator.Press(InputKey.StoreM2);

        calculator.Press(InputKey.ClearM1);

        Assert.False(calculator.Memory.IsNonEmpty(MemorySlotId.M1));
        Assert.Null(calculator.Memory.Recall(MemorySlotId.M1));
        Assert.True(calculator.Memory.IsNonEmpty(MemorySlotId.M2));
        Assert.Equal(9.0, calculator.Memory.Recall(MemorySlotId.M2));
        Assert.False(calculator.Memory.IsNonEmpty(MemorySlotId.M3));
    }

    [Theory]
    [InlineData(InputKey.RecallM1)]
    [InlineData(InputKey.RecallM2)]
    [InlineData(InputKey.RecallM3)]
    public void RecallFromEmptySlotIsIgnored(InputKey recall)
    {
        Calculator calculator = new Calculator();

        calculator.Press(recall);

        Assert.Empty(calculator.Buffer.Tokens);
        Assert.Null(calculator.Preview);
    }

    [Fact]
    public void RecallInsertsValueIntoCurrentExpression()
    {
        Calculator calculator = new Calculator().PressAll("8=");
        calculator.Press(InputKey.StoreM1);
        calculator.PressAll("2+");

        calculator.Press(InputKey.RecallM1);

        AssertPreview(calculator, 10);
    }

    [Fact]
    public void StoreWithoutAnswerEvaluatesCurrentBuffer()
    {
        Calculator calculator = new Calculator().PressAll("2+3");

        calculator.Press(InputKey.StoreM1);

        Assert.Equal(5.0, calculator.Memory.Recall(MemorySlotId.M1));
    }

    [Fact]
    public void StoreWithCurrentBufferPrefersPreviewOverLastAnswer()
    {
        Calculator calculator = new Calculator().PressAll("8=");
        calculator.PressAll("2+3");

        calculator.Press(InputKey.StoreM1);

        Assert.Equal(5.0, calculator.Memory.Recall(MemorySlotId.M1));
    }

    [Theory]
    [InlineData(InputKey.StoreM1, MemorySlotId.M1)]
    [InlineData(InputKey.StoreM2, MemorySlotId.M2)]
    [InlineData(InputKey.StoreM3, MemorySlotId.M3)]
    public void StoreWithIncompleteBufferFallsBackToLastAnswer(InputKey store, MemorySlotId slot)
    {
        Calculator calculator = new Calculator().PressAll("8=");
        calculator.PressAll("5+");

        Assert.Null(calculator.Preview);

        calculator.Press(store);

        Assert.Equal(8.0, calculator.Memory.Recall(slot));
        Assert.Equal("5+", calculator.Buffer.Text());
    }

    [Fact]
    public void StoringDoesNotChangeBufferOrPreview()
    {
        Calculator calculator = new Calculator().PressAll("2+3");

        calculator.Press(InputKey.StoreM2);

        Assert.Equal("2+3", calculator.Buffer.Text());
        AssertPreview(calculator, 5);
    }

    private static void AssertPreview(Calculator calculator, double expected)
    {
        Assert.NotNull(calculator.Preview);
        TestTokens.AssertClose(CalculationResult.Ok(calculator.Preview.Value), expected);
    }
}
