using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Sdk.Models;
using Uno.Sdk.Services;
using Uno.Sdk.Updater.Config;

namespace Uno.Sdk.Updater.Tests;

[TestClass]
public class SkiaSharpReleaseTests
{
    private const string ReleaseCommit = "c8c35b59e8de5efbc2abf52fc26202ae1731e593";
    private const string NextReleaseCommit = "2a53a3c8641073bc914c63ee158bbf62b8a0bb92";

    private static readonly ManifestGroup[] Manifest =
    [
        new("SkiaSharp", "4.156.1", ["SkiaSharp.Skottie", "SkiaSharp.NativeAssets.Android"]),
        new("HarfBuzzSharp", "14.4.0.101", ["HarfBuzzSharp.NativeAssets.Android"]),
    ];

    [TestMethod]
    public async Task HarfBuzzSharp_FromTheSkiaSharpRelease_Matches()
    {
        using var client = CreateClient(("skiasharp.skottie", "4.156.1", ReleaseCommit), ("harfbuzzsharp.nativeassets.android", "14.4.0.101", ReleaseCommit));

        Assert.IsNull(await SkiaSharpRelease.FindHarfBuzzSharpMismatchAsync(Manifest, client));
    }

    [TestMethod]
    public async Task HarfBuzzSharp_FromAnotherSkiaSharpRelease_IsReported()
    {
        // A SkiaSharp move that forgot HarfBuzzSharp, or HarfBuzzSharp moved alone to the next release's
        using var client = CreateClient(("skiasharp.skottie", "4.156.1", ReleaseCommit), ("harfbuzzsharp.nativeassets.android", "14.4.0.101", NextReleaseCommit));

        var mismatch = await SkiaSharpRelease.FindHarfBuzzSharpMismatchAsync(Manifest, client);

        Assert.IsNotNull(mismatch);
        StringAssert.Contains(mismatch, "HarfBuzzSharp.NativeAssets.Android 14.4.0.101");
        StringAssert.Contains(mismatch, "SkiaSharp.Skottie 4.156.1");
    }

    [TestMethod]
    public async Task Package_MissingOnNuGet_IsReported()
    {
        using var client = CreateClient(("skiasharp.skottie", "4.156.1", ReleaseCommit));

        var mismatch = await SkiaSharpRelease.FindHarfBuzzSharpMismatchAsync(Manifest, client);

        Assert.IsNotNull(mismatch);
        StringAssert.Contains(mismatch, "Could not read the source commit of HarfBuzzSharp.NativeAssets.Android 14.4.0.101");
    }

    [TestMethod]
    public async Task Manifest_WithoutHarfBuzzSharpGroup_IsReported()
    {
        using var client = CreateClient();

        var mismatch = await SkiaSharpRelease.FindHarfBuzzSharpMismatchAsync([Manifest[0]], client);

        Assert.IsNotNull(mismatch);
        StringAssert.Contains(mismatch, "'HarfBuzzSharp'");
    }

    private static NuGetApiClient CreateClient(params (string Id, string Version, string Commit)[] packages) => new(new NuspecHandler(packages));

    // Serves nuget.org flat-container nuspecs, in the 2012/06 schema namespace that SkiaSharp's packages use
    private sealed class NuspecHandler((string Id, string Version, string Commit)[] packages) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            foreach ((var id, var version, var commit) in packages)
            {
                if (request.RequestUri?.AbsolutePath == $"/v3-flatcontainer/{id}/{version}/{id}.nuspec")
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
                              <metadata>
                                <id>{id}</id>
                                <version>{version}</version>
                                <repository type="git" url="https://go.microsoft.com/fwlink/?linkid=868515" commit="{commit}" />
                              </metadata>
                            </package>
                            """)
                    });
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
