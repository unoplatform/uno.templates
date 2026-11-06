param(
	# The unoapp template's config folder: TemplateWizard.json and the template.json it describes.
	[string]$TemplateConfig = (Join-Path $PSScriptRoot ".." "src" "Uno.Templates" "content" "unoapp" ".template.config")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Checks that TemplateWizard.json only names options and choices that template.json defines. The wizards
# (Visual Studio, the Live Wizard and Rider) read both files from the templates package, and skip what
# template.json doesn't define without any error, so a renamed or removed option would just disappear from
# the wizard. Commented-out entries aren't checked: ConvertFrom-Json drops the comments.
# Also checks that each preset meets the requirements of the options and choices it turns on: a wizard
# disables those, so the preset would show an option it can't keep.

$wizard = Get-Content (Join-Path $TemplateConfig "TemplateWizard.json") -Raw | ConvertFrom-Json -AsHashtable
$symbols = (Get-Content (Join-Path $TemplateConfig "template.json") -Raw | ConvertFrom-Json -AsHashtable).symbols

# The options a wizard can set: the parameters, not the symbols template.json computes from them. Names are
# compared case-sensitively, like the wizards do.
$parameters = @($symbols.Keys | Where-Object { $symbols[$_].Contains("type") -and $symbols[$_].type -eq "parameter" })
$names = [System.Collections.Generic.HashSet[string]]::new([string[]]$parameters, [StringComparer]::Ordinal)
$problems = [System.Collections.Generic.List[string]]::new()

# The values an option takes: its choices, true or false for a bool, or $null when it takes any text.
function Get-Values([string]$Name) {
	$symbol = $symbols[$Name]
	if ($symbol.Contains("choices")) { return , @($symbol.choices | ForEach-Object { $_.choice }) }
	$type = @($symbol.Keys | Where-Object { $_ -eq "datatype" } | ForEach-Object { $symbol[$_] })
	if ($type -contains "bool") { return , @("true", "false") }
	return $null
}

function Test-Option([string]$Name, [string]$Where) {
	if ($names.Contains($Name)) { return $true }
	$problems.Add("$Where names the option '$Name', which template.json doesn't define as a parameter")
	return $false
}

function Test-Value([string]$Name, [string]$Value, [string]$Where) {
	$values = Get-Values $Name
	if ($null -ne $values -and $values -cnotcontains $Value) {
		$problems.Add("$Where gives '$Name' the value '$Value', which isn't one of its values in template.json")
	}
}

function Test-Requires([System.Collections.IDictionary]$Entry, [string]$Where) {
	if (-not $Entry.Contains("Requires")) { return }
	foreach ($name in $Entry.Requires.Keys) {
		if (-not (Test-Option $name "$Where (Requires)")) { continue }
		foreach ($required in $Entry.Requires[$name].RequiredValues) {
			# A required value is a string, or a { value, display } pair in the navigation tags. A leading !
			# means "any value but this one".
			$value = "$(if ($required -is [System.Collections.IDictionary]) { $required.value } else { $required })"
			Test-Value $name $value.TrimStart("!") "$Where (Requires)"
		}
	}
}

# The preset cards. The custom card isn't a preset: it lets the user pick every option.
$preset = $wizard.TemplateTypes.SymbolId
if (Test-Option $preset "TemplateTypes") {
	foreach ($card in $wizard.TemplateTypes.Choices) {
		$isCustom = $card.Contains("IsCustom") -and $card.IsCustom
		if (-not $isCustom) { Test-Value $preset $card.Id "TemplateTypes" }
		if (-not $card.Contains("Parameters")) { continue }
		foreach ($name in $card.Parameters.Keys) {
			if (-not (Test-Option $name "The $($card.Id) preset")) { continue }
			# An option that takes several values lists them separated by |.
			foreach ($value in "$($card.Parameters[$name])".Split("|")) { Test-Value $name $value "The $($card.Id) preset" }
		}
	}
}

# The wizard pages. A section with a SymbolId lists choices of that option, and Choices limits the ones it offers;
# a section without one lists options.
foreach ($page in $wizard.Sections.Keys) {
	foreach ($section in $wizard.Sections[$page]) {
		$where = "The $page page"
		$parent = if ($section.Contains("SymbolId")) { $section.SymbolId } else { $null }
		if ($parent -and -not (Test-Option $parent $where)) { continue }
		$ids = if ($section.Contains("SymbolIds")) { @($section.SymbolIds) } else { @() }
		foreach ($id in $ids) {
			if ($parent) { Test-Value $parent $id $where } else { [void](Test-Option $id $where) }
		}
		$choices = if ($section.Contains("Choices")) { @($section.Choices) } else { @() }
		foreach ($choice in $choices) {
			if ($parent) { Test-Value $parent $choice "$where (Choices)" }
			else { $problems.Add("$where lists the choice '$choice' without the option it belongs to (SymbolId)") }
		}
	}
}

# Icons, texts and requirements, keyed by "option" or "option.choice". NoValue is the value the wizard gives an
# option when nothing is selected, and Tag holds more requirements.
foreach ($key in $wizard.MetaData.Keys) {
	$name, $choice = $key.Split(".", 2)
	$entry = $wizard.MetaData[$key]
	$isOption = Test-Option $name "MetaData"
	if ($isOption -and $choice) { Test-Value $name $choice "MetaData" }
	if ($isOption -and $entry.Contains("NoValue")) { Test-Value $name "$($entry.NoValue)" "MetaData.$key (NoValue)" }
	Test-Requires $entry "MetaData.$key"
	if ($entry.Contains("Tag") -and $entry.Tag -is [System.Collections.IDictionary]) { Test-Requires $entry.Tag "MetaData.$key (Tag)" }
}

foreach ($element in $wizard.NavigationElements) {
	if ($element.Contains("Tag")) { Test-Requires $element.Tag "The $($element.NavigationId) tab" }
}

# The values a preset selects for an option: its own, or the template's default when it doesn't set the option.
function Get-PresetValues([System.Collections.IDictionary]$Card, [string]$Name) {
	if ($Card.Parameters.Contains($Name)) { return , @("$($Card.Parameters[$Name])".Split("|")) }
	if ($symbols.Contains($Name) -and $symbols[$Name].Contains("defaultValue")) { return , @("$($symbols[$Name].defaultValue)".Split("|")) }
	return $null
}

# The wizards' rule, as in the Visual Studio wizard: an option or a choice is disabled while one of its excluded
# (!) values is selected, or while none of its required values is. Tag requirements only label a choice.
foreach ($card in @($wizard.TemplateTypes.Choices | Where-Object { $_.Contains("Parameters") })) {
	foreach ($key in $wizard.MetaData.Keys) {
		$entry = $wizard.MetaData[$key]
		if (-not $entry.Contains("Requires")) { continue }
		$name, $choice = $key.Split(".", 2)
		$selected = Get-PresetValues $card $name
		if ($null -eq $selected -or $selected -cnotcontains $(if ($choice) { $choice } else { "true" })) { continue }
		foreach ($required in $entry.Requires.Keys) {
			$values = Get-PresetValues $card $required
			if ($null -eq $values) { continue }
			$listed = @($entry.Requires[$required].RequiredValues | ForEach-Object { "$(if ($_ -is [System.Collections.IDictionary]) { $_.value } else { $_ })" })
			$excluded = @($listed | Where-Object { $_.StartsWith("!") } | ForEach-Object { $_.Substring(1) })
			$wanted = @($listed | Where-Object { -not $_.StartsWith("!") })
			$clash = @($values | Where-Object { $excluded -ccontains $_ })
			$missing = $wanted.Count -gt 0 -and @($values | Where-Object { $wanted -ccontains $_ }).Count -eq 0
			if ($clash.Count -gt 0 -or $missing) {
				$problems.Add("The $($card.Id) preset turns on '$key', which requires '$required' to be $($listed -join ' or '), but the preset gives it '$($values -join '|')'")
			}
		}
	}
}

# isCustomized isn't a template option: Studio sets it itself when the user changes a preset's options.
foreach ($name in $wizard.ExportableSymbols | Where-Object { $_ -cne "isCustomized" }) { [void](Test-Option $name "ExportableSymbols") }
foreach ($name in $wizard.ExcludeFromCliCommand) { [void](Test-Option $name "ExcludeFromCliCommand") }

foreach ($package in $wizard.PackagesToUpdate) {
	$where = "PackagesToUpdate ($($package.PackageId))"
	[void](Test-Option $package.Symbol $where)
	if (-not $package.Contains("UsePrerelease")) { continue }
	foreach ($name in $package.UsePrerelease.Keys) {
		if (Test-Option $name $where) {
			foreach ($value in $package.UsePrerelease[$name]) { Test-Value $name $value $where }
		}
	}
}

if ($problems.Count -gt 0) {
	$problems | ForEach-Object { Write-Host "::error::$_" }
	throw "TemplateWizard.json doesn't match template.json or its own requirements ($($problems.Count) problems)."
}

Write-Host "TemplateWizard.json only names options and values that template.json defines, and its presets meet their requirements."
