namespace SciCalc.Domain;

public sealed class MathExpression(IReadOnlyList<Token> tokens)
{
    public CalculationResult Evaluate(EvaluationContext context)
    {
        try
        {
            Node root = new Parser(tokens).Parse();
            return Normalize(root.EvaluateNode(context));
        }
        catch (ParseFailure)
        {
            return CalculationResult.Fail(CalcError.Malformed);
        }
    }

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
        private int _position;

        private Token Current => tokens[_position];

        private bool AtEnd => _position >= tokens.Count;

        public Node Parse()
        {
            Node root = ParseExpression();
            return AtEnd ? root : throw new ParseFailure();
        }

        private Node ParseExpression() =>
            ParseBinaryLevel(ParseTerm, OperatorKind.Add, OperatorKind.Sub);

        private Node ParseTerm() =>
            ParseBinaryLevel(ParseUnary, OperatorKind.Mul, OperatorKind.Div, OperatorKind.Mod);

        private Node ParseBinaryLevel(Func<Node> parseNext, params OperatorKind[] operators)
        {
            Node left = parseNext();
            while (TryTakeAny(operators, out OperatorKind taken))
            {
                Node right = parseNext();
                left = new BinaryNode(taken, left, ResolvePercent(taken, left, right));
            }
            return left;
        }

        private Node ResolvePercent(OperatorKind op, Node left, Node right) =>
            op is OperatorKind.Add or OperatorKind.Sub
            && right is PercentNode { Previous: null } bare
                ? new PercentNode(bare.Inner, left)
                : right;

        private Node ParseUnary()
        {
            if (!At(OperatorKind.Sub)) return ParsePower();
            _position++;
            return new UnaryMinusNode(ParseUnary());
        }

        private Node ParsePower()
        {
            Node baseNode = ParsePostfix();
            if (!At(OperatorKind.Pow)) return baseNode;
            _position++;
            return new BinaryNode(OperatorKind.Pow, baseNode, ParseUnary());
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
            _position++;
            return postfix.Kind == TokenKind.Percent
                ? new PercentNode(operand, null)
                : new FunctionNode(postfix.FunctionKind!.Value, operand);
        }

        private Node ParsePrimary()
        {
            if (AtEnd) throw new ParseFailure();
            Token current = Current;
            _position++;
            return current.Kind switch
            {
                TokenKind.Number or TokenKind.Constant => new NumberNode(current.NumericValue!.Value),
                TokenKind.Function => ParseFunctionCall(current.FunctionKind!.Value),
                TokenKind.OpenParen => ParseParenthesized(),
                _ => throw new ParseFailure(),
            };
        }

        private Node ParseFunctionCall(FunctionKind kind)
        {
            if (AtEnd || Current.Kind != TokenKind.OpenParen) throw new ParseFailure();
            _position++;
            return new FunctionNode(kind, ParseParenthesized());
        }

        private Node ParseParenthesized()
        {
            Node inner = ParseExpression();
            if (AtEnd || Current.Kind != TokenKind.CloseParen) throw new ParseFailure();
            _position++;
            return inner;
        }

        private bool At(OperatorKind kind) =>
            !AtEnd && Current.Kind == TokenKind.Operator && Current.OperatorKind == kind;

        private bool TryTakeAny(OperatorKind[] candidates, out OperatorKind taken)
        {
            foreach (OperatorKind candidate in candidates)
            {
                if (!At(candidate)) continue;
                taken = Current.OperatorKind!.Value;
                _position++;
                return true;
            }
            taken = OperatorKind.Add;
            return false;
        }
    }
}
