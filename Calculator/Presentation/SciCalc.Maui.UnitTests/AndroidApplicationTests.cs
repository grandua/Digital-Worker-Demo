using Xunit;

namespace SciCalc.Maui.UnitTests;

public sealed class AndroidApplicationTests : ConformanceTests
{
    private const string MainApplicationPath = "Presentation/SciCalc.Maui/Platforms/Android/MainApplication.cs";

    private string MainApplicationSource => Repo.Read("Presentation", "SciCalc.Maui", "Platforms", "Android", "MainApplication.cs");

    [Fact]
    public void MainApplication_Exists()
    {
        Assert.True(Repo.File("Presentation", "SciCalc.Maui", "Platforms", "Android", "MainApplication.cs").Exists,
            $"'{MainApplicationPath}' is missing; the Android head needs MainApplication.");
    }

    [Fact]
    public void MainApplication_RegistersAppAndDelegatesToMauiProgram()
    {
        string source = MainApplicationSource;
        Assert.Contains("[Application]", source);
        Assert.Contains(": MauiApplication", source);
        Assert.Contains("MauiProgram.CreateMauiApp()", source);
    }

    [Fact]
    public void MainApplication_UsesRuntimeInitializationConstructor()
    {
        string source = MainApplicationSource;
        Assert.Matches(@"MainApplication\s*\(\s*IntPtr\s+\w+\s*,\s*JniHandleOwnership\s+\w+\s*\)", source);
    }
}
