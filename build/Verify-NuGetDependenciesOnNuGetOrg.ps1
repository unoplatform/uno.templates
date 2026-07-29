param(
	# Artifact mode (default): scan produced .nupkg files under this path.
	[string]$PackagesPath = "artifacts",
	# Manifest mode: when set, verify the Uno.Sdk source manifest (src/Uno.Sdk/packages.json)
	# instead of built artifacts. Enables a pre-build fail-fast check before the pipeline
	# spends runners on the build and template test matrix. Takes precedence over -PackagesPath.
	[string]$ManifestPath = "",
	[string]$PackageIdFilter = "",
	# Availability is verified in set-based rounds: MaxAttempts total sweeps (1 initial + up to
	# MaxAttempts-1 retries of only the still-missing set), RetryDelaySeconds between rounds.
	# Total wait on failure is bounded by (MaxAttempts - 1) * RetryDelaySeconds regardless of how
	# many packages are missing, so a genuinely absent version fails fast. Default: 2 retries max.
	# MaxAttempts must be >= 1: a value of 0 would skip the verification loop entirely and
	# silently treat every dependency as available.
	[ValidateRange(1, [int]::MaxValue)]
	[int]$MaxAttempts = 3,
	[ValidateRange(0, [int]::MaxValue)]
	[int]$RetryDelaySeconds = 20,
	[ValidateRange(1, [int]::MaxValue)]
	[int]$HttpTimeoutSeconds = 30,
	[ValidateRange(0, [int]::MaxValue)]
	[int]$TransitiveDependencyDepth = 1,
	[switch]$IncludeStableTransitiveVersions,
	# PR fail-fast pass: verify ONLY prerelease (dev) coordinates on nuget.org, skipping every
	# stable-versioned dependency (direct or transitive). Dev packages are published to nuget.org
	# continuously, so a missing one is a genuine error worth failing a PR fast; stable versions may
	# legitimately be unpublished while a stable release is staged on the internal feed first, and
	# are gated later — only right before the nuget.org publish. Full-closure (switch off) is used
	# for the dev-publish (main) path and the pre-prod-publish gate.
	[switch]$PrereleaseDependenciesOnly,
	# Report mode (non-blocking): collect missing dependencies but DO NOT throw / fail. Used by the
	# PR job, which surfaces the result as a resolvable inline comment instead of failing the build.
	# When -ReportOutputPath is set the missing set is written there as JSON (always, even if empty,
	# so the consumer is deterministic).
	[switch]$ReportOnly,
	[string]$ReportOutputPath = "",
	[string[]]$StableTransitiveIncludePrefixes = @('Uno.'),
	[ValidateRange(0, [int]::MaxValue)]
	[int]$TrackedTransitiveDependencyMaxDepth = 16
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ExactDependencyVersion {
	param(
		[Parameter(Mandatory = $true)]
		[string]$VersionRange
	)

	if ($VersionRange -match '^\[(?<version>[^,\]]+)\]$') {
		return $Matches.version
	}

	if ($VersionRange -match '^[0-9A-Za-z\.-]+$') {
		return $VersionRange
	}

	return $null
}

function Test-IsTrackedPackageId {
	# True when $PackageId starts with any of $IncludePrefixes (case-insensitive).
	# "Tracked" ids (default: Uno.*) follow our own release cadence, so — unlike
	# third-party / BCL packages — they can legitimately reference a version that is
	# not published yet. Tracked ids therefore get two special treatments:
	#   * stable transitive versions are still verified (not skipped), and
	#   * their dependency graph is walked to its full closure (not capped at
	#     TransitiveDependencyDepth), so an unpublished tracked package at ANY depth
	#     is caught before publish instead of only one hop deep.
	param(
		[Parameter(Mandatory = $true)]
		[string]$PackageId,
		[AllowEmptyCollection()]
		[string[]]$IncludePrefixes
	)

	if ($null -eq $IncludePrefixes) {
		return $false
	}

	foreach ($prefix in $IncludePrefixes) {
		if ([string]::IsNullOrWhiteSpace($prefix)) {
			continue
		}

		if ($PackageId.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
			return $true
		}
	}

	return $false
}

function Test-IsPrereleaseVersion {
	# True when the version carries a SemVer prerelease label (contains '-'), e.g. 7.1.0-dev.1.
	# Stable versions (7.0.3, 10.0.28000.2270) return false, as does a null/blank version.
	param(
		[AllowEmptyString()]
		[AllowNull()]
		[string]$Version
	)

	return (-not [string]::IsNullOrWhiteSpace($Version)) -and $Version.Contains('-')
}

function Test-ShouldVerifyTransitiveDependency {
	# Whether a discovered transitive dependency must be verified on nuget.org.
	# Prereleases are always verified. Stable versions are skipped (third-party / BCL
	# stable packages are assumed present, and verifying the whole closure would be slow
	# and noisy) unless -IncludeStableTransitiveVersions is set or the id is tracked
	# (Uno.*), which can point at an unpublished stable.
	# In the PR fail-fast pass (-PrereleaseDependenciesOnly) stable versions are ALWAYS
	# skipped — even tracked ones — because a stable dependency may be intentionally
	# unpublished while a stable release is staged on the internal feed first.
	param(
		[Parameter(Mandatory = $true)]
		[string]$PackageId,
		[string]$Version,
		[bool]$IncludeStableTransitiveVersions,
		[AllowEmptyCollection()]
		[string[]]$IncludePrefixes,
		[bool]$PrereleaseDependenciesOnly
	)

	if (Test-IsPrereleaseVersion -Version $Version) {
		return $true
	}

	# Stable version below this point.
	if ($PrereleaseDependenciesOnly) {
		return $false
	}

	if ($IncludeStableTransitiveVersions) {
		return $true
	}

	return (Test-IsTrackedPackageId -PackageId $PackageId -IncludePrefixes $IncludePrefixes)
}

function Test-ShouldExpandCoordinate {
	# Whether a coordinate's own dependencies should be walked (expanded further).
	# Non-tracked packages expand up to TransitiveDependencyDepth hops; tracked (Uno.*)
	# packages expand to their full closure, bounded only by MaxTrackedDepth as a runaway
	# guard (coordinate de-duplication already makes the walk finite).
	param(
		[Parameter(Mandatory = $true)]
		[string]$PackageId,
		[int]$Depth,
		[int]$TransitiveDependencyDepth,
		[int]$MaxTrackedDepth,
		[AllowEmptyCollection()]
		[string[]]$IncludePrefixes
	)

	if ($Depth -lt $TransitiveDependencyDepth) {
		return $true
	}

	if ($Depth -lt $MaxTrackedDepth -and (Test-IsTrackedPackageId -PackageId $PackageId -IncludePrefixes $IncludePrefixes)) {
		return $true
	}

	return $false
}

function Get-NuGetVersionAvailability {
	# One probe of nuget.org for a specific package version. Returns a tri-state:
	#   'Available' - the package id exists and the exact version is listed.
	#   'Absent'    - a DEFINITIVE negative: the id exists but the version is not listed (HTTP 200),
	#                 or the id itself is not published (HTTP 404). Retrying will not change this.
	#   'Error'     - a TRANSIENT failure (network / timeout / 5xx / CDN blip). Retrying may help.
	# The distinction lets callers retry transient blips while failing fast on genuinely-missing
	# versions, and — critically — avoids treating a transient blip as "missing", which could
	# otherwise skip a coordinate's transitive closure and hide unpublished deep dependencies.
	param(
		[Parameter(Mandatory = $true)]
		[string]$PackageId,
		[Parameter(Mandatory = $true)]
		[string]$Version,
		[Parameter(Mandatory = $true)]
		[int]$HttpTimeoutSeconds
	)

	$indexUrl = "https://api.nuget.org/v3-flatcontainer/$($PackageId.ToLowerInvariant())/index.json"

	try {
		$response = Invoke-RestMethod -Uri $indexUrl -Method Get -TimeoutSec $HttpTimeoutSeconds -ErrorAction Stop
		if ($null -ne $response.versions -and ($response.versions -contains $Version.ToLowerInvariant())) {
			return 'Available'
		}

		return 'Absent'
	}
	catch {
		$statusCode = $null
		try { $statusCode = [int]$_.Exception.Response.StatusCode } catch { }

		if ($statusCode -eq 404) {
			# flat-container returns 404 when the package id has no published versions at all.
			return 'Absent'
		}

		Write-Host "Transient error querying $indexUrl : $_"
		return 'Error'
	}
}

function Test-NuGetVersionAvailability {
	# Confirm a version is on nuget.org, retrying ONLY on transient errors. A definitive 'Absent'
	# returns immediately, so genuinely-missing versions fail fast (no wasted retries) while a
	# transient blip is absorbed — closing the gap where a blip could skip a coordinate's
	# transitive closure and hide unpublished deep dependencies.
	param(
		[Parameter(Mandatory = $true)]
		[string]$PackageId,
		[Parameter(Mandatory = $true)]
		[string]$Version,
		[Parameter(Mandatory = $true)]
		[int]$MaxAttempts,
		[Parameter(Mandatory = $true)]
		[int]$RetryDelaySeconds,
		[Parameter(Mandatory = $true)]
		[int]$HttpTimeoutSeconds
	)

	for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
		$status = Get-NuGetVersionAvailability -PackageId $PackageId -Version $Version -HttpTimeoutSeconds $HttpTimeoutSeconds
		if ($status -eq 'Available') {
			return $true
		}

		if ($status -eq 'Absent') {
			return $false
		}

		# transient error — retry
		if ($attempt -lt $MaxAttempts) {
			Write-Host "Transient error for $PackageId $Version (attempt $attempt/$MaxAttempts); retrying in $RetryDelaySeconds second(s)..."
			Start-Sleep -Seconds $RetryDelaySeconds
		}
	}

	return $false
}

function Add-StepSummaryLines {
	param(
		[Parameter(Mandatory = $true)]
		[AllowEmptyString()]
		[string[]]$Lines
	)

	$stepSummaryPath = $env:GITHUB_STEP_SUMMARY
	if ([string]::IsNullOrWhiteSpace($stepSummaryPath)) {
		return
	}

	$content = ($Lines -join [Environment]::NewLine) + [Environment]::NewLine
	Add-Content -Path $stepSummaryPath -Value $content
}

function Read-ZipEntryContent {
	param(
		[Parameter(Mandatory = $true)]
		[System.IO.Compression.ZipArchiveEntry]$Entry
	)

	$reader = New-Object System.IO.StreamReader($Entry.Open())
	try {
		return $reader.ReadToEnd()
	}
	finally {
		$reader.Dispose()
	}
}

function Add-DependencyCheck {
	param(
		[Parameter(Mandatory = $true)]
		[AllowEmptyCollection()]
		[System.Collections.Generic.List[object]]$Checks,
		[Parameter(Mandatory = $true)]
		[string]$PackageId,
		[Parameter(Mandatory = $true)]
		[string]$Version,
		[Parameter(Mandatory = $true)]
		[string]$Source
	)

	if ([string]::IsNullOrWhiteSpace($PackageId) -or [string]::IsNullOrWhiteSpace($Version)) {
		return
	}

	$Checks.Add([PSCustomObject]@{
		Id = $PackageId
		Version = $Version
		Source = $Source
	})
}

function Select-ChecksForVerification {
	# PR fail-fast pass filter: when PrereleaseDependenciesOnly is set, keep only prerelease
	# (dev) coordinates. Stable-versioned direct dependencies are dropped so a stable release
	# staged on the internal feed (its stable deps not yet on nuget.org) does not fail a PR.
	# When the switch is off, the list is returned unchanged (full-closure verification).
	param(
		[Parameter(Mandatory = $true)]
		[AllowEmptyCollection()]
		[System.Collections.Generic.List[object]]$Checks,
		[bool]$PrereleaseDependenciesOnly
	)

	# Return with the unary comma so the List is passed back intact: a bare `return $list`
	# is enumerated by the pipeline (collapsing an empty list to $null and a single-item list
	# to a scalar), which would break the caller's List typing and later `.Add()` calls.
	if (-not $PrereleaseDependenciesOnly) {
		return ,$Checks
	}

	$filtered = New-Object System.Collections.Generic.List[object]
	foreach ($check in $Checks) {
		if (Test-IsPrereleaseVersion -Version ([string]$check.Version)) {
			[void]$filtered.Add($check)
		}
	}

	return ,$filtered
}

function Build-DependencySourcesMap {
	param(
		[Parameter(Mandatory = $true)]
		[AllowEmptyCollection()]
		[System.Collections.Generic.List[object]]$Checks
	)

	$sourcesByDependencyKey = @{}
	foreach ($check in $Checks) {
		$sourceKey = "$($check.Id)|$($check.Version)"
		if (-not $sourcesByDependencyKey.ContainsKey($sourceKey)) {
			$sourcesByDependencyKey[$sourceKey] = New-Object System.Collections.Generic.HashSet[string]
		}

		[void]$sourcesByDependencyKey[$sourceKey].Add([string]$check.Source)
	}

	return $sourcesByDependencyKey
}

function Add-MissingDependencyRecord {
	param(
		[Parameter(Mandatory = $true)]
		[hashtable]$MissingDependencies,
		[Parameter(Mandatory = $true)]
		[string]$PackageId,
		[Parameter(Mandatory = $true)]
		[string]$Version,
		[Parameter(Mandatory = $true)]
		[string]$Source,
		[AllowNull()]
		[hashtable]$SourcesByDependencyKey
	)

	$missingKey = "$PackageId|$Version"
	if (-not $MissingDependencies.ContainsKey($missingKey)) {
		$MissingDependencies[$missingKey] = [PSCustomObject]@{
			Id = $PackageId
			Version = $Version
			Sources = New-Object System.Collections.Generic.List[string]
		}
	}

	if ($null -ne $SourcesByDependencyKey -and $SourcesByDependencyKey.ContainsKey($missingKey)) {
		foreach ($aggregatedSource in $SourcesByDependencyKey[$missingKey]) {
			if (-not $MissingDependencies[$missingKey].Sources.Contains([string]$aggregatedSource)) {
				$MissingDependencies[$missingKey].Sources.Add([string]$aggregatedSource)
			}
		}
	}

	if (-not $MissingDependencies[$missingKey].Sources.Contains($Source)) {
		$MissingDependencies[$missingKey].Sources.Add($Source)
	}
}

function ConvertTo-MissingDependencyReport {
	# Shape the collected missing-dependency records into a plain, serializable list
	# (Id / Version / Sources), sorted, for the PR report payload. Returned with a unary
	# comma so an empty result keeps its List typing through the pipeline.
	param(
		[Parameter(Mandatory = $true)]
		[hashtable]$MissingDependencies
	)

	$report = New-Object System.Collections.Generic.List[object]
	foreach ($entry in ($MissingDependencies.Values | Sort-Object Id, Version)) {
		$report.Add([PSCustomObject]@{
			Id      = $entry.Id
			Version = $entry.Version
			Sources = @($entry.Sources)
		})
	}

	return ,$report
}

function Get-ExactDependenciesFromPackageNuspec {
	param(
		[Parameter(Mandatory = $true)]
		[string]$PackageId,
		[Parameter(Mandatory = $true)]
		[string]$Version,
		[Parameter(Mandatory = $true)]
		[int]$HttpTimeoutSeconds,
		[Parameter(Mandatory = $true)]
		[int]$MaxAttempts,
		[Parameter(Mandatory = $true)]
		[int]$RetryDelaySeconds
	)

	$packageIdLower = $PackageId.ToLowerInvariant()
	$versionLower = $Version.ToLowerInvariant()
	$nuspecUrl = "https://api.nuget.org/v3-flatcontainer/$packageIdLower/$versionLower/$packageIdLower.nuspec"

	$nuspecContent = $null
	for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
		try {
			$nuspecContent = Invoke-RestMethod -Uri $nuspecUrl -Method Get -TimeoutSec $HttpTimeoutSeconds -ErrorAction Stop
			break
		}
		catch {
			Write-Host "Attempt $($attempt)/$($MaxAttempts): Unable to query nuspec $nuspecUrl. $_"

			if ($attempt -ge $MaxAttempts) {
				throw "Failed to retrieve nuspec for package '$PackageId' version '$Version' from '$nuspecUrl' after $MaxAttempts attempt(s)."
			}

			Start-Sleep -Seconds $RetryDelaySeconds
		}
	}

	[xml]$nuspec = $nuspecContent
	$dependencyNodes = @($nuspec.SelectNodes('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="dependencies"]/*[local-name()="dependency"]'))
	$dependencyNodes += @($nuspec.SelectNodes('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="dependencies"]/*[local-name()="group"]/*[local-name()="dependency"]'))

	$result = New-Object System.Collections.Generic.List[object]

	foreach ($dependencyNode in $dependencyNodes) {
		if ($null -eq $dependencyNode -or [string]::IsNullOrWhiteSpace([string]$dependencyNode.id) -or [string]::IsNullOrWhiteSpace([string]$dependencyNode.version)) {
			continue
		}

		$exactVersion = Get-ExactDependencyVersion -VersionRange ([string]$dependencyNode.version)
		if ([string]::IsNullOrWhiteSpace($exactVersion)) {
			continue
		}

		$result.Add([PSCustomObject]@{
			Id = [string]$dependencyNode.id
			Version = $exactVersion
		})
	}

	return @($result | Sort-Object Id, Version -Unique)
}

function Add-ChecksFromPackageGroups {
	# Parse a packages.json catalog (an array of { group, version, packages[], versionOverride? }
	# — the Uno.Sdk manifest shape) and add a check for every package at its group base version,
	# plus any per-target-framework versionOverride. Shared by artifact mode (packages.json
	# embedded in a produced .nupkg) and manifest mode (the source src/Uno.Sdk/packages.json).
	param(
		[Parameter(Mandatory = $true)]
		[AllowEmptyCollection()]
		[System.Collections.Generic.List[object]]$Checks,
		[Parameter(Mandatory = $true)]
		[string]$JsonContent,
		[Parameter(Mandatory = $true)]
		[string]$SourceLabel
	)

	$parsedGroups = $JsonContent | ConvertFrom-Json
	$packageGroups = @()
	if ($parsedGroups -is [System.Array]) {
		$packageGroups += $parsedGroups
	}
	elseif ($null -ne $parsedGroups) {
		$packageGroups += ,$parsedGroups
	}

	foreach ($group in $packageGroups) {
		$groupName = [string]$group.group
		$baseVersion = [string]$group.version

		foreach ($packageId in @($group.packages)) {
			Add-DependencyCheck -Checks $Checks -PackageId ([string]$packageId) -Version $baseVersion -Source "$($SourceLabel):$groupName"
		}

		$versionOverrideProperty = $group.PSObject.Properties['versionOverride']
		if ($null -ne $versionOverrideProperty -and $null -ne $versionOverrideProperty.Value) {
			foreach ($overrideProperty in @($versionOverrideProperty.Value.PSObject.Properties)) {
				$overrideTargetFramework = [string]$overrideProperty.Name
				$overrideVersion = [string]$overrideProperty.Value

				if ([string]::IsNullOrWhiteSpace($overrideTargetFramework) -or [string]::IsNullOrWhiteSpace($overrideVersion)) {
					continue
				}

				foreach ($packageId in @($group.packages)) {
					Add-DependencyCheck -Checks $Checks -PackageId ([string]$packageId) -Version $overrideVersion -Source "$($SourceLabel):$($groupName):$overrideTargetFramework"
				}
			}
		}
	}
}

function Add-ChecksFromManifest {
	# Manifest mode (pre-build fail-fast): read the source Uno.Sdk packages.json and add a
	# check for every package it lists, so the pipeline can verify the Uno.* dependency
	# closure on nuget.org BEFORE the build and template test matrix consume runners.
	param(
		[Parameter(Mandatory = $true)]
		[AllowEmptyCollection()]
		[System.Collections.Generic.List[object]]$Checks,
		[Parameter(Mandatory = $true)]
		[string]$ManifestPath
	)

	if (-not (Test-Path $ManifestPath)) {
		Add-StepSummaryLines -Lines @(
			"## NuGet Dependency Verification",
			"❌ Failed: manifest path '$ManifestPath' was not found."
		)

		throw "Manifest path '$ManifestPath' was not found."
	}

	Write-Host "Inspecting package catalog from manifest '$ManifestPath'..."
	$manifestContent = Get-Content -Path $ManifestPath -Raw

	try {
		Add-ChecksFromPackageGroups -Checks $Checks -JsonContent $manifestContent -SourceLabel "manifest:$(Split-Path $ManifestPath -Leaf)"
	}
	catch {
		throw "Failed to parse manifest '$ManifestPath': $_"
	}
}

function Add-ChecksFromArtifacts {
	# Artifact mode (authoritative publish gate): inspect the produced .nupkg files —
	# their direct nuspec dependencies and any embedded packages.json catalog.
	param(
		[Parameter(Mandatory = $true)]
		[AllowEmptyCollection()]
		[System.Collections.Generic.List[object]]$Checks,
		[Parameter(Mandatory = $true)]
		[string]$PackagesPath,
		[string]$PackageIdFilter
	)

	if (-not (Test-Path $PackagesPath)) {
		Add-StepSummaryLines -Lines @(
			"## NuGet Dependency Verification",
			"❌ Failed: packages path '$PackagesPath' was not found."
		)

		throw "Packages path '$PackagesPath' was not found."
	}

	$packageFiles = Get-ChildItem -Path $PackagesPath -Filter "*.nupkg" -File |
		Where-Object {
			if ([string]::IsNullOrWhiteSpace($PackageIdFilter)) {
				return $true
			}

			return $_.BaseName -like "$PackageIdFilter*"
		}

	if (-not $packageFiles) {
		if ([string]::IsNullOrWhiteSpace($PackageIdFilter)) {
			Add-StepSummaryLines -Lines @(
				"## NuGet Dependency Verification",
				"❌ Failed: no .nupkg package was found in '$PackagesPath'."
			)

			throw "No .nupkg package was found in '$PackagesPath'."
		}

		Add-StepSummaryLines -Lines @(
			"## NuGet Dependency Verification",
			"❌ Failed: no package matching '$PackageIdFilter*.nupkg' was found in '$PackagesPath'."
		)

		throw "No package matching '$PackageIdFilter*.nupkg' was found in '$PackagesPath'."
	}

	Add-Type -AssemblyName System.IO.Compression.FileSystem

	foreach ($packageFile in $packageFiles) {
		Write-Host "Inspecting dependencies for package '$($packageFile.Name)'..."

		$archive = [System.IO.Compression.ZipFile]::OpenRead($packageFile.FullName)
		try {
			$nuspecEntry = $archive.Entries | Where-Object { $_.FullName -like "*.nuspec" } | Select-Object -First 1
			if ($null -eq $nuspecEntry) {
				throw "No .nuspec entry was found in '$($packageFile.FullName)'."
			}

			$nuspecContent = Read-ZipEntryContent -Entry $nuspecEntry

			[xml]$nuspec = $nuspecContent
			$dependencyNodes = @($nuspec.SelectNodes('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="dependencies"]/*[local-name()="dependency"]'))
			$dependencyNodes += @($nuspec.SelectNodes('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="dependencies"]/*[local-name()="group"]/*[local-name()="dependency"]'))

			$dependencies = $dependencyNodes |
				Where-Object { $_ -and $_.id -and $_.version } |
				ForEach-Object {
					[PSCustomObject]@{
						Id = $_.id
						VersionRange = $_.version
					}
				} |
				Sort-Object Id, VersionRange -Unique

			foreach ($dependency in $dependencies) {
				$exactVersion = Get-ExactDependencyVersion -VersionRange $dependency.VersionRange
				if ([string]::IsNullOrWhiteSpace($exactVersion)) {
					Write-Host "Skipping non-exact version range '$($dependency.VersionRange)' for dependency '$($dependency.Id)'."
					continue
				}

				Add-DependencyCheck -Checks $Checks -PackageId $dependency.Id -Version $exactVersion -Source "nuspec:$($packageFile.Name)"
			}

			$packagesJsonEntries = @($archive.Entries | Where-Object { $_.FullName -match '(^|/)packages\.json$' })
			foreach ($packagesJsonEntry in $packagesJsonEntries) {
				Write-Host "Inspecting package catalog '$($packagesJsonEntry.FullName)' in '$($packageFile.Name)'..."
				$packagesJsonContent = Read-ZipEntryContent -Entry $packagesJsonEntry

				try {
					Add-ChecksFromPackageGroups -Checks $Checks -JsonContent $packagesJsonContent -SourceLabel "packages.json:$($packageFile.Name)"
				}
				catch {
					throw "Failed to parse '$($packagesJsonEntry.FullName)' from '$($packageFile.Name)': $_"
				}
			}
		}
		finally {
			$archive.Dispose()
		}
	}
}

if ($MyInvocation.InvocationName -eq '.') {
	# Dot-sourced (e.g. from the Pester tests): load the functions only and skip the
	# artifact-scanning / nuget.org entry point below so unit tests run offline.
	return
}

$checks = New-Object System.Collections.Generic.List[object]
$missingDependencies = @{}
$availabilityCache = @{}

if (-not [string]::IsNullOrWhiteSpace($ManifestPath)) {
	# Manifest mode: fail fast on the source Uno.Sdk manifest before build/tests run.
	Add-ChecksFromManifest -Checks $checks -ManifestPath $ManifestPath
}
else {
	# Artifact mode: authoritative gate on the produced packages before publish.
	Add-ChecksFromArtifacts -Checks $checks -PackagesPath $PackagesPath -PackageIdFilter $PackageIdFilter
}

# PR fail-fast pass: drop stable coordinates so only prerelease (dev) dependencies are verified.
# No-op unless -PrereleaseDependenciesOnly is set (dev-publish / pre-prod-publish verify everything).
$checks = Select-ChecksForVerification -Checks $checks -PrereleaseDependenciesOnly ([bool]$PrereleaseDependenciesOnly)

$uniqueChecks = @($checks |
	Sort-Object Id, Version -Unique)

# Coordinates skipped during expansion because they were not available at probe time. If any of
# these turns out to be available in the final verification (eventual consistency / just-published),
# its transitive closure was never walked — we fail (re-run) rather than risk a silent false pass.
$deferredDuringExpansion = New-Object System.Collections.Generic.HashSet[string]

if ($TransitiveDependencyDepth -gt 0) {
	$expandedKeys = New-Object System.Collections.Generic.HashSet[string]

	# Seed the frontier with the directly-referenced coordinates at depth 0, then walk
	# breadth-first. Non-tracked packages are expanded up to TransitiveDependencyDepth
	# hops; tracked (Uno.*) packages are walked to their full closure so an unpublished
	# tracked package at ANY depth is caught. Coordinate de-duplication ($expandedKeys)
	# keeps the walk finite; TrackedTransitiveDependencyMaxDepth is a runaway guard.
	$frontier = New-Object System.Collections.Generic.List[object]
	foreach ($seed in $uniqueChecks) {
		$frontier.Add([PSCustomObject]@{ Id = $seed.Id; Version = $seed.Version; Source = $seed.Source; Depth = 0 })
	}

	$round = 0
	while ($frontier.Count -gt 0) {
		$round++
		Write-Host "Expanding transitive dependencies (round $round) for $($frontier.Count) package/version coordinate(s)..."

		$nextFrontier = New-Object System.Collections.Generic.List[object]

		foreach ($coordinate in ($frontier | Sort-Object Id, Version -Unique)) {
			$coordinateKey = "$($coordinate.Id)|$($coordinate.Version)"
			if (-not $expandedKeys.Add($coordinateKey)) {
				continue
			}

			if (-not (Test-ShouldExpandCoordinate -PackageId ([string]$coordinate.Id) -Depth $coordinate.Depth -TransitiveDependencyDepth $TransitiveDependencyDepth -MaxTrackedDepth $TrackedTransitiveDependencyMaxDepth -IncludePrefixes $StableTransitiveIncludePrefixes)) {
				# Leaf for the walk: already recorded in $checks when discovered, and its
				# availability is confirmed by the final verification pass below.
				continue
			}

			# Probe availability before walking this coordinate's dependencies. A definitive
			# 'Absent' returns immediately (genuinely-missing versions fail fast); a transient
			# blip is retried (MaxAttempts) so it can't silently skip this coordinate's transitive
			# closure and hide unpublished deep dependencies. Only positives are cached; the
			# missing-dependency reporting happens in the final verification pass below.
			if ($availabilityCache.ContainsKey($coordinateKey) -and [bool]$availabilityCache[$coordinateKey]) {
				$coordinateAvailable = $true
			}
			else {
				$coordinateAvailable = Test-NuGetVersionAvailability -PackageId $coordinate.Id -Version $coordinate.Version -MaxAttempts $MaxAttempts -RetryDelaySeconds $RetryDelaySeconds -HttpTimeoutSeconds $HttpTimeoutSeconds
				if ($coordinateAvailable) {
					$availabilityCache[$coordinateKey] = $true
				}
			}

			if (-not $coordinateAvailable) {
				# Genuinely missing (or unreachable after retries): its nuspec can't be fetched, so
				# stop walking this branch. Final verification reports it as missing. Record it so
				# that, if it turns out available later, we fail instead of hiding its closure.
				[void]$deferredDuringExpansion.Add($coordinateKey)
				Write-Host "Deferring '$($coordinate.Id)' $($coordinate.Version) to final verification (not available)."
				continue
			}

			$transitiveDependencies = Get-ExactDependenciesFromPackageNuspec -PackageId $coordinate.Id -Version $coordinate.Version -HttpTimeoutSeconds $HttpTimeoutSeconds -MaxAttempts $MaxAttempts -RetryDelaySeconds $RetryDelaySeconds
			foreach ($transitiveDependency in $transitiveDependencies) {
				$depId = [string]$transitiveDependency.Id
				if (-not (Test-ShouldVerifyTransitiveDependency -PackageId $depId -Version ([string]$transitiveDependency.Version) -IncludeStableTransitiveVersions ([bool]$IncludeStableTransitiveVersions) -IncludePrefixes $StableTransitiveIncludePrefixes -PrereleaseDependenciesOnly ([bool]$PrereleaseDependenciesOnly))) {
					continue
				}

				$transitiveSource = "transitive:$($coordinate.Id):$($coordinate.Version)"
				Add-DependencyCheck -Checks $checks -PackageId $depId -Version $transitiveDependency.Version -Source $transitiveSource
				$nextFrontier.Add([PSCustomObject]@{ Id = $depId; Version = $transitiveDependency.Version; Source = $transitiveSource; Depth = ($coordinate.Depth + 1) })
			}
		}

		$frontier = $nextFrontier
	}

	$uniqueChecks = @($checks |
		Sort-Object Id, Version -Unique)
}

$sourcesByDependencyKey = Build-DependencySourcesMap -Checks $checks

Write-Host "Checking $($uniqueChecks.Count) unique package/version coordinate(s) on nuget.org..."

# Set-based retry: sweep every coordinate once (single attempt each), then retry ONLY the
# still-missing set between rounds. Total wait is bounded by (MaxAttempts - 1) * RetryDelaySeconds
# regardless of how many coordinates are missing, so a genuinely absent version fails fast
# instead of each missing coordinate serially exhausting the whole retry budget.
$pending = @($uniqueChecks)
$stillMissing = New-Object System.Collections.Generic.List[object]

for ($round = 1; $round -le $MaxAttempts; $round++) {
	$stillMissing = New-Object System.Collections.Generic.List[object]

	foreach ($check in $pending) {
		$cacheKey = "$($check.Id)|$($check.Version)"

		if ($availabilityCache.ContainsKey($cacheKey) -and [bool]$availabilityCache[$cacheKey]) {
			continue
		}

		$available = Test-NuGetVersionAvailability -PackageId $check.Id -Version $check.Version -MaxAttempts 1 -RetryDelaySeconds 0 -HttpTimeoutSeconds $HttpTimeoutSeconds
		if ($available) {
			$availabilityCache[$cacheKey] = $true
			Write-Host "Verified dependency '$($check.Id)' version '$($check.Version)' on nuget.org."
		}
		else {
			$stillMissing.Add($check)
		}
	}

	if ($stillMissing.Count -eq 0) {
		break
	}

	if ($round -lt $MaxAttempts) {
		Write-Host "$($stillMissing.Count) dependency/ies not available on nuget.org yet; retrying in $RetryDelaySeconds second(s) (round $round/$MaxAttempts)..."
		Start-Sleep -Seconds $RetryDelaySeconds
	}

	$pending = $stillMissing
}

foreach ($check in $stillMissing) {
	Add-MissingDependencyRecord -MissingDependencies $missingDependencies -PackageId $check.Id -Version $check.Version -Source ([string]$check.Source) -SourcesByDependencyKey $sourcesByDependencyKey
	Write-Host "Missing dependency '$($check.Id)' version '$($check.Version)' (source: $($check.Source))."
}

# Report mode always writes the missing set (even when empty) so the PR job can act deterministically.
if ($ReportOnly -and -not [string]::IsNullOrWhiteSpace($ReportOutputPath)) {
	$reportPayload = ConvertTo-MissingDependencyReport -MissingDependencies $missingDependencies
	($reportPayload | ConvertTo-Json -Depth 5 -AsArray) | Set-Content -Path $ReportOutputPath -Encoding utf8
	Write-Host "ReportOnly: wrote $($reportPayload.Count) missing dependency record(s) to '$ReportOutputPath'."
}

if ($missingDependencies.Count -gt 0) {
	$missingList = $missingDependencies.Values |
		Sort-Object Id, Version |
		ForEach-Object { "$($_.Id) $($_.Version)" }

	if ($ReportOnly) {
		$summaryLines = @(
			"## NuGet Dependency Verification",
			"⚠️ Prerelease (dev) dependencies not yet on nuget.org (informational — not blocking this PR):"
		)
		$summaryLines += ($missingList | ForEach-Object { "- $_" })
		$summaryLines += "These must be published before the dev/prod publish gate will pass."
		Add-StepSummaryLines -Lines $summaryLines

		Write-Host "ReportOnly: $($missingDependencies.Count) dependency/ies not on nuget.org (not failing)."
	}
	else {
		$summaryLines = @(
			"## NuGet Dependency Verification",
			"❌ Missing dependencies on nuget.org:"
		)
		$summaryLines += ($missingList | ForEach-Object { "- $_" })
		$summaryLines += "Publish to nuget.org was blocked to avoid unresolved dependencies."

		Add-StepSummaryLines -Lines $summaryLines

		$messageLines = @("The following dependencies are not available on nuget.org:")
		$messageLines += ($missingList | ForEach-Object { " - $_" })
		$messageLines += "Aborting publish to avoid pushing a package with unresolved dependencies."
		$message = $messageLines -join [Environment]::NewLine

		throw $message
	}
}

# Safety net for the transitive walk: a coordinate skipped during expansion (not available at
# probe time) that is now confirmed available means its transitive closure was never walked, so an
# unpublished deep dependency behind it could be hidden. Fail (re-run) rather than risk a false pass.
$unwalkedButAvailable = @(
	$deferredDuringExpansion | Where-Object { $availabilityCache.ContainsKey($_) -and [bool]$availabilityCache[$_] } | Sort-Object -Unique
)

if ($unwalkedButAvailable.Count -gt 0 -and -not $ReportOnly) {
	$summaryLines = @(
		"## NuGet Dependency Verification",
		"❌ Could not fully verify the transitive closure. These coordinates were skipped during dependency-tree expansion (not available at the time) but are now available, so their dependencies were not walked — re-run the check:"
	)
	$summaryLines += ($unwalkedButAvailable | ForEach-Object { "- $_" })
	Add-StepSummaryLines -Lines $summaryLines

	$messageLines = @("Could not fully verify the transitive dependency closure. The following coordinates were unavailable during expansion but are now available, so their dependencies were not walked:")
	$messageLines += ($unwalkedButAvailable | ForEach-Object { " - $_" })
	$messageLines += "This is usually a transient nuget.org/CDN timing effect — re-run the check."
	$message = $messageLines -join [Environment]::NewLine

	throw $message
}

# Success summary only when nothing was missing. In blocking mode a miss already threw above; in
# report mode the run continues, so this guard prevents a misleading "all available" after a warning.
if ($missingDependencies.Count -eq 0) {
	$verifiedLines = @($uniqueChecks | Sort-Object Id, Version | ForEach-Object { "- $($_.Id) $($_.Version)" })
	$successSummary = @(
		"## NuGet Dependency Verification",
		"✅ All checked package dependencies are available on nuget.org.",
		"",
		"<details><summary>Verified dependencies ($($uniqueChecks.Count))</summary>",
		""
	)
	$successSummary += $verifiedLines
	$successSummary += @("", "</details>")
	Add-StepSummaryLines -Lines $successSummary

	Write-Host "All checked dependencies are available on nuget.org."
}