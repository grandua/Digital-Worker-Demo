using Encapsulate.UnitTests.Support;

namespace Encapsulate.UnitTests.Integration;

public sealed class ProgramTests : TempDirectoryTests
{
    private const string SmellyClass =
        "public class Order { public int Total; public int Tax; }\n" +
        "public class Target\n{\n    public void M(Order o)\n    {\n        var a = o.Total + o.Tax;\n    }\n}\n";

    private RunResult Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exit = new Program().Run(args, output, error);
        return new RunResult(exit, output.ToString(), error.ToString());
    }

    private sealed record RunResult(int Exit, string Output, string Error);

    [Fact]
    public void FileArgumentPrintsHeaderAndSmellLineWithExitZero()
    {
        string file = WriteFile("Smelly.cs", SmellyClass);

        var result = Run(file);

        Assert.Equal(0, result.Exit);
        Assert.Equal("Smelly.cs:\n  Feature Envy (6, 17)\n", result.Output);
    }

    [Fact]
    public void DirectoryArgumentOmitsCleanFilesAndUsesRelativeHeaders()
    {
        WriteFile("clean/Clean.cs", "public class Clean { }");
        WriteFile("dirty/Smelly.cs", SmellyClass);

        var result = Run(Root);

        Assert.Equal(0, result.Exit);
        Assert.Equal("dirty/Smelly.cs:\n  Feature Envy (6, 17)\n", result.Output);
    }

    [Fact]
    public void CleanFileProducesNoOutputWithExitZero()
    {
        string file = WriteFile("Clean.cs", "public class Clean { }");

        var result = Run(file);

        Assert.Equal(0, result.Exit);
        Assert.Empty(result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void NonCSharpFileProducesNoOutputWithExitZero()
    {
        string file = WriteFile("notes.txt", SmellyClass);

        var result = Run(file);

        Assert.Equal(0, result.Exit);
        Assert.Empty(result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void MissingPathReportsOnStderrWithExitTwo()
    {
        var result = Run(Path.Combine(Root, "missing"));

        Assert.Equal(2, result.Exit);
        Assert.Equal(string.Empty, result.Output);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public void TwoArgumentsAreUsageErrorWithExitTwo()
    {
        var result = Run(Root, Root);

        Assert.Equal(2, result.Exit);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public void NoArgumentScansTheCurrentDirectoryWithExitZero()
    {
        var result = Run();

        Assert.Equal(0, result.Exit);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void OutputUsesInvariantLineFeedsEvenForCrlfWriters()
    {
        string file = WriteFile("Smelly.cs", SmellyClass);
        using var output = new StringWriter();
        output.NewLine = "\r\n";
        using var error = new StringWriter();

        int exit = new Program().Run([file], output, error);

        Assert.Equal(0, exit);
        Assert.Equal("Smelly.cs:\n  Feature Envy (6, 17)\n", output.ToString());
        Assert.DoesNotContain('\r', output.ToString());
    }
}
