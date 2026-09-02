using System.Xml.Linq;
using Xunit;

namespace SciCalc.Packaging.Tests;

public sealed class PackagingManifestTests : ConformanceTests
{
    private const string PlaceholderAsset = "$placeholder$.png";

    private const string FoundationNamespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

    private const string UapNamespace = "http://schemas.microsoft.com/appx/manifest/uap/windows10";

    private XDocument Project => Repo.Xml("src", "SciCalc", "SciCalc.csproj");

    private XDocument Manifest => Repo.Xml("src", "SciCalc", "Platforms", "Windows", "Package.appxmanifest");

    [Fact]
    public void Csproj_DeclaresIconAndSplashScreenSourcesUnderResources()
    {
        XDocument project = Project;
        Assert.NotEmpty(project.Descendants("MauiIcon"));
        Assert.NotEmpty(project.Descendants("MauiSplashScreen"));
        Assert.All(IconAndSplashIncludes(project), AssertUnderResources);
    }

    [Theory]
    [InlineData("AppIcon")]
    [InlineData("Splash")]
    public void ResourceFolder_ContainsAtLeastOneAsset(string folder)
    {
        Assert.NotEmpty(Repo.Dir("src", "SciCalc", "Resources", folder).EnumerateFiles());
    }

    [Fact]
    public void AppxManifest_DeclaresIdentityApplicationAndVisualElements()
    {
        XElement package = Manifest.Root!;
        AssertIdentityIsComplete(package);
        AssertApplicationIsComplete(package);
    }

    [Theory]
    [InlineData("Logo")]
    [InlineData("Square150x150Logo")]
    [InlineData("Square44x44Logo")]
    [InlineData("Square71x71Logo")]
    [InlineData("Wide310x150Logo")]
    [InlineData("Square310x310Logo")]
    [InlineData("Image")]
    public void ManifestAsset_IsPlaceholderOrExistingFile(string assetName)
    {
        string? value = Asset(assetName);
        Assert.False(string.IsNullOrWhiteSpace(value), $"'{assetName}' must declare an asset value in Package.appxmanifest.");
        Assert.True(IsAllowedAsset(value), $"'{assetName}' value '{value}' must be '{PlaceholderAsset}' or resolve to an existing file.");
    }

    [Fact]
    public void ManifestAssetPlaceholders_AreAllOrNothing()
    {
        Assert.Single(AssetNames().Select(IsPlaceholder).Distinct());
    }

    private static IEnumerable<string> AssetNames() =>
        ["Logo", "Square150x150Logo", "Square44x44Logo", "Square71x71Logo", "Wide310x150Logo", "Square310x310Logo", "Image"];

    private static IEnumerable<string> IconAndSplashIncludes(XDocument project) =>
        project.Descendants("MauiIcon").Concat(project.Descendants("MauiSplashScreen"))
            .Select(item => item.Attribute("Include")!.Value);

    private void AssertUnderResources(string include)
    {
        FileInfo source = ResolveProjectAsset(include);
        Assert.StartsWith("Resources", include.Replace('\\', Path.DirectorySeparatorChar));
        Assert.True(source.Exists, $"'{include}' must resolve to an existing file under src/SciCalc/Resources/.");
    }

    private FileInfo ResolveProjectAsset(string include)
    {
        string[] segments = include.Replace('\\', Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        string[] path = ["src", "SciCalc", .. segments];
        return Repo.File(path);
    }

    private static void AssertIdentityIsComplete(XElement package)
    {
        XElement? identity = package.Element(Found("Identity"));
        Assert.NotNull(identity);
        Assert.False(string.IsNullOrWhiteSpace((string?)identity.Attribute("Name")), "Identity/@Name must be non-empty.");
        Assert.False(string.IsNullOrWhiteSpace((string?)identity.Attribute("Version")), "Identity/@Version must be non-empty.");
    }

    private static void AssertApplicationIsComplete(XElement package)
    {
        XElement? application = package.Element(Found("Applications"))?.Element(Found("Application"));
        Assert.NotNull(application);
        Assert.NotNull(application.Element(Uap("VisualElements")));
    }

    private string? Asset(string assetName) => AssetAttribute(assetName) ?? AssetElement(assetName);

    private string? AssetAttribute(string assetName) =>
        Manifest.Descendants().Attributes().Where(attribute => attribute.Name.LocalName == assetName)
            .Select(attribute => attribute.Value).FirstOrDefault();

    private string? AssetElement(string assetName) =>
        Manifest.Descendants().Where(element => element.Name.LocalName == assetName)
            .Select(element => element.Value).FirstOrDefault();

    private bool IsAllowedAsset(string? value) => IsPlaceholder(value) ? DeclaresIconAndSplash() : ManifestAssetFile(value).Exists;

    private bool DeclaresIconAndSplash() =>
        Project.Descendants("MauiIcon").Any() && Project.Descendants("MauiSplashScreen").Any();

    private FileInfo ManifestAssetFile(string? value)
    {
        string relative = value!.Replace('/', Path.DirectorySeparatorChar);
        string[] segments = ["src", "SciCalc", "Platforms", "Windows", .. relative.Split(Path.DirectorySeparatorChar)];
        return Repo.File(segments);
    }

    private static bool IsPlaceholder(string? value) => value == PlaceholderAsset;

    private static XName Found(string localName) => XName.Get(localName, FoundationNamespace);

    private static XName Uap(string localName) => XName.Get(localName, UapNamespace);
}
