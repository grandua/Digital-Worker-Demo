namespace SciCalc;

public class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage())
        {
            Title = "SciCalc",
            Width = 1000,
            Height = 780,
        };
    }
}
