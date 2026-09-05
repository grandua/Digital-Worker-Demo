using System.Xml.Linq;
using Xunit;

namespace SciCalc.Packaging.Tests;

public sealed class WindowsApplicationTests : ConformanceTests
{
    private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    private const string ClassDirective = "SciCalc.Platforms.Windows.App";

    private string CodeBehindSource => Repo.Read("src", "SciCalc", "Platforms", "Windows", "App.xaml.cs");

    [Fact]
    public void AppXaml_RootsMauiWinUIApplicationWithClassDirective()
    {
        XDocument xaml = Repo.Xml("src", "SciCalc", "Platforms", "Windows", "App.xaml");
        Assert.Equal("MauiWinUIApplication", xaml.Root!.Name.LocalName);
        Assert.Equal(ClassDirective, (string?)xaml.Root.Attribute(XName.Get("Class", XamlNamespace)));
    }

    [Fact]
    public void AppCodeBehind_DeclaresPartialAppAndCreatesMauiApp()
    {
        string source = CodeBehindSource;
        Assert.Contains("partial class App : MauiWinUIApplication", source);
        Assert.Contains("MauiProgram.CreateMauiApp()", source);
    }

    [Fact]
    public void AppCodeBehind_NeverUsesStandaloneMauiWinApplication()
    {
        Assert.DoesNotMatch(@"\bMauiWinApplication\b", CodeBehindSource);
    }
}
