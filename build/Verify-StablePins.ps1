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
	# Prerelease pins a stable release ships on purpose, each with its reason, such as a dependency
	# that has no stable version yet. Matched by group and field, so the version itself can still
	# move; any other prerelease pin fails.
	[string]$AllowedPrereleasesPath = "build/Verify-StablePins.AllowedPrereleases.json",
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
	Reads the allowed prerelease pins: one entry per group and field, each with its reason.
.DESCRIPTION
	For an MSBuild property file, the group is the file name and the field the property name, as
	Get-MSBuildPinnedVersions records them. An entry without a reason is rejected, so every
	exception says why it ships.
#>
function Get-AllowedPrereleases {
	param([Parameter(Mandatory)][AllowEmptyString()][string]$Json)

	if ([string]::IsNullOrWhiteSpace($Json)) {
		return ,@()
	}

	$entries = @($Json | ConvertFrom-Json)
	foreach ($entry in $entries) {
		foreach ($name in 'group', 'field', 'reason') {
			if (-not ($entry.PSObject.Properties.Name -contains $name) -or [string]::IsNullOrWhiteSpace($entry.$name)) {
				throw "Every allowed prerelease needs a '$name': $($entry | ConvertTo-Json -Compress)"
			}
		}
	}

	# Comma operator: see Get-ManifestPinnedVersions.
	return ,$entries
}

<#
.SYNOPSIS
	Splits prerelease pins into the ones the allowed list accepts and the ones that fail.
.OUTPUTS
	An object with Accepted (each pin with the Reason of its entry) and Rejected.
#>
function Split-PrereleasePins {
	param(
		[Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Pins,
		[Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Allowed
	)

	$accepted = @()
	$rejected = @()

	foreach ($pin in $Pins) {
		$entry = $Allowed | Where-Object { $_.group -eq $pin.Group -and $_.field -eq $pin.Field } | Select-Object -First 1

		if ($null -ne $entry) {
			$accepted += [pscustomobject]@{
				Group   = $pin.Group
				Field   = $pin.Field
				Version = $pin.Version
				Reason  = $entry.reason
			}
		}
		else {
			$rejected += $pin
		}
	}

	return [pscustomobject]@{
		Accepted = $accepted
		Rejected = $rejected
	}
}

<#
.SYNOPSIS
	Returns the allowed entries that matched no prerelease pin.
.DESCRIPTION
	Such an entry is out of date, for instance once the dependency ships a stable version. It is
	reported rather than failed, so a forgotten clean-up never blocks a release.
#>
function Get-UnusedAllowedPrereleases {
	param(
		[Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Allowed,
		[Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Accepted
	)

	$unused = @()
	foreach ($entry in $Allowed) {
		if (-not ($Accepted | Where-Object { $_.Group -eq $entry.group -and $_.Field -eq $entry.field })) {
			$unused += $entry
		}
	}

	# Comma operator: see Get-ManifestPinnedVersions.
	return ,$unused
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

	$allowed = @()
	if (Test-Path $AllowedPrereleasesPath) {
		$allowed = Get-AllowedPrereleases -Json (Get-Content -Raw -Path $AllowedPrereleasesPath)
	}
	$accepted = @()

	$pins = Get-ManifestPinnedVersions -Manifest $manifest
	$split = Split-PrereleasePins -Pins (Get-PrereleasePins -Pins $pins) -Allowed $allowed
	$prerelease = $split.Rejected
	$accepted += $split.Accepted

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
		Write-Host "release needs one. A prerelease the release ships on purpose, because no stable version"
		Write-Host "exists yet, goes in $AllowedPrereleasesPath with its reason."
	}
	elseif ($split.Accepted.Count -gt 0) {
		Write-Host "OK: the $($pins.Count) pins in $ManifestPath are stable, apart from $($split.Accepted.Count) allowed prerelease pin(s)."
	}
	else {
		Write-Host "OK: all $($pins.Count) pins in $ManifestPath are stable."
	}

	foreach ($propertyFile in $PropertyFilePaths) {
		if (-not (Test-Path $propertyFile)) {
			continue
		}

		$propertyPins = Get-MSBuildPinnedVersions -Content (Get-Content -Raw -Path $propertyFile) -SourceName (Split-Path -Leaf $propertyFile)
		$propertySplit = Split-PrereleasePins -Pins (Get-PrereleasePins -Pins $propertyPins) -Allowed $allowed
		$propertyPrerelease = $propertySplit.Rejected
		$accepted += $propertySplit.Accepted

		if ($propertyPrerelease.Count -gt 0) {
			$failed = $true
			Write-Host "::error::$propertyFile pins $($propertyPrerelease.Count) prerelease version(s) on a stable release branch."
			foreach ($pin in $propertyPrerelease) {
				Write-Host "::error file=$propertyFile::$($pin.Field) = $($pin.Version)"
			}
		}
		elseif ($propertySplit.Accepted.Count -gt 0) {
			Write-Host "OK: the $($propertyPins.Count) pins in $propertyFile are stable, apart from $($propertySplit.Accepted.Count) allowed prerelease pin(s)."
		}
		else {
			Write-Host "OK: all $($propertyPins.Count) pins in $propertyFile are stable."
		}
	}

	# Shown on every run, so an exception stays visible until it is removed.
	foreach ($pin in $accepted) {
		Write-Host "::warning::Allowed prerelease pin: $($pin.Group) -> $($pin.Field) = $($pin.Version). $($pin.Reason)"
	}

	foreach ($entry in (Get-UnusedAllowedPrereleases -Allowed $allowed -Accepted $accepted)) {
		Write-Host "::warning file=$AllowedPrereleasesPath::$($entry.group) -> $($entry.field) is allowed, but it no longer pins a prerelease version: remove it."
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
