using Encapsulate.Domain;

namespace Encapsulate.Data;

/// <summary>Reads C# sources from a directory (recursively) or a single file; owns root resolution for the CLI.</summary>
public sealed class SourceTree(string root)
{
    public string Root => root;

    /// <summary>Resolves the scan root from the CLI arguments: the single argument, or the current directory when none is given.</summary>
    public static SourceTree FromArguments(IReadOnlyList<string> arguments) =>
        new(arguments.Count == 0 ? Directory.GetCurrentDirectory() : arguments[0]);

    public bool Exists => Directory.Exists(root) || File.Exists(root);

    public IReadOnlyList<SourceFile> ReadSources() =>
        Directory.Exists(root) ? ReadDirectory() : ReadFile();

    private IReadOnlyList<SourceFile> ReadDirectory() =>
        Directory.EnumerateFiles(root, "*", DiscoveryOptions)
            .Where(IsCSharpFile)
            .Order(StringComparer.Ordinal)
            .Select(TryRead)
            .OfType<SourceFile>()
            .ToList();

    private SourceFile? TryRead(string path)
    {
        try
        {
            return new SourceFile(HeaderPath(path), File.ReadAllText(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private IReadOnlyList<SourceFile> ReadFile() =>
        IsCSharpFile(root) ? [new SourceFile(Path.GetFileName(root), File.ReadAllText(root))] : [];

    private string HeaderPath(string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private bool IsCSharpFile(string path) =>
        Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase);

    // Hidden and system entries are kept (plan R9); only reparse points (symlinks) are skipped.
    private EnumerationOptions DiscoveryOptions => new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
        MatchCasing = MatchCasing.CaseInsensitive,
    };
}
