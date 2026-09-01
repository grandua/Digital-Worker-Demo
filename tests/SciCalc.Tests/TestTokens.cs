using System.Globalization;
using SciCalc.Domain;

namespace SciCalc.Tests;

public static class TestTokens
{
    public static MathExpression Parse(string source) => new(Tokenize(source));

    private static IReadOnlyList<Token> Tokenize(string source)
    {
        List<Token> tokens = [];
        for (int index = 0; index < source.Length;)
        {
            if (char.IsWhiteSpace(source[index]))
            {
                index++;
                continue;
            }
            index = AppendToken(source, index, tokens);
        }
        return tokens;
    }

    private static int AppendToken(string source, int index, List<Token> tokens)
    {
        if (char.IsDigit(source[index]) || source[index] == '.')
            return AppendNumber(source, index, tokens);
        if (source[index] == 'm')
        {
            tokens.Add(Token.Operator(OperatorKind.Mod));
            return index + 3;
        }
        return AppendSymbol(source[index], index, tokens);
    }

    private static int AppendNumber(string source, int index, List<Token> tokens)
    {
        int start = index;
        while (index < source.Length && (char.IsDigit(source[index]) || source[index] == '.'))
            index++;
        tokens.Add(Token.Number(double.Parse(source[start..index], CultureInfo.InvariantCulture)));
        return index;
    }

    private static int AppendSymbol(char symbol, int index, List<Token> tokens)
    {
        if (Operators.TryGetValue(symbol, out OperatorKind op))
            tokens.Add(Token.Operator(op));
        else if (Constants.TryGetValue(symbol, out (ConstantKind Kind, double Value) constant))
            tokens.Add(Token.Constant(constant.Kind, constant.Value));
        else if (Parentheses.TryGetValue(symbol, out Token paren))
            tokens.Add(paren);
        else
            throw new FormatException($"Unexpected character '{symbol}' at {index}");
        return index + 1;
    }

    private static readonly Dictionary<char, OperatorKind> Operators = new()
    {
        ['+'] = OperatorKind.Add,
        ['-'] = OperatorKind.Sub,
        ['*'] = OperatorKind.Mul,
        ['/'] = OperatorKind.Div,
        ['^'] = OperatorKind.Pow,
    };

    private static readonly Dictionary<char, (ConstantKind Kind, double Value)> Constants = new()
    {
        ['π'] = (ConstantKind.Pi, Math.PI),
        ['e'] = (ConstantKind.E, Math.E),
    };

    private static readonly Dictionary<char, Token> Parentheses = new()
    {
        ['('] = Token.OpenParen(),
        [')'] = Token.CloseParen(),
    };
}
