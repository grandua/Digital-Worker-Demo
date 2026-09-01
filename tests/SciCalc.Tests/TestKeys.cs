using SciCalc.Domain;

namespace SciCalc.Tests;

public static class TestKeys
{
    public static Calculator PressAll(this Calculator calculator, string keys)
    {
        foreach (char key in keys)
            calculator.Press(ToKey(key));
        return calculator;
    }

    private static InputKey ToKey(char key) => key switch
    {
        '0' => InputKey.Digit0,
        '1' => InputKey.Digit1,
        '2' => InputKey.Digit2,
        '3' => InputKey.Digit3,
        '4' => InputKey.Digit4,
        '5' => InputKey.Digit5,
        '6' => InputKey.Digit6,
        '7' => InputKey.Digit7,
        '8' => InputKey.Digit8,
        '9' => InputKey.Digit9,
        '.' => InputKey.Dot,
        '+' => InputKey.Add,
        '-' => InputKey.Sub,
        '*' => InputKey.Mul,
        '/' => InputKey.Div,
        '^' => InputKey.Pow,
        '(' => InputKey.OpenParen,
        ')' => InputKey.CloseParen,
        '%' => InputKey.Percent,
        '=' => InputKey.Eq,
        'π' => InputKey.Pi,
        'e' => InputKey.E,
        _ => throw new FormatException($"No key maps to '{key}'"),
    };
}
