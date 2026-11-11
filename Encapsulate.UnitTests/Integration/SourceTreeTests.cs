using Encapsulate.Data;
using Encapsulate.UnitTests.Support;

namespace Encapsulate.UnitTests.Integration;

public sealed class SourceTreeTests : TempDirectoryTests
{
    private void Write(string relativePath, string text = "class X { }") =>
        WriteFile(relativePath, text);

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

        var headers = new SourceTree(Root).ReadSources().Select(source => source.HeaderPath).Order(StringComparer.Ordinal);

        Assert.Equal([".hidden/C.cs", "A.CS", "A.cs", "bin/D.cs", "obj/E.cs", "sub/B.cs"], headers);
    }

    [Fact]
    public void SourceTextIsReadAsWritten()
    {
        Write("A.cs", "class Alpha { }");

        var source = Assert.Single(new SourceTree(Root).ReadSources());

        Assert.Equal("class Alpha { }", source.Text);
    }

    [Fact]
    public void SingleCSharpFileIsScannedByItsFileName()
    {
        Write("Single.cs", "class Single { }");

        var sources = new SourceTree(Path.Combine(Root, "Single.cs")).ReadSources();

        Assert.Equal([new Encapsulate.Domain.SourceFile("Single.cs", "class Single { }")], sources);
    }

    [Fact]
    public void ExplicitNonCSharpFileIsNotScanned()
    {
        Write("readme.md", "# text");

        Assert.Empty(new SourceTree(Path.Combine(Root, "readme.md")).ReadSources());
    }

    [Fact]
    public void EmptyDirectoryHasNoSources()
    {
        Assert.Empty(new SourceTree(Root).ReadSources());
    }

    [Fact]
    public void FileThatCannotBeReadIsSkippedAndTheOtherFilesAreStillScanned()
    {
        Write("Readable.cs", "class Readable { }");
        Write("Locked.cs", "class Locked { }");
        using var exclusive = new FileStream(Path.Combine(Root, "Locked.cs"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var headers = new SourceTree(Root).ReadSources().Select(source => source.HeaderPath);

        Assert.Equal(["Readable.cs"], headers);
    }

    [Fact]
    public void MissingPathDoesNotExist()
    {
        Assert.False(new SourceTree(Path.Combine(Root, "missing")).Exists);
    }

    [Fact]
    public void ExistingDirectoryAndFileExist()
    {
        Write("A.cs");

        Assert.True(new SourceTree(Root).Exists);
        Assert.True(new SourceTree(Path.Combine(Root, "A.cs")).Exists);
    }
}
