namespace Encapsulate.Domain;

/// <summary>An immutable C# source file: its header path (relative to the scanned root) and its text.</summary>
public sealed record SourceFile(string HeaderPath, string Text);
