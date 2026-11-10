using Encapsulate.Data;
using Encapsulate.Domain;
using Encapsulate.Presentation;

namespace Encapsulate;

/// <summary>Composition root: resolves the optional path argument, scans, and writes the report.</summary>
public sealed class Program
{
    private const int InvalidUsage = 2;

    public static int Main(string[] args) =>
        new Program().Run(args, Console.Out, Console.Error);

    public int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        if (args.Count > 1)
        {
            return Reject(error, "usage: encapsulate [path]");
        }
        string root = args.Count == 0 ? Directory.GetCurrentDirectory() : args[0];
        var tree = new SourceTree(root);
        return tree.Exists ? Scan(tree, output) : Reject(error, $"encapsulate: path not found: {root}");
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
