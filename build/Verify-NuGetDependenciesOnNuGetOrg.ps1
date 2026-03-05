param(
	[string]$PackagesPath = "artifacts",
	[string]$PackageIdFilter = "",
	[int]$MaxAttempts = 12,
	[int]$RetryDelaySeconds = 20,
	[int]$HttpTimeoutSeconds = 30,
	[int]$TransitiveDependencyDepth = 1,
	[switch]$IncludeStableTransitiveVersions
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

function Test-NuGetVersionAvailability {
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

	$packageIdLower = $PackageId.ToLowerInvariant()
	$expectedVersion = $Version.ToLowerInvariant()
	$indexUrl = "https://api.nuget.org/v3-flatcontainer/$packageIdLower/index.json"

	for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
		try {
			$response = Invoke-RestMethod -Uri $indexUrl -Method Get -TimeoutSec $HttpTimeoutSeconds
			if ($null -ne $response.versions -and $response.versions -contains $expectedVersion) {
				return $true
			}
		}
		catch {
			Write-Host "Unable to query $indexUrl on attempt $attempt/$MaxAttempts. $_"
		}

		if ($attempt -lt $MaxAttempts) {
			Write-Host "Dependency $PackageId $Version is not available yet on nuget.org. Waiting $RetryDelaySeconds second(s) before retrying..."
			Start-Sleep -Seconds $RetryDelaySeconds
		}
	}

	return $false
}

function Add-StepSummaryLines {
	param(
		[Parameter(Mandatory = $true)]
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

$checks = New-Object System.Collections.Generic.List[object]
$missingDependencies = @{}
$availabilityCache = @{}

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

			Add-DependencyCheck -Checks $checks -PackageId $dependency.Id -Version $exactVersion -Source "nuspec:$($packageFile.Name)"
		}

		$packagesJsonEntries = @($archive.Entries | Where-Object { $_.FullName -match '(^|/)packages\.json$' })
		foreach ($packagesJsonEntry in $packagesJsonEntries) {
			Write-Host "Inspecting package catalog '$($packagesJsonEntry.FullName)' in '$($packageFile.Name)'..."
			$packagesJsonContent = Read-ZipEntryContent -Entry $packagesJsonEntry

			try {
				$parsedGroups = $packagesJsonContent | ConvertFrom-Json
				$packageGroups = @()
				if ($parsedGroups -is [System.Array]) {
					$packageGroups += $parsedGroups
				}
				elseif ($null -ne $parsedGroups) {
					$packageGroups += ,$parsedGroups
				}
			}
			catch {
				throw "Failed to parse '$($packagesJsonEntry.FullName)' from '$($packageFile.Name)': $_"
			}

			foreach ($group in $packageGroups) {
				$groupName = [string]$group.group
				$baseVersion = [string]$group.version

				foreach ($packageId in @($group.packages)) {
					Add-DependencyCheck -Checks $checks -PackageId ([string]$packageId) -Version $baseVersion -Source "packages.json:$($packageFile.Name):$groupName"
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
							Add-DependencyCheck -Checks $checks -PackageId ([string]$packageId) -Version $overrideVersion -Source "packages.json:$($packageFile.Name):$($groupName):$overrideTargetFramework"
						}
					}
				}
			}
		}
	}
	finally {
		$archive.Dispose()
	}
}


$uniqueChecks = @($checks |
	Sort-Object Id, Version -Unique)

if ($TransitiveDependencyDepth -gt 0) {
	$expandedKeys = New-Object System.Collections.Generic.HashSet[string]
	$frontier = @($uniqueChecks)

	for ($depth = 1; $depth -le $TransitiveDependencyDepth; $depth++) {
		if ($frontier.Count -eq 0) {
			break
		}

		Write-Host "Expanding transitive dependencies (depth $depth/$TransitiveDependencyDepth) for $($frontier.Count) package/version coordinate(s)..."

		$nextFrontier = New-Object System.Collections.Generic.List[object]

		foreach ($coordinate in $frontier) {
			$coordinateKey = "$($coordinate.Id)|$($coordinate.Version)"
			if (-not $expandedKeys.Add($coordinateKey)) {
				continue
			}

			if ($availabilityCache.ContainsKey($coordinateKey)) {
				$coordinateAvailable = [bool]$availabilityCache[$coordinateKey]
			}
			else {
				$coordinateAvailable = Test-NuGetVersionAvailability -PackageId $coordinate.Id -Version $coordinate.Version -MaxAttempts $MaxAttempts -RetryDelaySeconds $RetryDelaySeconds -HttpTimeoutSeconds $HttpTimeoutSeconds
				$availabilityCache[$coordinateKey] = $coordinateAvailable
			}

			if (-not $coordinateAvailable) {
				Add-MissingDependencyRecord -MissingDependencies $missingDependencies -PackageId $coordinate.Id -Version $coordinate.Version -Source ([string]$coordinate.Source) -SourcesByDependencyKey $null
				Write-Host "Skipping transitive expansion for missing dependency '$($coordinate.Id)' version '$($coordinate.Version)'."
				continue
			}

			$transitiveDependencies = Get-ExactDependenciesFromPackageNuspec -PackageId $coordinate.Id -Version $coordinate.Version -HttpTimeoutSeconds $HttpTimeoutSeconds -MaxAttempts $MaxAttempts -RetryDelaySeconds $RetryDelaySeconds
			foreach ($transitiveDependency in $transitiveDependencies) {
				if (-not $IncludeStableTransitiveVersions -and -not [string]::IsNullOrWhiteSpace($transitiveDependency.Version) -and -not $transitiveDependency.Version.Contains('-')) {
					continue
				}

				Add-DependencyCheck -Checks $checks -PackageId $transitiveDependency.Id -Version $transitiveDependency.Version -Source "transitive:$($coordinate.Id):$($coordinate.Version)"
				$nextFrontier.Add($transitiveDependency)
			}
		}

		$frontier = @($nextFrontier | Sort-Object Id, Version -Unique)
	}

	$uniqueChecks = @($checks |
		Sort-Object Id, Version -Unique)
}

$sourcesByDependencyKey = Build-DependencySourcesMap -Checks $checks

Write-Host "Checking $($uniqueChecks.Count) unique package/version coordinate(s) on nuget.org..."

foreach ($check in $uniqueChecks) {
	$cacheKey = "$($check.Id)|$($check.Version)"

	if ($availabilityCache.ContainsKey($cacheKey)) {
		$available = [bool]$availabilityCache[$cacheKey]
	}
	else {
		$available = Test-NuGetVersionAvailability -PackageId $check.Id -Version $check.Version -MaxAttempts $MaxAttempts -RetryDelaySeconds $RetryDelaySeconds -HttpTimeoutSeconds $HttpTimeoutSeconds
		$availabilityCache[$cacheKey] = $available
	}

	if ($available) {
		Write-Host "Verified dependency '$($check.Id)' version '$($check.Version)' on nuget.org."
	}
	else {
		Add-MissingDependencyRecord -MissingDependencies $missingDependencies -PackageId $check.Id -Version $check.Version -Source ([string]$check.Source) -SourcesByDependencyKey $sourcesByDependencyKey
		Write-Host "Missing dependency '$($check.Id)' version '$($check.Version)' (source: $($check.Source))."
	}
}

if ($missingDependencies.Count -gt 0) {
	$missingList = $missingDependencies.Values |
		Sort-Object Id, Version |
		ForEach-Object { "$($_.Id) $($_.Version)" }

	$summaryLines = @(
		"## NuGet Dependency Verification",
		"❌ Missing dependencies on nuget.org:"
	)
	$summaryLines += ($missingList | ForEach-Object { "- $_" })
	$summaryLines += "Publish to nuget.org was blocked to avoid unresolved dependencies."

	Add-StepSummaryLines -Lines $summaryLines

	$message = @(
		"The following dependencies are not available on nuget.org:",
		($missingList | ForEach-Object { " - $_" }),
		"Aborting publish to avoid pushing a package with unresolved dependencies."
	) -join [Environment]::NewLine

	throw $message
}

Add-StepSummaryLines -Lines @(
	"## NuGet Dependency Verification",
	"✅ All checked package dependencies are available on nuget.org."
)

Write-Host "All checked dependencies are available on nuget.org."