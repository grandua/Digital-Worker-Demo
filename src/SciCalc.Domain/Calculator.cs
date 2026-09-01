namespace SciCalc.Domain;

public sealed class Calculator
{
    private readonly List<HistoryEntry> _history = [];
    private readonly Dictionary<InputKey, Token> _keyTokens = new()
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
        [InputKey.Sub] = Token.Operator(OperatorKind.Sub),
        [InputKey.Mul] = Token.Operator(OperatorKind.Mul),
        [InputKey.Div] = Token.Operator(OperatorKind.Div),
        [InputKey.Pow] = Token.Operator(OperatorKind.Pow),
        [InputKey.Mod] = Token.Operator(OperatorKind.Mod),
        [InputKey.OpenParen] = Token.OpenParen(),
        [InputKey.CloseParen] = Token.CloseParen(),
        [InputKey.Percent] = Token.Percent(),
        [InputKey.Pi] = Token.Constant(ConstantKind.Pi, Math.PI),
        [InputKey.E] = Token.Constant(ConstantKind.E, Math.E),
    };
    private readonly Dictionary<InputKey, FunctionKind> _functionKeys = new()
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

    public InputBuffer Buffer { get; } = new();

    public MemoryBank Memory { get; } = new();

    public IReadOnlyList<HistoryEntry> History => _history;

    public AngleMode Mode { get; private set; } = AngleMode.Radians;

    public double? LastAnswer { get; private set; }

    public CalcError? ActiveError { get; private set; }

    public bool Locked { get; private set; }

    public double? Preview { get; private set; }

    public void Press(InputKey key)
    {
        if (Locked) { HandleLockedPress(key); return; }
        if (HandleCommand(key)) return;
        AppendKey(key);
        RefreshPreview();
    }

    public void ToggleAngleMode()
    {
        Mode = Mode == AngleMode.Radians ? AngleMode.Degrees : AngleMode.Radians;
        RefreshPreview();
    }

    public void RestoreHistory(HistoryEntry entry)
    {
        if (Locked) return;
        Buffer.Clear();
        foreach (Token token in entry.Tokens)
            Buffer.Add(token);
        RefreshPreview();
    }

    private void HandleLockedPress(InputKey key)
    {
        if (key == InputKey.AllClear) ResetSession();
    }

    private void ResetSession()
    {
        Buffer.Clear();
        Locked = false;
        ActiveError = null;
        Preview = null;
    }

    private bool HandleCommand(InputKey key)
    {
        if (key == InputKey.AllClear) { ResetSession(); return true; }
        if (key == InputKey.Eq) { EvaluateEquals(); return true; }
        if (key == InputKey.DegRadToggle) { ToggleAngleMode(); return true; }
        if (key == InputKey.Delete) { Buffer.RemoveLastToken(); RefreshPreview(); return true; }
        return HandleMemoryCommand(key);
    }

    private bool HandleMemoryCommand(InputKey key)
    {
        if (IsSlotKey(key, InputKey.StoreM1, InputKey.StoreM3)) { StoreMemory(SlotOf(key)); return true; }
        if (IsSlotKey(key, InputKey.RecallM1, InputKey.RecallM3)) { RecallMemory(SlotOf(key)); return true; }
        if (IsSlotKey(key, InputKey.ClearM1, InputKey.ClearM3)) { Memory.Clear(SlotOf(key)); return true; }
        return false;
    }

    private bool IsSlotKey(InputKey key, InputKey first, InputKey last) => key >= first && key <= last;

    private MemorySlotId SlotOf(InputKey key) => key switch
    {
        InputKey.StoreM1 or InputKey.RecallM1 or InputKey.ClearM1 => MemorySlotId.M1,
        InputKey.StoreM2 or InputKey.RecallM2 or InputKey.ClearM2 => MemorySlotId.M2,
        _ => MemorySlotId.M3,
    };

    private void AppendKey(InputKey key)
    {
        if (key == InputKey.Ans) { InsertAnswer(); return; }
        if (_keyTokens.TryGetValue(key, out Token token)) { Buffer.Add(token); return; }
        if (_functionKeys.TryGetValue(key, out FunctionKind function)) AppendFunction(function);
    }

    private void AppendFunction(FunctionKind function)
    {
        Buffer.Add(Token.Function(function));
        if (function != FunctionKind.Factorial) Buffer.Add(Token.OpenParen());
    }

    private void InsertAnswer()
    {
        if (LastAnswer is { } answer) Buffer.Add(Token.Number(answer));
    }

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
        RefreshPreview();
    }

    private void EvaluateEquals()
    {
        CalculationResult result = EvaluateBuffer();
        if (result.HasError) { FailWith(result.Error!.Value); return; }
        LastAnswer = result.Value;
        Preview = result.Value;
        PushHistory(result.Value!.Value);
        Buffer.Clear();
    }

    private void FailWith(CalcError error)
    {
        ActiveError = error;
        Locked = true;
        Preview = null;
    }

    private void PushHistory(double value)
    {
        HistoryEntry entry = new(Buffer.Text(), value, Buffer.Tokens.ToList(), DateTime.UtcNow);
        _history.Add(entry);
        if (_history.Count > 10) _history.RemoveAt(0);
    }

    private void RefreshPreview()
    {
        CalculationResult result = EvaluateBuffer();
        Preview = result.HasError ? null : result.Value;
    }

    private CalculationResult EvaluateBuffer() =>
        new MathExpression(Buffer.Tokens).Evaluate(new EvaluationContext(Mode, LastAnswer));
}
