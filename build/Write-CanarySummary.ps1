param(
	# packages.json as pinned on the branch, saved before the canary moved it.
	[Parameter(Mandatory)][string]$PinnedManifest,
	# packages.json after the canary moved the Uno packages to their newest dev builds.
	[string]$Manifest = "src/Uno.Sdk/packages.json",
	# Where the pinned Uno.UITest.Helpers version lives (UnoUITestHelpersVersion).
	[string]$BuildTargets = "src/Directory.Build.targets"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Writes the canary's run summary: every version the canary moved away from the branch's pins,
# so a red canary can be traced to the Uno build it tried. Markdown goes to stdout.

function Get-Versions {
	param([Parameter(Mandatory)][string]$Path)

	# One entry per group, plus one per target-framework override ("WasmBootstrap (net10.0)").
	$versions = [ordered]@{}
	foreach ($group in (Get-Content $Path -Raw | ConvertFrom-Json)) {
		$versions[$group.group] = $group.version
		if ($group.PSObject.Properties.Name -contains "versionOverride" -and $group.versionOverride) {
			foreach ($override in $group.versionOverride.PSObject.Properties) {
				$versions["$($group.group) ($($override.Name))"] = $override.Value
			}
		}
	}
	return $versions
}

$pinned = Get-Versions $PinnedManifest
$current = Get-Versions $Manifest

$rows = @(foreach ($name in $current.Keys) {
	$before = if ($pinned.Contains($name)) { $pinned[$name] } else { "(new)" }
	if ($before -ne $current[$name]) { "| $name | $before | $($current[$name]) |" }
})

# The one Uno package outside packages.json, moved through the UnoUITestHelpersVersion property.
if ($env:UnoUITestHelpersVersion) {
	$match = Select-String -Path $BuildTargets -Pattern '<UnoUITestHelpersVersion[^>]*>([^<]+)<' | Select-Object -First 1
	$before = if ($match) { $match.Matches[0].Groups[1].Value } else { "(unknown)" }
	if ($before -ne $env:UnoUITestHelpersVersion) { $rows += "| Uno.UITest.Helpers | $before | $env:UnoUITestHelpersVersion |" }
}

"## Canary: Uno versions under test"
""
if ($rows.Count -eq 0) {
	"The branch is already on the newest Uno dev builds: this run tests its pinned versions."
}
else {
	"| Package group | Pinned | Under test |"
	"|---|---|---|"
	$rows
}
