namespace Encapsulate.UnitTests.Support;

/// <summary>Test fixture owning a fresh temporary directory that is removed on disposal.</summary>
public abstract class TempDirectoryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "encapsulate-tests", Guid.NewGuid().ToString("N"));

    protected TempDirectoryTests() => Directory.CreateDirectory(root);

    public void Dispose() => Directory.Delete(root, recursive: true);

    protected string Root => root;

    /// <summary>Writes <paramref name="text"/> to a relative path inside the temp root, creating parent directories.</summary>
    protected string WriteFile(string relativePath, string text)
    {
        string full = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
        return full;
    }
}
