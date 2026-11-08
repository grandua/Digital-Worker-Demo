using Encapsulate.Domain;

namespace Encapsulate.Data;

/// <summary>Reads C# sources from a directory (recursively) or a single file.</summary>
public sealed class SourceTree(string root)
{
    public bool Exists => Directory.Exists(root) || File.Exists(root);

    public IReadOnlyList<SourceFile> ReadSources() =>
        Directory.Exists(root) ? ReadDirectory() : ReadFile();

    private IReadOnlyList<SourceFile> ReadDirectory() =>
        Directory.EnumerateFiles(root, "*", DiscoveryOptions)
            .Where(IsCSharpFile)
            .Order(StringComparer.Ordinal)
            .Select(path => TryRead(path))
            .OfType<SourceFile>()
            .ToList();

    // A file that cannot be read (permissions, locks) is skipped, like the inaccessible entries discovery skips.
    //TODO: Framework Design -- Exceptions: split the filter into two typed catch clauses (IOException, UnauthorizedAccessException) instead of catching System.Exception.
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
