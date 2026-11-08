using Encapsulate.Data;

namespace Encapsulate.UnitTests.Integration;

public sealed class SourceTreeTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "encapsulate-tests", Guid.NewGuid().ToString("N"));

    public SourceTreeTests() => Directory.CreateDirectory(root);

    public void Dispose() => Directory.Delete(root, recursive: true);

    private void Write(string relativePath, string text = "class X { }")
    {
        string full = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    [Fact]
    public void ScansDirectoriesRecursivelyIncludingHiddenBinAndObjAndCaseInsensitiveExtensions()
    {
        Write("A.cs");
        Write("A.CS");
        Write("sub/B.cs");
        Write(".hidden/C.cs");
        Write("bin/D.cs");
        Write("obj/E.cs");
        Write("notes.txt");

        var headers = new SourceTree(root).ReadSources().Select(source => source.HeaderPath).Order(StringComparer.Ordinal);

        Assert.Equal([".hidden/C.cs", "A.CS", "A.cs", "bin/D.cs", "obj/E.cs", "sub/B.cs"], headers);
    }

    [Fact]
    public void SourceTextIsReadAsWritten()
    {
        Write("A.cs", "class Alpha { }");

        var source = Assert.Single(new SourceTree(root).ReadSources());

        Assert.Equal("class Alpha { }", source.Text);
    }

    [Fact]
    public void SingleCSharpFileIsScannedByItsFileName()
    {
        Write("Single.cs", "class Single { }");

        var sources = new SourceTree(Path.Combine(root, "Single.cs")).ReadSources();

        Assert.Equal([new Encapsulate.Domain.SourceFile("Single.cs", "class Single { }")], sources);
    }

    [Fact]
    public void ExplicitNonCSharpFileIsNotScanned()
    {
        Write("readme.md", "# text");

        Assert.Empty(new SourceTree(Path.Combine(root, "readme.md")).ReadSources());
    }

    [Fact]
    public void EmptyDirectoryHasNoSources()
    {
        Assert.Empty(new SourceTree(root).ReadSources());
    }

    [Fact]
    public void FileThatCannotBeReadIsSkippedAndTheOtherFilesAreStillScanned()
    {
        Write("Readable.cs", "class Readable { }");
        Write("Locked.cs", "class Locked { }");
        using var exclusive = new FileStream(Path.Combine(root, "Locked.cs"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var headers = new SourceTree(root).ReadSources().Select(source => source.HeaderPath);

        Assert.Equal(["Readable.cs"], headers);
    }

    [Fact]
    public void MissingPathDoesNotExist()
    {
        Assert.False(new SourceTree(Path.Combine(root, "missing")).Exists);
    }

    [Fact]
    public void ExistingDirectoryAndFileExist()
    {
        Write("A.cs");

        Assert.True(new SourceTree(root).Exists);
        Assert.True(new SourceTree(Path.Combine(root, "A.cs")).Exists);
    }
}
