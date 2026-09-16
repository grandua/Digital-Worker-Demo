using System.Xml.Linq;
using Xunit;

namespace SciCalc.Maui.UnitTests;

public sealed class WindowsAppManifestTests : ConformanceTests
{
    private const string AssemblyNamespace = "urn:schemas-microsoft-com:asm.v1";

    private const string ManifestVersionAttribute = "manifestVersion";

    private const string BogusManifestIdentifierAttribute = "manifestIdentifier";

    private XDocument Manifest => Repo.Xml("Presentation", "SciCalc.Maui", "Platforms", "Windows", "app.manifest");

    private XDocument Project => Repo.Xml("Presentation", "SciCalc.Maui", "SciCalc.Maui.csproj");

    [Fact]
    public void AppManifest_RootDeclaresRequiredManifestVersion()
    {
        XElement root = Manifest.Root!;
        Assert.Equal("assembly", root.Name.LocalName);
        Assert.Equal(AssemblyNamespace, root.Name.NamespaceName);
        Assert.Equal("1.0", (string?)root.Attribute(ManifestVersionAttribute));
    }

    [Fact]
    public void AppManifest_RootCarriesNoBogusManifestIdentifier()
    {
        Assert.Null(Manifest.Root!.Attribute(BogusManifestIdentifierAttribute));
    }

    [Fact]
    public void AppManifest_DeclaresAssemblyIdentity()
    {
        XElement identity = Manifest.Root!.Element(XName.Get("assemblyIdentity", AssemblyNamespace))
            ?? throw new Xunit.Sdk.XunitException("app.manifest must declare <assemblyIdentity>.");
        Assert.False(string.IsNullOrWhiteSpace((string?)identity.Attribute("version")), "assemblyIdentity/@version must be non-empty.");
        Assert.False(string.IsNullOrWhiteSpace((string?)identity.Attribute("name")), "assemblyIdentity/@name must be non-empty.");
    }

    [Fact]
    public void Csproj_RunsUnpackagedForDirectExeLaunch()
    {
        XElement project = Project.Root!;
        Assert.Equal("None", (string?)project.Element("PropertyGroup")?.Element("WindowsPackageType"));
    }

    [Fact]
    public void Csproj_DeploysWindowsAppSdkSelfContained()
    {
        XElement project = Project.Root!;
        Assert.Equal("true", (string?)project.Element("PropertyGroup")?.Element("WindowsAppSDKSelfContained"));
    }
}
