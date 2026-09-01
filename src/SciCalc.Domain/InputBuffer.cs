using System.Globalization;

namespace SciCalc.Domain;

public sealed class InputBuffer
{
    // TODO(smell): LOW — underscore field names (_tokens, _numberText).
    private readonly List<Token> _tokens = [];
    private readonly Dictionary<OperatorKind, string> operatorText = new()
    {
        [OperatorKind.Add] = "+",
        [OperatorKind.Subtract] = "-",
        [OperatorKind.Multiply] = "*",
        [OperatorKind.Divide] = "/",
        [OperatorKind.Power] = "^",
        [OperatorKind.Modulo] = "mod",
    };
    private readonly Dictionary<FunctionKind, string> functionText = new()
    {
        [FunctionKind.Sin] = "sin",
        [FunctionKind.Cos] = "cos",
        [FunctionKind.Tan] = "tan",
        [FunctionKind.Asin] = "asin",
        [FunctionKind.Acos] = "acos",
        [FunctionKind.Atan] = "atan",
        [FunctionKind.Sinh] = "sinh",
        [FunctionKind.Cosh] = "cosh",
        [FunctionKind.Tanh] = "tanh",
        [FunctionKind.Log10] = "log",
        [FunctionKind.Ln] = "ln",
        [FunctionKind.Exp] = "exp",
        [FunctionKind.TenPow] = "tenpow",
        [FunctionKind.Square] = "sqr",
        [FunctionKind.Cube] = "cube",
        [FunctionKind.Sqrt] = "sqrt",
        [FunctionKind.Cbrt] = "cbrt",
        [FunctionKind.Abs] = "abs",
        [FunctionKind.Reciprocal] = "recip",
    };
    private readonly Dictionary<ConstantKind, string> constantText = new()
    {
        [ConstantKind.Pi] = "π",
        [ConstantKind.E] = "e",
    };
    private string? _numberText;

    public IReadOnlyList<Token> Tokens => _tokens.AsReadOnly();

    // TODO(review): MEDIUM - derive overflow from number text; mutable cached state becomes stale after RemoveLastToken.
    public bool HasLiteralOverflow { get; private set; }

    public string? EditingNumber => _numberText;

    public void Add(Token token)
    {
        switch (token.Kind)
        {
            case TokenKind.Digit:
                AppendDigit(token.NumericValue!.Value);
                return;
            case TokenKind.Dot:
                AppendDot();
                return;
            default:
                AppendOther(token);
                return;
        }
    }

    public void RemoveLastToken()
    {
        if (_tokens.Count == 0) return;
        _tokens.RemoveAt(_tokens.Count - 1);
        _numberText = _tokens.Count > 0 && _tokens[^1].Kind == TokenKind.Number
            ? _tokens[^1].NumericValue!.Value.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    public void Clear()
    {
        _tokens.Clear();
        _numberText = null;
        HasLiteralOverflow = false;
    }

    public string Text() => string.Concat(_tokens.Select((token, index) => Render(token, index)));

    private string Render(Token token, int index) => token.Kind switch
    {
        TokenKind.Number => RenderNumber(token, index),
        TokenKind.Operator => operatorText[token.OperatorKind!.Value],
        TokenKind.Function => FunctionNeutralText(token.FunctionKind!.Value),
        TokenKind.OpenParen => "(",
        TokenKind.CloseParen => ")",
        TokenKind.Percent => "%",
        TokenKind.Constant => constantText[token.ConstantKind!.Value],
        _ => string.Empty,
    };

    private string FunctionNeutralText(FunctionKind kind) =>
        kind == FunctionKind.Factorial ? "!" : functionText[kind];

    private string RenderNumber(Token token, int index) =>
        index == _tokens.Count - 1 && _numberText is not null
            ? _numberText
            : token.NumericValue!.Value.ToString(CultureInfo.InvariantCulture);

    private void AppendOther(Token token)
    {
        _numberText = null;
        _tokens.Add(token);
    }

    private void AppendDigit(double digit)
    {
        bool replace = _numberText is not null;
        _numberText = (_numberText ?? string.Empty) + ((int)digit).ToString(CultureInfo.InvariantCulture);
        SyncNumberToken(replace);
    }

    private void AppendDot()
    {
        bool replace = _numberText is not null;
        if (_numberText is null) _numberText = "0.";
        else if (!_numberText.Contains('.')) _numberText += '.';
        SyncNumberToken(replace);
    }

    private void SyncNumberToken(bool replaceLast)
    {
        if (!TryParseFinite(_numberText!, out double value))
        {
            HasLiteralOverflow = true;
            return;
        }
        HasLiteralOverflow = false;
        if (replaceLast) _tokens[^1] = Token.Number(value);
        else _tokens.Add(Token.Number(value));
    }

    private bool TryParseFinite(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && double.IsFinite(value);
}