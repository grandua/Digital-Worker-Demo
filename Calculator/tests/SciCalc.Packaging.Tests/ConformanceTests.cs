namespace SciCalc.Packaging.Tests;

public abstract class ConformanceTests
{
    protected RepoRoot Repo { get; } = RepoRoot.Locate();
}
