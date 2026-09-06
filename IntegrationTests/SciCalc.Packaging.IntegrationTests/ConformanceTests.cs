namespace SciCalc.Packaging.IntegrationTests;

public abstract class ConformanceTests
{
    protected RepoRoot Repo { get; } = RepoRoot.Locate();
}
