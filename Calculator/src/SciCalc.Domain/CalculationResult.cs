namespace SciCalc.Domain;

public readonly struct CalculationResult
{
    public double? Value { get; }

    public CalcError? Error { get; }

    public bool HasError => Error is not null;

    private CalculationResult(double? value, CalcError? error)
    {
        Value = value;
        Error = error;
    }

    public static CalculationResult Ok(double value) => new(value, null);

    public static CalculationResult Fail(CalcError error) => new(null, error);
}
