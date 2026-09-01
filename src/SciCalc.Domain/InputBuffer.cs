using System.Globalization;

namespace SciCalc.Domain;

public sealed class InputBuffer
{
    private readonly List<Token> _tokens = [];
    private readonly Dictionary<OperatorKind, string> _operatorNames = new()
    {
        [OperatorKind.Add] = "+",
        [OperatorKind.Sub] = "−",
        [OperatorKind.Mul] = "×",
        [OperatorKind.Div] = "÷",
        [OperatorKind.Pow] = "^",
        [OperatorKind.Mod] = "mod",
    };
    private readonly Dictionary<FunctionKind, string> _functionNames = new()
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
        [FunctionKind.Exp] = "e^",
        [FunctionKind.TenPow] = "10^",
        [FunctionKind.Square] = "sqr",
        [FunctionKind.Cube] = "cube",
        [FunctionKind.Sqrt] = "√",
        [FunctionKind.Cbrt] = "∛",
        [FunctionKind.Factorial] = "!",
        [FunctionKind.Abs] = "abs",
        [FunctionKind.Reciprocal] = "recip",
    };
    private readonly Dictionary<ConstantKind, string> _constantNames = new()
    {
        [ConstantKind.Pi] = "π",
        [ConstantKind.E] = "e",
    };
    private string? _numberText;

    public IReadOnlyList<Token> Tokens => _tokens;

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
        _numberText = null;
    }

    public void Clear()
    {
        _tokens.Clear();
        _numberText = null;
    }

    public string Text()
    {
        var builder = new System.Text.StringBuilder();
        for (int index = 0; index < _tokens.Count; index++)
            builder.Append(Render(_tokens[index], index));
        return builder.ToString();
    }

    private string Render(Token token, int index) => token.Kind switch
    {
        TokenKind.Number => RenderNumber(token, index),
        TokenKind.Operator => _operatorNames[token.OperatorKind!.Value],
        TokenKind.Function => _functionNames[token.FunctionKind!.Value],
        TokenKind.OpenParen => "(",
        TokenKind.CloseParen => ")",
        TokenKind.Percent => "%",
        TokenKind.Constant => _constantNames[token.ConstantKind!.Value],
        _ => string.Empty,
    };

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
        double value = double.Parse(_numberText!, CultureInfo.InvariantCulture);
        if (replaceLast) _tokens[^1] = Token.Number(value);
        else _tokens.Add(Token.Number(value));
    }
}
