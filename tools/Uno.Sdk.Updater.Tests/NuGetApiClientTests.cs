using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Sdk.Services;

namespace Uno.Sdk.Updater.Tests;

[TestClass]
public class NuGetApiClientTests
{
    private const string TooNewUnoWinUI = """<dependency id="Uno.WinUI" version="9.0.0" />""";
    private const string SupportedUnoWinUI = """<dependency id="Uno.WinUI" version="6.0.0" />""";

    [TestMethod]
    public async Task ValidatePackage_ExactVersionDependency_ValidatesThatVersion()
    {
        using var client = CreateClient(
            ("Uno.Tests.ExactParent", """<dependency id="Uno.Tests.ExactChild" version="[1.0.0]" />"""),
            ("Uno.Tests.ExactChild", SupportedUnoWinUI));

        Assert.IsTrue(await client.ValidatePackage("Uno.Tests.ExactParent", "1.0.0"));
    }

    [TestMethod]
    public async Task ValidatePackage_ExactVersionDependencyRejected_RejectsParent()
    {
        using var client = CreateClient(
            ("Uno.Tests.ExactRejectedParent", """<dependency id="Uno.Tests.ExactRejectedChild" version="[1.0.0]" />"""),
            ("Uno.Tests.ExactRejectedChild", TooNewUnoWinUI));

        Assert.IsFalse(await client.ValidatePackage("Uno.Tests.ExactRejectedParent", "1.0.0"));
    }

    [TestMethod]
    public async Task ValidatePackage_DependencyAlreadyRejected_RejectsParent()
    {
        using var client = CreateClient(
            ("Uno.Tests.KnownRejectedParent", """<dependency id="Uno.Tests.KnownRejectedChild" version="1.0.0" />"""),
            ("Uno.Tests.KnownRejectedChild", TooNewUnoWinUI));

        Assert.IsFalse(await client.ValidatePackage("Uno.Tests.KnownRejectedChild", "1.0.0"));
        Assert.IsFalse(await client.ValidatePackage("Uno.Tests.KnownRejectedParent", "1.0.0"));
    }

    [TestMethod]
    public async Task ValidatePackage_DependencyWithoutLowerBound_IsSkipped()
    {
        using var client = CreateClient(
            ("Uno.Tests.UnboundedParent", """<dependency id="Uno.Tests.UnboundedChild" version="(,2.0.0]" />"""));

        Assert.IsTrue(await client.ValidatePackage("Uno.Tests.UnboundedParent", "1.0.0"));
    }

    private static NuGetApiClient CreateClient(params (string PackageId, string Dependencies)[] packages) =>
        new(new NuspecHandler(packages)) { UnoVersion = "7.0.0" };

    private sealed class NuspecHandler((string PackageId, string Dependencies)[] packages) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            foreach ((var packageId, var dependencies) in packages)
            {
                if (request.RequestUri!.AbsolutePath.EndsWith($"/{packageId.ToLowerInvariant()}.nuspec", StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"""
                            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                              <metadata>
                                <dependencies>
                                  <group targetFramework="net10.0">{dependencies}</group>
                                </dependencies>
                              </metadata>
                            </package>
                            """),
                    });
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
