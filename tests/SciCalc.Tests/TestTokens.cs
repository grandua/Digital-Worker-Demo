using System.Globalization;
using SciCalc.Domain;
using Xunit;

namespace SciCalc.Tests;

public static class TestTokens
{
    public static MathExpression Parse(string source) => new(Tokenize(source));

    public static void AssertClose(CalculationResult result, double expected)
    {
        Assert.False(result.HasError, $"expected {expected} but got error {result.Error}");
        Assert.True(
            Math.Abs(result.Value!.Value - expected) < 1e-9,
            $"expected {expected} but got {result.Value!.Value}");
    }

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
        if (char.IsLetter(source[index]))
            return AppendWord(source, index, tokens);
        return AppendSymbol(source[index], index, tokens);
    }

    private static int AppendWord(string source, int index, List<Token> tokens)
    {
        int end = index;
        while (end < source.Length && char.IsLetter(source[end]))
            end++;
        string word = source[index..end];
        if (!Words.TryGetValue(word, out Token token))
            throw new FormatException($"Unexpected word '{word}' at {index}");
        tokens.Add(token);
        return end;
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
        else if (Symbols.TryGetValue(symbol, out Token single))
            tokens.Add(single);
        else
            throw new FormatException($"Unexpected character '{symbol}' at {index}");
        return index + 1;
    }

    private static readonly Dictionary<char, OperatorKind> Operators = new()
    {
        ['+'] = OperatorKind.Add,
        ['-'] = OperatorKind.Subtract,
        ['*'] = OperatorKind.Multiply,
        ['/'] = OperatorKind.Divide,
        ['^'] = OperatorKind.Power,
    };

    private static readonly Dictionary<char, Token> Symbols = new()
    {
        ['('] = Token.OpenParen(),
        [')'] = Token.CloseParen(),
        ['%'] = Token.Percent(),
        ['!'] = Token.Function(FunctionKind.Factorial),
        ['√'] = Token.Function(FunctionKind.Sqrt),
        ['∛'] = Token.Function(FunctionKind.Cbrt),
    };

    private static readonly Dictionary<string, Token> Words = new()
    {
        ["mod"] = Token.Operator(OperatorKind.Modulo),
        ["π"] = Token.Constant(ConstantKind.Pi, Math.PI),
        ["e"] = Token.Constant(ConstantKind.E, Math.E),
        ["sin"] = Token.Function(FunctionKind.Sin),
        ["cos"] = Token.Function(FunctionKind.Cos),
        ["tan"] = Token.Function(FunctionKind.Tan),
        ["asin"] = Token.Function(FunctionKind.Asin),
        ["acos"] = Token.Function(FunctionKind.Acos),
        ["atan"] = Token.Function(FunctionKind.Atan),
        ["sinh"] = Token.Function(FunctionKind.Sinh),
        ["cosh"] = Token.Function(FunctionKind.Cosh),
        ["tanh"] = Token.Function(FunctionKind.Tanh),
        ["log"] = Token.Function(FunctionKind.Log10),
        ["ln"] = Token.Function(FunctionKind.Ln),
        ["exp"] = Token.Function(FunctionKind.Exp),
        ["tenpow"] = Token.Function(FunctionKind.TenPow),
        ["sqr"] = Token.Function(FunctionKind.Square),
        ["cube"] = Token.Function(FunctionKind.Cube),
        ["sqrt"] = Token.Function(FunctionKind.Sqrt),
        ["cbrt"] = Token.Function(FunctionKind.Cbrt),
        ["abs"] = Token.Function(FunctionKind.Abs),
        ["recip"] = Token.Function(FunctionKind.Reciprocal),
    };
}
