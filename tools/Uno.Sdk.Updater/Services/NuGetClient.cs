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
		var privateVersions = await GetPrivatePackageVersions(packageId);
		allVersions.AddRange(publicVersions);
		allVersions.AddRange(privateVersions);

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

	public void Dispose()
	{
		PublicNuGetClient.Dispose();
	}
}
