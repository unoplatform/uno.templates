param(
	# The unoapp template's config folder: TemplateWizard.json and the Assets it names.
	[string]$TemplateConfig = (Join-Path $PSScriptRoot ".." "src" "Uno.Templates" "content" "unoapp" ".template.config")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Checks that the wizard icons match TemplateWizard.json. The metadata names each option's icon as
# /Assets/<name>.svg, and the wizards (Visual Studio and the Live Wizard) draw it from Assets/ in the
# light theme and from Assets/Dark/, Assets/Disable/ and Assets/DarkDisable/ otherwise. Fails when a named
# icon is missing a variant, or when a file under Assets/ is named by nothing, so adding or dropping an
# option or a .NET version keeps the icons in step.

# A full path: [IO.Path]::GetRelativePath below would resolve a relative one against the process directory.
$assets = Convert-Path -LiteralPath (Join-Path $TemplateConfig "Assets")

# The theme variants of an icon. The preset cards (Template_*) are never drawn disabled, so they have no Disable ones.
function Get-Variants([string]$Name) {
	if ($Name -clike "Template_*") { return "", "Dark" }
	return "", "Dark", "Disable", "DarkDisable"
}

# Icons kept for an option that TemplateWizard.json doesn't name yet, or only names in a comment:
# - Features.WasmMultiThreading.svg, until https://github.com/unoplatform/uno-private/issues/1339 is done.
# - Platforms.tvOS.svg, until https://github.com/unoplatform/uno.templates/pull/2316 adds the tvOS platform.
$kept = @("Features.WasmMultiThreading.svg", "Platforms.tvOS.svg")

# Whole-line // comments are commented-out entries: they don't name an icon.
$metadata = Get-Content (Join-Path $TemplateConfig "TemplateWizard.json") | Where-Object { $_ -notmatch '^\s*//' }
$named = @($metadata |
	Select-String -Pattern '"/Assets/([^"/]+\.svg)"' -AllMatches |
	ForEach-Object { $_.Matches } |
	ForEach-Object { $_.Groups[1].Value } |
	Sort-Object -Unique -CaseSensitive)

$problems = @(foreach ($name in $named) {
	foreach ($variant in Get-Variants $name) {
		$relative = if ($variant) { "$variant/$name" } else { $name }
		if (-not (Test-Path -LiteralPath (Join-Path $assets $relative))) {
			"Assets/$relative is missing: TemplateWizard.json names $name"
		}
	}
})

# Names are compared case-sensitively: the Live Wizard loads them over HTTP.
$problems += @(foreach ($file in Get-ChildItem -LiteralPath $assets -Recurse -File) {
	$relative = [IO.Path]::GetRelativePath($assets, $file.FullName).Replace("\", "/")
	$folder = if ($relative.Contains("/")) { $relative.Substring(0, $relative.LastIndexOf("/")) } else { "" }
	if ($file.Name -cnotin $named -and $file.Name -cnotin $kept) {
		"Assets/$relative is not named by TemplateWizard.json"
	}
	elseif ($folder -cnotin @(Get-Variants $file.Name)) {
		"Assets/$relative is not one of the theme variants the wizards draw"
	}
})

if ($problems.Count -gt 0) {
	$problems | ForEach-Object { Write-Host "::error::$_" }
	throw "The wizard icons don't match TemplateWizard.json ($($problems.Count) problems)."
}

Write-Host "The wizard icons match TemplateWizard.json: $($named.Count) icons in their theme variants."
