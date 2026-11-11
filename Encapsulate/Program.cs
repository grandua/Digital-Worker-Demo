using Encapsulate.Data;
using Encapsulate.Presentation;

namespace Encapsulate;

/// <summary>Composition root: resolves the optional path argument, scans, and writes the report.</summary>
public sealed class Program
{
    private const int InvalidUsage = 2;

    public static int Main(string[] args) =>
        new Program().Run(args, Console.Out, Console.Error);

    // The composition root keeps only the usage check (a decision on raw CLI input), console writes and the exit code;
    // root resolution and the existence check live on SourceTree (plan P7-a).
    public int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        if (args.Count > 1)
        {
            return Reject(error, "usage: encapsulate [path]");
        }
        var tree = SourceTree.FromArguments(args);
        return tree.Exists ? Scan(tree, output) : Reject(error, $"encapsulate: path not found: {tree.Root}");
    }

    private int Scan(SourceTree tree, TextWriter output)
    {
        var findings = new Codebase(tree.ReadSources()).FindSmells();
        foreach (string line in new Report(findings).Render())
        {
            output.Write($"{line}\n");
        }
        return 0;
    }

    private int Reject(TextWriter error, string message)
    {
        error.WriteLine(message);
        return InvalidUsage;
    }
}
