using System.Net.Http.Json;
using Uno.Sdk.Models;

namespace Uno.Sdk.Services;

internal class NuGetApiClient : IDisposable
{
	private HttpClient PublicNuGetClient { get; } = new HttpClient
	{
		BaseAddress = new Uri("https://api.nuget.org")
	};

	private HttpClient PrivateNuGetClient { get; } = new HttpClient
	{
		BaseAddress = new Uri("https://pkgs.dev.azure.com")
	};

	public NuGetVersion? UnoVersion { get; set; }

	public async Task<Stream> DownloadPackageAsync(string packageId, string version)
	{
		var downloadUrl = $"/uno-platform/1dd81cbd-cb35-41de-a570-b0df3571a196/_apis/packaging/feeds/e7ce08df-613a-41a3-8449-d42784dd45ce/nuget/packages/{packageId}/versions/{version}/content";
		using var response = await PrivateNuGetClient.GetAsync(downloadUrl);

		if (!response.IsSuccessStatusCode)
			return Stream.Null;

		using var tempStream = await response.Content.ReadAsStreamAsync();
		var memoryStream = new MemoryStream();
		await tempStream.CopyToAsync(memoryStream);

		return memoryStream;
	}

	internal record VersionsResponse(string[] Versions);

	public async Task<IEnumerable<NuGetVersion>> GetPackageVersions(string packageId)
	{
		var allVersions = new List<string>();
		var publicVersions = await GetPublicPackageVersions(packageId);
		allVersions.AddRange(publicVersions);

		if (!UnoVersion.HasValue || !UnoVersion.Value.IsPreview)
		{
			var privateVersions = await GetPrivatePackageVersions(packageId);
			allVersions.AddRange(privateVersions);
		}

		var output = new List<NuGetVersion>();
		foreach (var version in allVersions.Distinct())
		{
			if (NuGetVersion.TryParse(version, out var nugetVersion))
			{
				output.Add(nugetVersion);
			}
		}

		return output.OrderByDescending(x => x);
	}

	private async Task<IEnumerable<string>> GetPrivatePackageVersions(string packageId)
	{
		try
		{
			var response = await PrivateNuGetClient.GetFromJsonAsync<VersionsResponse>($"/uno-platform/1dd81cbd-cb35-41de-a570-b0df3571a196/_packaging/e7ce08df-613a-41a3-8449-d42784dd45ce/nuget/v3/flat2/{packageId.ToLowerInvariant()}/index.json");
			return response?.Versions ?? [];
		}
		catch
		{
			return [];
		}
	}

	private async Task<IEnumerable<string>> GetPublicPackageVersions(string packageId)
	{
		try
		{
			var response = await PublicNuGetClient.GetFromJsonAsync<VersionsResponse>($"/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json");
			return response?.Versions ?? [];
		}
		catch
		{
			return [];
		}
	}

	public async Task<string> GetVersionAsync(string packageId, bool preview, string? minimumVersionString = null)
	{
		var versions = await GetPackageVersions(packageId);
		versions = versions.Where(x => x.IsPreview == preview);

		if (NuGetVersion.TryParse(minimumVersionString, out var minimumVersion))
		{
			versions = versions.Where(x => minimumVersion.Version <= x.Version);
		}

		if (!versions.Any())
		{
			return string.Empty;
		}

		return versions.OrderByDescending(x => x).First().OriginalVersion;
	}

<<<<<<< HEAD
	public void Dispose()
	{
		PublicNuGetClient.Dispose();
	}
=======
    private static NuGetVersion GetGroupVersion(string packageId, NuGetVersion packageVersion)
    {
        if (packageId.StartsWith("Uno", StringComparison.InvariantCultureIgnoreCase))
        {
            return NuGetVersion.Parse($"{packageVersion.Version.Major}.{packageVersion.Version.Minor}.0");
        }

        return NuGetVersion.Parse($"{packageVersion.Version.Major}.{packageVersion.Version.Minor}.{packageVersion.Version.Build}");
    }

    private bool RequiresValidation(string packageId) =>
        UnoVersion is not null && 
        packageId.Contains("Uno", StringComparison.InvariantCultureIgnoreCase) &&
        !packageId.StartsWith("Uno.Sdk", StringComparison.InvariantCultureIgnoreCase);

    private async Task<bool> ValidatePackage(string packageId, NuGetVersion version)
    {
        if (_validation.HasBeenChecked(packageId, version))
        {
            return _validation.IsValid(packageId, version);
        }

        var nuspecUrl = $"/v3-flatcontainer/{packageId.ToLowerInvariant()}/{version.OriginalVersion}/{packageId.ToLowerInvariant()}.nuspec";
        using var response = PublicNuGetClient.GetAsync(nuspecUrl).Result;
        if (!response.IsSuccessStatusCode)
        {
            // This could happen if the package we are checking is not publicly available.
            return true;
        }

        var packageResponse = await response.Content.ReadAsStringAsync();
        var xDocument = XDocument.Parse(packageResponse);

        // Define the namespace manager
        var namespaceManager = new XmlNamespaceManager(new NameTable());
        namespaceManager.AddNamespace("ns", "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd");

        // Select dependency groups
        var dependencyGroups = xDocument.XPathSelectElements("//ns:dependencies/ns:group", namespaceManager);

        foreach (var group in dependencyGroups)
        {
            if (_validation.HasBeenChecked(packageId, version))
                continue;

            _validation.AddResult(packageId, version, await IsCompatibleWithUnoWinUI(group));
        }

        _validation.AddResult(packageId, version, true);
        return _validation.IsValid(packageId, version);
    }

    private async Task<bool> IsCompatibleWithUnoWinUI(XElement group)
    {
        var dependencies = group.Elements().Where(e => e.Name.LocalName == "dependency").ToList();
        var unoWinUIDependency = dependencies.FirstOrDefault(d => d.Attribute("id")?.Value == UnoWinUIPackageId);

        // We don't have a dependency on Uno.WinUI
        if (unoWinUIDependency is null)
        {
            var unoDependencies = dependencies.Where(x => x.Attribute("id")?.Value.Contains("Uno", StringComparison.InvariantCultureIgnoreCase) ?? false);

            // Check for Transitive Dependency
            if (unoDependencies.Any())
            {
                var transitiveDependencies = unoDependencies.ToDictionary(x => x.Attribute("id")!.Value, x => NuGetVersion.Parse(x.Attribute("version")!.Value));
                foreach((var packageId, var version) in transitiveDependencies)
                {
                    if (_validation.HasBeenChecked(packageId, version))
                    {
                        continue;
                    }
                    else if (!await ValidatePackage(packageId, version))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        if (!NuGetVersion.TryParse(unoWinUIDependency.Attribute("version")?.Value, out var unoWinUIVersion))
        {
            // This shouldn't happen
            return false;
        }

        if (!_validation.HasBeenChecked(UnoWinUIPackageId, unoWinUIVersion))
        {
            _validation.AddResult(UnoWinUIPackageId, unoWinUIVersion, UnoVersion >= unoWinUIVersion);
        }

        return _validation.IsValid(UnoWinUIPackageId, unoWinUIVersion);
    }

    private async Task<IEnumerable<string>> GetPrivatePackageVersions(string packageId)
    {
        try
        {
            var response = await PrivateNuGetClient.GetFromJsonAsync<VersionsResponse>($"/uno-platform/1dd81cbd-cb35-41de-a570-b0df3571a196/_packaging/e7ce08df-613a-41a3-8449-d42784dd45ce/nuget/v3/flat2/{packageId.ToLowerInvariant()}/index.json");
            return response?.Versions ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task<IEnumerable<string>> GetPublicPackageVersions(string packageId)
    {
        try
        {
            var response = await PublicNuGetClient.GetFromJsonAsync<VersionsResponse>($"/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json");
            return response?.Versions ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<string> GetVersionAsync(string packageId, bool preview, string? minimumVersionString = null)
    {
        var versions = await GetPackageVersions(packageId);
        versions = versions.Where(x => x.IsPreview == preview);

        // https://api.nuget.org/v3-flatcontainer/uno.extensions.hosting.winui/4.2.0-dev.137/uno.extensions.hosting.winui.nuspec
        if (!string.IsNullOrEmpty(minimumVersionString) && NuGetVersion.TryParse(minimumVersionString, out var minimumVersion))
        {
            versions = versions.Where(x => minimumVersion.Version <= x.Version);

            if (UnoVersion.HasValue && !UnoVersion.Value.IsPreview)
            {
                var maxVersion = NuGetVersion.Parse($"{minimumVersion.Version.Major}.{minimumVersion.Version.Minor + 1}.0");
                versions = versions.Where(x => x < maxVersion);
            }
        }

        if (!versions.Any())
        {
            return string.Empty;
        }

        return versions.OrderByDescending(x => x).First().OriginalVersion;
    }

    public void Dispose()
    {
        PublicNuGetClient.Dispose();
    }
>>>>>>> 509a186 (fix: limit updates for stable to the current Major.Minor)
}
