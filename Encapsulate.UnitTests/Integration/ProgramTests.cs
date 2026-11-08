namespace Encapsulate.UnitTests.Integration;

public sealed class ProgramTests : IDisposable
{
    private const string SmellyClass =
        "public class Order { public int Total; public int Tax; }\n" +
        "public class Target\n{\n    public void M(Order o)\n    {\n        var a = o.Total + o.Tax;\n    }\n}\n";

    private readonly string root = Path.Combine(Path.GetTempPath(), "encapsulate-program-tests", Guid.NewGuid().ToString("N"));

    public ProgramTests() => Directory.CreateDirectory(root);

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string WriteFile(string relativePath, string text)
    {
        string full = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
        return full;
    }

    private (int Exit, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exit = new Program().Run(args, output, error);
        return (exit, output.ToString(), error.ToString());
    }

    [Fact]
    public void FileArgumentPrintsHeaderAndSmellLineWithExitZero()
    {
        string file = WriteFile("Smelly.cs", SmellyClass);

        var result = Run(file);

        Assert.Equal(0, result.Exit);
        Assert.Equal("Smelly.cs\n  Feature Envy (6, 17)\n", result.Output);
    }

    [Fact]
    public void DirectoryArgumentOmitsCleanFilesAndUsesRelativeHeaders()
    {
        WriteFile("clean/Clean.cs", "public class Clean { }");
        WriteFile("dirty/Smelly.cs", SmellyClass);

        var result = Run(root);

        Assert.Equal(0, result.Exit);
        Assert.Equal("dirty/Smelly.cs\n  Feature Envy (6, 17)\n", result.Output);
    }

    [Fact]
    public void CleanFileProducesNoOutputWithExitZero()
    {
        string file = WriteFile("Clean.cs", "public class Clean { }");

        var result = Run(file);

        Assert.Equal((0, string.Empty, string.Empty), (result.Exit, result.Output, result.Error));
    }

    [Fact]
    public void NonCSharpFileProducesNoOutputWithExitZero()
    {
        string file = WriteFile("notes.txt", SmellyClass);

        var result = Run(file);

        Assert.Equal((0, string.Empty, string.Empty), (result.Exit, result.Output, result.Error));
    }

    [Fact]
    public void MissingPathReportsOnStderrWithExitTwo()
    {
        var result = Run(Path.Combine(root, "missing"));

        Assert.Equal(2, result.Exit);
        Assert.Equal(string.Empty, result.Output);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public void TwoArgumentsAreUsageErrorWithExitTwo()
    {
        var result = Run(root, root);

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
}
