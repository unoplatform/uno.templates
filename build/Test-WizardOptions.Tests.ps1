# Tests for Test-WizardOptions.ps1, run against small template configs built in Pester's TestDrive,
# so they run fully offline and don't depend on the real templates. Requires Pester v5+.
#
#   Invoke-Pester -Path build/Test-WizardOptions.Tests.ps1

BeforeAll {
	$script:Check = Join-Path $PSScriptRoot 'Test-WizardOptions.ps1'

	$script:Template = @{
		symbols = [ordered]@{
			preset     = @{ type = 'parameter'; datatype = 'choice'; choices = @(@{ choice = 'blank' }, @{ choice = 'recommended' }) }
			platforms  = @{ type = 'parameter'; datatype = 'choice'; allowMultipleValues = $true; choices = @(@{ choice = 'android' }, @{ choice = 'ios' }, @{ choice = 'desktop' }) }
			toolkit    = @{ type = 'parameter'; datatype = 'bool' }
			appId      = @{ type = 'parameter'; datatype = 'string' }
			useToolkit = @{ type = 'computed'; datatype = 'bool'; value = 'toolkit' }
		}
	}

	# Wizard metadata that matches the template above, in every place the check looks at.
	function New-Wizard {
		[ordered]@{
			NavigationElements    = @(@{ NavigationId = 'Platforms'; Tag = @{ Requires = @{ platforms = @{ RequiredValues = @(@{ value = 'android'; display = 'Android' }) } } } })
			TemplateTypes         = @{
				SymbolId = 'preset'
				Choices  = @(
					@{ Id = 'blank'; Parameters = [ordered]@{ platforms = 'android|desktop'; toolkit = 'false'; appId = 'com.example' } },
					@{ Id = 'custom'; IsCustom = $true })
			}
			Sections              = [ordered]@{
				Platforms = @(
					@{ SymbolId = 'platforms'; SymbolIds = @('android', 'ios') },
					@{ SymbolId = 'platforms'; Choices = @('desktop') })
				Features  = @(@{ SymbolIds = @('toolkit') })
			}
			MetaData              = [ordered]@{
				'platforms.android' = @{ Icon = '/Assets/Platforms.Android.svg' }
				'platforms.ios'     = @{ Tag = @{ Requires = @{ toolkit = @{ RequiredValues = @('true') } } } }
				toolkit             = @{ NoValue = 'false'; Requires = @{ platforms = @{ RequiredValues = @('!desktop') } } }
			}
			ExportableSymbols     = @('platforms', 'toolkit', 'isCustomized')
			PackagesToUpdate      = @(@{ PackageId = 'Uno.Sdk'; Symbol = 'appId'; UsePrerelease = @{ platforms = @('android') } })
			ExcludeFromCliCommand = @('appId')
		}
	}

	# Writes the template and the given wizard metadata (a hashtable, or JSON text) to a new config folder.
	function New-Config($Wizard) {
		$config = Join-Path $TestDrive ([guid]::NewGuid())
		New-Item -ItemType Directory -Path $config | Out-Null
		$script:Template | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $config 'template.json')
		$text = if ($Wizard -is [string]) { $Wizard } else { $Wizard | ConvertTo-Json -Depth 10 }
		Set-Content -Path (Join-Path $config 'TemplateWizard.json') -Value $text
		return $config
	}

	# The check's error lines (Write-Host goes to the information stream) and whether it threw. The output is
	# collected from the whole try statement, so the lines written before the throw are kept.
	function Invoke-Check([string]$Config) {
		$threw = $false
		$output = try { & $script:Check -TemplateConfig $Config 6>&1 } catch { $threw = $true }
		$errors = @($output | Where-Object { "$_" -like '::error::*' } | ForEach-Object { "$_".Substring(9) })
		return [pscustomobject]@{ Threw = $threw; Errors = $errors }
	}
}

Describe 'Test-WizardOptions.ps1' {
	It 'passes when the wizard only names options and values the template defines' {
		$result = Invoke-Check (New-Config (New-Wizard))

		$result.Threw | Should -BeFalse
		$result.Errors | Should -BeNullOrEmpty
	}

	It 'reports an option a page lists that the template does not define' {
		$wizard = New-Wizard
		$wizard.Sections.Features[0].SymbolIds += 'serialization'

		$result = Invoke-Check (New-Config $wizard)

		$result.Threw | Should -BeTrue
		$result.Errors | Should -Be @("The Features page names the option 'serialization', which template.json doesn't define as a parameter")
	}

	It 'reports a symbol that template.json only computes, as no wizard can set it' {
		$wizard = New-Wizard
		$wizard.Sections.Features[0].SymbolIds = @('useToolkit')

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("The Features page names the option 'useToolkit', which template.json doesn't define as a parameter")
	}

	It 'reports a choice a page lists that its option does not have' {
		$wizard = New-Wizard
		$wizard.Sections.Platforms[0].SymbolIds += 'tvos'

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("The Platforms page gives 'platforms' the value 'tvos', which isn't one of its values in template.json")
	}

	It 'reports a choice that a section limits its option to, when the option does not have it' {
		$wizard = New-Wizard
		$wizard.Sections.Platforms[1].Choices += 'DPS'

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("The Platforms page (Choices) gives 'platforms' the value 'DPS', which isn't one of its values in template.json")
	}

	It 'reports choices listed without the option they belong to' {
		$wizard = New-Wizard
		$wizard.Sections.Features += @{ Choices = @('simple') }

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("The Features page lists the choice 'simple' without the option it belongs to (SymbolId)")
	}

	It 'reports preset values that the template does not accept' {
		$wizard = New-Wizard
		$wizard.TemplateTypes.Choices[0].Parameters.platforms = 'android|tvos'
		$wizard.TemplateTypes.Choices[0].Parameters.toolkit = 'yes'

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors.Count | Should -Be 2
		$result.Errors | Should -Contain "The blank preset gives 'platforms' the value 'tvos', which isn't one of its values in template.json"
		$result.Errors | Should -Contain "The blank preset gives 'toolkit' the value 'yes', which isn't one of its values in template.json"
	}

	It 'reports a preset card that is not a preset, unless it is the custom card' {
		$wizard = New-Wizard
		$wizard.TemplateTypes.Choices += @{ Id = 'minimal' }

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("TemplateTypes gives 'preset' the value 'minimal', which isn't one of its values in template.json")
	}

	It 'reports metadata for an option or a choice that does not exist' {
		$wizard = New-Wizard
		$wizard.MetaData['tfm.net8.0'] = @{ Icon = '/Assets/Framework.NET8.svg' }
		$wizard.MetaData['platforms.tvos'] = @{ Icon = '/Assets/Platforms.TvOS.svg' }

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors.Count | Should -Be 2
		$result.Errors | Should -Contain "MetaData names the option 'tfm', which template.json doesn't define as a parameter"
		$result.Errors | Should -Contain "MetaData gives 'platforms' the value 'tvos', which isn't one of its values in template.json"
	}

	It 'reports a value for an empty selection that the option does not take' {
		$wizard = New-Wizard
		$wizard.MetaData.toolkit.NoValue = 'maybe'

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("MetaData.toolkit (NoValue) gives 'toolkit' the value 'maybe', which isn't one of its values in template.json")
	}

	It 'reports a requirement on a value that does not exist, also when it is negated' {
		$wizard = New-Wizard
		$wizard.MetaData.toolkit.Requires.platforms.RequiredValues = @('!tvos')
		$wizard.NavigationElements[0].Tag.Requires.platforms.RequiredValues = @(@{ value = 'native'; display = 'Native' })

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors.Count | Should -Be 2
		$result.Errors | Should -Contain "MetaData.toolkit (Requires) gives 'platforms' the value 'tvos', which isn't one of its values in template.json"
		$result.Errors | Should -Contain "The Platforms tab (Requires) gives 'platforms' the value 'native', which isn't one of its values in template.json"
	}

	It 'reports a requirement in a metadata tag on a value that does not exist' {
		$wizard = New-Wizard
		$wizard.MetaData['platforms.ios'].Tag.Requires.toolkit.RequiredValues = @('maybe')

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("MetaData.platforms.ios (Tag) (Requires) gives 'toolkit' the value 'maybe', which isn't one of its values in template.json")
	}

	It 'reports a package whose version option does not exist' {
		$wizard = New-Wizard
		$wizard.PackagesToUpdate += @{ PackageId = 'Uno.WinUI'; Symbol = 'unoWinUIVersion' }

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("PackagesToUpdate (Uno.WinUI) names the option 'unoWinUIVersion', which template.json doesn't define as a parameter")
	}

	It 'reports unknown names in the exported and excluded options' {
		$wizard = New-Wizard
		$wizard.ExportableSymbols += 'unoLottie'
		$wizard.ExcludeFromCliCommand += 'serialization'

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors.Count | Should -Be 2
		$result.Errors | Should -Contain "ExportableSymbols names the option 'unoLottie', which template.json doesn't define as a parameter"
		$result.Errors | Should -Contain "ExcludeFromCliCommand names the option 'serialization', which template.json doesn't define as a parameter"
	}

	It 'compares option names case-sensitively' {
		$wizard = New-Wizard
		$wizard.Sections.Features[0].SymbolIds = @('Toolkit')

		$result = Invoke-Check (New-Config $wizard)

		$result.Errors | Should -Be @("The Features page names the option 'Toolkit', which template.json doesn't define as a parameter")
	}

	It 'does not check a commented-out entry' {
		$json = (New-Wizard | ConvertTo-Json -Depth 10).Replace('"isCustomized"', "`"isCustomized`"`n    // `"wasmMultiThreading`"")

		$result = Invoke-Check (New-Config $json)

		$json | Should -Match '// "wasmMultiThreading"'
		$result.Threw | Should -BeFalse
	}
}
