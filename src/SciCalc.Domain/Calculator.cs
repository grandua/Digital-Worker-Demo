namespace SciCalc.Domain;

public sealed class Calculator
{
    private const int MaxHistoryEntries = 10;
    private readonly List<HistoryEntry> history = [];
    private readonly Dictionary<InputKey, Token> keyTokens = new()
    {
        [InputKey.Digit0] = Token.Digit(0),
        [InputKey.Digit1] = Token.Digit(1),
        [InputKey.Digit2] = Token.Digit(2),
        [InputKey.Digit3] = Token.Digit(3),
        [InputKey.Digit4] = Token.Digit(4),
        [InputKey.Digit5] = Token.Digit(5),
        [InputKey.Digit6] = Token.Digit(6),
        [InputKey.Digit7] = Token.Digit(7),
        [InputKey.Digit8] = Token.Digit(8),
        [InputKey.Digit9] = Token.Digit(9),
        [InputKey.Dot] = Token.Dot(),
        [InputKey.Add] = Token.Operator(OperatorKind.Add),
        [InputKey.Subtract] = Token.Operator(OperatorKind.Subtract),
        [InputKey.Multiply] = Token.Operator(OperatorKind.Multiply),
        [InputKey.Divide] = Token.Operator(OperatorKind.Divide),
        [InputKey.Power] = Token.Operator(OperatorKind.Power),
        [InputKey.Modulo] = Token.Operator(OperatorKind.Modulo),
        [InputKey.OpenParen] = Token.OpenParen(),
        [InputKey.CloseParen] = Token.CloseParen(),
        [InputKey.Percent] = Token.Percent(),
        [InputKey.Pi] = Token.Constant(ConstantKind.Pi, Math.PI),
        [InputKey.E] = Token.Constant(ConstantKind.E, Math.E),
    };
    private readonly Dictionary<InputKey, FunctionKind> functionKeys = new()
    {
        [InputKey.Sin] = FunctionKind.Sin,
        [InputKey.Cos] = FunctionKind.Cos,
        [InputKey.Tan] = FunctionKind.Tan,
        [InputKey.Asin] = FunctionKind.Asin,
        [InputKey.Acos] = FunctionKind.Acos,
        [InputKey.Atan] = FunctionKind.Atan,
        [InputKey.Sinh] = FunctionKind.Sinh,
        [InputKey.Cosh] = FunctionKind.Cosh,
        [InputKey.Tanh] = FunctionKind.Tanh,
        [InputKey.Log10] = FunctionKind.Log10,
        [InputKey.Ln] = FunctionKind.Ln,
        [InputKey.Exp] = FunctionKind.Exp,
        [InputKey.TenPow] = FunctionKind.TenPow,
        [InputKey.Square] = FunctionKind.Square,
        [InputKey.Cube] = FunctionKind.Cube,
        [InputKey.Sqrt] = FunctionKind.Sqrt,
        [InputKey.Cbrt] = FunctionKind.Cbrt,
        [InputKey.Factorial] = FunctionKind.Factorial,
        [InputKey.Abs] = FunctionKind.Abs,
        [InputKey.Reciprocal] = FunctionKind.Reciprocal,
    };

    private readonly Dictionary<InputKey, MemorySlotId> storeKeys = new()
    {
        [InputKey.StoreM1] = MemorySlotId.M1,
        [InputKey.StoreM2] = MemorySlotId.M2,
        [InputKey.StoreM3] = MemorySlotId.M3,
    };
    private readonly Dictionary<InputKey, MemorySlotId> recallKeys = new()
    {
        [InputKey.RecallM1] = MemorySlotId.M1,
        [InputKey.RecallM2] = MemorySlotId.M2,
        [InputKey.RecallM3] = MemorySlotId.M3,
    };
    private readonly Dictionary<InputKey, MemorySlotId> clearKeys = new()
    {
        [InputKey.ClearM1] = MemorySlotId.M1,
        [InputKey.ClearM2] = MemorySlotId.M2,
        [InputKey.ClearM3] = MemorySlotId.M3,
    };

    public InputBuffer Buffer { get; } = new();

    public MemoryBank Memory { get; } = new();

    public IReadOnlyList<HistoryEntry> History => history.AsReadOnly();

    public AngleMode Mode { get; private set; } = AngleMode.Radians;

    public double? LastAnswer => History.Count > 0 ? History[^1].ResultValue : null;

    public CalcError? ActiveError { get; private set; }

    public bool Locked => ActiveError is not null;

    public double? Preview => ActiveError is not null
        ? null
        : Buffer.Tokens.Count == 0 ? LastAnswer : LivePreview();

    public void Press(InputKey key)
    {
        if (Locked) { HandleLockedPress(key); return; }
        if (HandleCommand(key)) return;
        AppendKey(key);
        if (Buffer.HasLiteralOverflow) FailWith(CalcError.Overflow);
    }

    public void ToggleAngleMode() =>
        Mode = Mode == AngleMode.Radians ? AngleMode.Degrees : AngleMode.Radians;

    public void RestoreHistory(HistoryEntry entry)
    {
        if (Locked) return;
        Buffer.Clear();
        foreach (Token token in entry.Tokens) Buffer.Add(token);
    }

    private void HandleLockedPress(InputKey key)
    {
        if (key == InputKey.AllClear) ResetSession();
    }

    private void ResetSession()
    {
        Buffer.Clear();
        ActiveError = null;
    }

    private bool HandleCommand(InputKey key)
    {
        if (key == InputKey.AllClear) { ResetSession(); return true; }
        if (key == InputKey.Eq) { EvaluateEquals(); return true; }
        if (key == InputKey.DegRadToggle) { ToggleAngleMode(); return true; }
        if (key == InputKey.Delete) { Buffer.RemoveLastToken(); return true; }
        return HandleMemoryCommand(key);
    }

    private bool HandleMemoryCommand(InputKey key)
    {
        if (storeKeys.TryGetValue(key, out MemorySlotId store)) { StoreMemory(store); return true; }
        if (recallKeys.TryGetValue(key, out MemorySlotId recall)) { RecallMemory(recall); return true; }
        if (clearKeys.TryGetValue(key, out MemorySlotId clear)) { Memory.Clear(clear); return true; }
        return false;
    }

    private void AppendKey(InputKey key)
    {
        if (key == InputKey.Ans) { InsertAnswer(); return; }
        if (keyTokens.TryGetValue(key, out Token token)) { Buffer.Add(token); return; }
        if (functionKeys.TryGetValue(key, out FunctionKind function)) AppendFunction(function);
    }

    private void AppendFunction(FunctionKind function)
    {
        if (IsPostfixWrapKey(function)) { WrapBufferInFunction(function); return; }
        Buffer.Add(Token.Function(function));
        if (function != FunctionKind.Factorial) Buffer.Add(Token.OpenParen());
    }

    private bool IsPostfixWrapKey(FunctionKind function) =>
        function is FunctionKind.Square or FunctionKind.Cube or FunctionKind.Sqrt
            or FunctionKind.Cbrt or FunctionKind.Reciprocal or FunctionKind.Exp
            or FunctionKind.TenPow or FunctionKind.Abs;

    private void WrapBufferInFunction(FunctionKind function)
    {
        Token[] inner = [.. Buffer.Tokens];
        Buffer.Clear();
        Buffer.Add(Token.Function(function));
        Buffer.Add(Token.OpenParen());
        foreach (Token token in inner.Append(Token.CloseParen())) Buffer.Add(token);
    }

    private void InsertAnswer() => Buffer.Add(Token.Number(LastAnswer ?? 0));

    private void StoreMemory(MemorySlotId slot)
    {
        if (LastAnswer is { } answer) { Memory.Store(slot, answer); return; }
        CalculationResult current = EvaluateBuffer();
        if (!current.HasError) Memory.Store(slot, current.Value!.Value);
    }

    private void RecallMemory(MemorySlotId slot)
    {
        if (Memory.Recall(slot) is not { } value) return;
        Buffer.Add(Token.Number(value));
    }

    private void EvaluateEquals()
    {
        CalculationResult result = EvaluateBuffer();
        if (result.HasError) { FailWith(result.Error!.Value); return; }
        PushHistory(result.Value!.Value);
        Buffer.Clear();
    }

    private void FailWith(CalcError error) => ActiveError = error;

    private void PushHistory(double value)
    {
        HistoryEntry entry = new(Buffer.Text(), value, Buffer.Tokens);
        history.Add(entry);
        if (history.Count > MaxHistoryEntries) history.RemoveAt(0);
    }

    private double? LivePreview()
    {
        CalculationResult result = EvaluateBuffer();
        return result.HasError ? null : result.Value;
    }

    private CalculationResult EvaluateBuffer() =>
        new MathExpression(Buffer.Tokens).Evaluate(Mode);
}
