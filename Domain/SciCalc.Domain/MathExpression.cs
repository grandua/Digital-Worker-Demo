namespace SciCalc.Domain;

public sealed class MathExpression(IReadOnlyList<Token> tokens)
{
    public CalculationResult Evaluate(AngleMode mode)
    {
        try
        {
            return EvaluateParsed(mode);
        }
        catch (ParseFailure)
        {
            return CalculationResult.Fail(CalcError.Malformed);
        }
    }

    private CalculationResult EvaluateParsed(AngleMode mode) =>
        Normalize(new Parser(tokens).Parse().EvaluateNode(mode));

    private CalculationResult Normalize(CalculationResult result)
    {
        if (result.HasError) return result;
        double value = result.Value!.Value;
        return double.IsNaN(value) || double.IsInfinity(value)
            ? CalculationResult.Fail(CalcError.Overflow)
            : result;
    }

    private sealed class ParseFailure() : Exception;

    private sealed class Parser(IReadOnlyList<Token> tokens)
    {
        private int position;

        private Token Current => tokens[position];

        private bool AtEnd => position >= tokens.Count;

        public Node Parse()
        {
            Node root = ParseExpression();
            return AtEnd ? root : throw new ParseFailure();
        }

        private Node ParseExpression() =>
            ParseBinaryLevel(ParseTerm, OperatorKind.Add, OperatorKind.Subtract);

        private Node ParseTerm() =>
            ParseBinaryLevel(ParseUnary, OperatorKind.Multiply, OperatorKind.Divide, OperatorKind.Modulo);

        private Node ParseBinaryLevel(Func<Node> parseNext, params OperatorKind[] operators)
        {
            Node left = parseNext();
            while (TakeAny(operators) is { } taken)
            {
                Node right = parseNext();
                left = new BinaryNode(taken, left, ResolvePercent(taken, left, right));
            }
            return left;
        }

        private Node ResolvePercent(OperatorKind op, Node left, Node right) =>
            op is OperatorKind.Add or OperatorKind.Subtract
            && right is PercentNode { Previous: null } bare
                ? new PercentNode(bare.Inner, left)
                : right;

        private Node ParseUnary()
        {
            if (!At(OperatorKind.Subtract)) return ParsePower();
            position++;
            return new UnaryMinusNode(ParseUnary());
        }

        private Node ParsePower()
        {
            Node baseNode = ParsePostfix();
            if (!At(OperatorKind.Power)) return baseNode;
            position++;
            return new BinaryNode(OperatorKind.Power, baseNode, ParseUnary());
        }

        private Node ParsePostfix()
        {
            Node node = ParsePrimary();
            while (!AtEnd && IsPostfix(Current))
                node = ApplyPostfix(Current, node);
            return node;
        }

        private bool IsPostfix(Token token) =>
            token.Kind == TokenKind.Percent
            || (token.Kind == TokenKind.Function && token.FunctionKind == FunctionKind.Factorial);

        private Node ApplyPostfix(Token postfix, Node operand)
        {
            position++;
            return postfix.Kind == TokenKind.Percent
                ? new PercentNode(operand, null)
                : new FunctionNode(postfix.FunctionKind!.Value, operand);
        }

        private Node ParsePrimary()
        {
            if (AtEnd) throw new ParseFailure();
            Token current = Take();
            return current.Kind switch
            {
                TokenKind.Number or TokenKind.Constant => new NumberNode(current.NumericValue!.Value),
                TokenKind.Function => ParseFunctionCall(current.FunctionKind!.Value),
                TokenKind.OpenParen => ParseParenthesized(),
                _ => throw new ParseFailure(),
            };
        }

        private Token Take()
        {
            Token current = Current;
            position++;
            return current;
        }

        private Node ParseFunctionCall(FunctionKind kind)
        {
            if (AtEnd || Current.Kind != TokenKind.OpenParen) throw new ParseFailure();
            Take();
            return new FunctionNode(kind, ParseParenthesized());
        }

        private Node ParseParenthesized()
        {
            Node inner = ParseExpression();
            if (AtEnd || Current.Kind != TokenKind.CloseParen) throw new ParseFailure();
            Take();
            return inner;
        }

        private bool At(OperatorKind kind) =>
            !AtEnd && Current.Kind == TokenKind.Operator && Current.OperatorKind == kind;

        private OperatorKind? TakeAny(OperatorKind[] candidates)
        {
            OperatorKind? taken = candidates
                .Select(kind => At(kind) ? (OperatorKind?)kind : null)
                .FirstOrDefault(candidate => candidate is not null);
            if (taken is not null) position++;
            return taken;
        }
    }
}
