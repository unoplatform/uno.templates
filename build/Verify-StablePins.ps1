param(
	# The Uno.Sdk manifest to validate.
	[string]$ManifestPath = "src/Uno.Sdk/packages.json",
	# The ReadMe that embeds a copy of the manifest. The updater rewrites both together, so a
	# mismatch means one of them was edited by hand and the shipped package would disagree with
	# its own documentation.
	[string]$ReadMePath = "src/Uno.Sdk/ReadMe.md",
	# The MSBuild property files the updater writes alongside the manifest. They carry the same
	# pins as MSBuild properties, so a dev version can ship through them even when packages.json
	# is clean.
	[string[]]$PropertyFilePaths = @("src/Uno.Sdk.Updater.targets", "src/Uno.Sdk/Uno.Sdk.Updater.props"),
	# Branch the manifest is destined for. Enforcement only applies to stable release branches;
	# main and servicing lines are expected to carry dev pins.
	[string]$Branch = "",
	# Validate regardless of branch (used by the Pester tests and for local runs).
	[switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

<#
.SYNOPSIS
	True when a NuGet version string carries a SemVer prerelease label.
.DESCRIPTION
	Prerelease is the hyphen-delimited segment BEFORE any build-metadata '+' segment.
	'1.2.3-dev.4' is prerelease; '1.2.3', '1.2.30405060' and '1.2.3+abc' are not.
#>
function Test-IsPrereleaseVersion {
	param([Parameter(Mandatory)][AllowEmptyString()][string]$Version)

	if ([string]::IsNullOrWhiteSpace($Version)) {
		return $false
	}

	$core = $Version.Split('+')[0]
	return $core.Contains('-')
}

<#
.SYNOPSIS
	True for branches that ship a stable Uno.Sdk (release/stable/<version>).
#>
function Test-IsStableReleaseBranch {
	param([Parameter(Mandatory)][AllowEmptyString()][string]$Branch)

	if ([string]::IsNullOrWhiteSpace($Branch)) {
		return $false
	}

	$normalized = $Branch -replace '^refs/heads/', ''
	return $normalized -match '^release/stable/'
}

<#
.SYNOPSIS
	Flattens a parsed packages.json into one record per pinned version.
.DESCRIPTION
	Covers both the group 'version' and every 'versionOverride' entry, because a dev pin hides
	just as easily in a per-TFM override as in the group default.
#>
function Get-ManifestPinnedVersions {
	param([Parameter(Mandatory)]$Manifest)

	$pins = @()

	foreach ($group in $Manifest) {
		$groupName = $group.group

		if ($group.PSObject.Properties.Name -contains 'version') {
			$pins += [pscustomobject]@{
				Group   = $groupName
				Field   = 'version'
				Version = $group.version
			}
		}

		if ($group.PSObject.Properties.Name -contains 'versionOverride' -and $null -ne $group.versionOverride) {
			foreach ($override in $group.versionOverride.PSObject.Properties) {
				$pins += [pscustomobject]@{
					Group   = $groupName
					Field   = "versionOverride[$($override.Name)]"
					Version = $override.Value
				}
			}
		}
	}

	# Comma operator: without it PowerShell unrolls an empty array to $null and callers lose .Count.
	return ,$pins
}

<#
.SYNOPSIS
	Returns the pins that carry a prerelease label.
#>
function Get-PrereleasePins {
	param([Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Pins)

	# Comma operator: see Get-ManifestPinnedVersions - keeps an empty result an array.
	return ,@($Pins | Where-Object { Test-IsPrereleaseVersion -Version $_.Version })
}

<#
.SYNOPSIS
	Reads the version-bearing MSBuild properties out of a .props/.targets file.
.DESCRIPTION
	Matches any element whose name contains 'Version' (UnoVersion, SdkVersion,
	UnoWasmBootstrapVersionNet10, ...), which is how the updater records its pins outside the
	JSON manifest.
#>
function Get-MSBuildPinnedVersions {
	param(
		[Parameter(Mandatory)][AllowEmptyString()][string]$Content,
		[Parameter(Mandatory)][AllowEmptyString()][string]$SourceName
	)

	$pins = @()

	foreach ($match in [regex]::Matches($Content, '<([A-Za-z0-9_]*Version[A-Za-z0-9_]*)>\s*([^<]+?)\s*</\1>')) {
		$pins += [pscustomobject]@{
			Group   = $SourceName
			Field   = $match.Groups[1].Value
			Version = $match.Groups[2].Value
		}
	}

	# Comma operator: see Get-ManifestPinnedVersions.
	return ,$pins
}

<#
.SYNOPSIS
	Extracts the JSON array embedded in the ReadMe's first ```json fence.
.OUTPUTS
	The raw JSON text, or $null when no fenced block is present.
#>
function Get-ReadMeEmbeddedManifestJson {
	param([Parameter(Mandatory)][AllowEmptyString()][string]$Content)

	$match = [regex]::Match($Content, '(?s)```json\s*(.*?)```')
	if (-not $match.Success) {
		return $null
	}

	return $match.Groups[1].Value.Trim()
}

<#
.SYNOPSIS
	True when the ReadMe's embedded copy agrees with the manifest.
.DESCRIPTION
	Compared as normalized JSON so indentation and line endings do not produce false failures.
#>
function Test-ReadMeMatchesManifest {
	param(
		[Parameter(Mandatory)][AllowEmptyString()][string]$ManifestJson,
		[Parameter(Mandatory)][AllowEmptyString()][string]$ReadMeContent
	)

	$embedded = Get-ReadMeEmbeddedManifestJson -Content $ReadMeContent
	if ($null -eq $embedded) {
		return $false
	}

	try {
		$left = ($ManifestJson | ConvertFrom-Json) | ConvertTo-Json -Depth 32 -Compress
		$right = ($embedded | ConvertFrom-Json) | ConvertTo-Json -Depth 32 -Compress
	}
	catch {
		return $false
	}

	return $left -eq $right
}

# Guarded so the Pester tests can dot-source the functions without running the validation.
if ($MyInvocation.InvocationName -ne '.') {
	if (-not $Force -and -not (Test-IsStableReleaseBranch -Branch $Branch)) {
		Write-Host "Branch '$Branch' is not a stable release branch - prerelease pins are allowed here. Skipping."
		exit 0
	}

	if (-not (Test-Path $ManifestPath)) {
		throw "Manifest not found: $ManifestPath"
	}

	$manifestJson = Get-Content -Raw -Path $ManifestPath
	$manifest = $manifestJson | ConvertFrom-Json

	$pins = Get-ManifestPinnedVersions -Manifest $manifest
	$prerelease = Get-PrereleasePins -Pins $pins

	$failed = $false

	if ($prerelease.Count -gt 0) {
		$failed = $true
		Write-Host "::error::$ManifestPath pins $($prerelease.Count) prerelease version(s) on a stable release branch."
		foreach ($pin in $prerelease) {
			Write-Host "::error file=$ManifestPath::$($pin.Group) -> $($pin.Field) = $($pin.Version)"
		}
		Write-Host ""
		Write-Host "A stable Uno.Sdk ships stable pins. A release branch inherits whatever dev versions the"
		Write-Host "branch it was cut from carried, so each one has to be moved to a stable version before the"
		Write-Host "release: the previous stable when nothing changed, or a new or bumped stable when the"
		Write-Host "release needs one."
	}
	else {
		Write-Host "OK: all $($pins.Count) pins in $ManifestPath are stable."
	}

	foreach ($propertyFile in $PropertyFilePaths) {
		if (-not (Test-Path $propertyFile)) {
			continue
		}

		$propertyPins = Get-MSBuildPinnedVersions -Content (Get-Content -Raw -Path $propertyFile) -SourceName (Split-Path -Leaf $propertyFile)
		$propertyPrerelease = Get-PrereleasePins -Pins $propertyPins

		if ($propertyPrerelease.Count -gt 0) {
			$failed = $true
			Write-Host "::error::$propertyFile pins $($propertyPrerelease.Count) prerelease version(s) on a stable release branch."
			foreach ($pin in $propertyPrerelease) {
				Write-Host "::error file=$propertyFile::$($pin.Field) = $($pin.Version)"
			}
		}
		else {
			Write-Host "OK: all $($propertyPins.Count) pins in $propertyFile are stable."
		}
	}

	if (Test-Path $ReadMePath) {
		$readMe = Get-Content -Raw -Path $ReadMePath
		if (-not (Test-ReadMeMatchesManifest -ManifestJson $manifestJson -ReadMeContent $readMe)) {
			$failed = $true
			Write-Host "::error file=$ReadMePath::The manifest embedded in $ReadMePath does not match $ManifestPath."
			Write-Host "The updater rewrites both together; a mismatch means one was edited by hand."
		}
		else {
			Write-Host "OK: $ReadMePath matches $ManifestPath."
		}
	}

	if ($failed) {
		exit 1
	}
}
