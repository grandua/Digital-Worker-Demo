using System.Xml.Linq;

namespace SciCalc.Packaging.Tests;

public sealed class RepoRoot
{
    private const string SolutionFile = "SciCalc.sln";

    private readonly DirectoryInfo _root;

    private RepoRoot(DirectoryInfo root) => _root = root;

    public static RepoRoot Locate()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !IsRepoRoot(current))
        {
            current = current.Parent;
        }
        return new RepoRoot(current ?? throw new InvalidOperationException($"'{SolutionFile}' not found above the test output directory."));
    }

    private static bool IsRepoRoot(DirectoryInfo directory) => directory.EnumerateFiles(SolutionFile).Any();

    private string PathFor(params string[] segments) => Path.Combine(_root.FullName, Path.Combine(segments));

    public FileInfo File(params string[] segments) => new(PathFor(segments));

    public DirectoryInfo Dir(params string[] segments) => new(PathFor(segments));

    public string Read(params string[] segments) => System.IO.File.ReadAllText(PathFor(segments));

    public XDocument Xml(params string[] segments) => XDocument.Parse(Read(segments));
}
