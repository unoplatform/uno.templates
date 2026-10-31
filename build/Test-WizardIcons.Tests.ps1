# Tests for Test-WizardIcons.ps1, run against small template configs built in Pester's TestDrive,
# so they run fully offline and don't depend on the real icons. Requires Pester v5+.
#
#   Invoke-Pester -Path build/Test-WizardIcons.Tests.ps1

BeforeAll {
	$script:Check = Join-Path $PSScriptRoot 'Test-WizardIcons.ps1'

	# Writes a TemplateWizard.json naming the given icons (one per line, so a line can be commented out),
	# and an Assets file for each given relative path.
	function New-Config {
		param([string[]]$Named, [string[]]$Files, [string[]]$CommentedOut = @())

		$config = Join-Path $TestDrive ([guid]::NewGuid())
		$lines = @(foreach ($name in $Named) { "		`"$name`": { `"Icon`": `"/Assets/$name`" }," })
		$lines += @(foreach ($name in $CommentedOut) { "		// `"$name`": { `"Icon`": `"/Assets/$name`" }," })
		New-Item -ItemType Directory -Path (Join-Path $config 'Assets') -Force | Out-Null
		Set-Content -Path (Join-Path $config 'TemplateWizard.json') -Value (@('{', '	"Icons": {') + $lines + @('	}', '}'))
		foreach ($file in $Files) {
			$path = Join-Path $config 'Assets' $file
			New-Item -ItemType Directory -Path (Split-Path $path) -Force | Out-Null
			Set-Content -Path $path -Value '<svg xmlns="http://www.w3.org/2000/svg"/>'
		}
		return $config
	}

	function Get-AllVariants([string]$Name) {
		"$Name", "Dark/$Name", "Disable/$Name", "DarkDisable/$Name"
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

Describe 'Test-WizardIcons.ps1' {
	It 'passes when every named icon has its four variants and nothing else is there' {
		$config = New-Config -Named 'Features.A.svg', 'Platforms.B.svg' -Files ((Get-AllVariants 'Features.A.svg') + (Get-AllVariants 'Platforms.B.svg'))

		$result = Invoke-Check $config

		$result.Threw | Should -BeFalse
		$result.Errors | Should -BeNullOrEmpty
	}

	It 'reports a named icon that misses a variant' {
		$config = New-Config -Named 'Features.A.svg' -Files 'Features.A.svg', 'Dark/Features.A.svg', 'Disable/Features.A.svg'

		$result = Invoke-Check $config

		$result.Threw | Should -BeTrue
		$result.Errors | Should -Be @('Assets/DarkDisable/Features.A.svg is missing: TemplateWizard.json names Features.A.svg')
	}

	It 'reports a file that nothing names' {
		$config = New-Config -Named 'Features.A.svg' -Files ((Get-AllVariants 'Features.A.svg') + 'Dark/Features.Old.svg')

		$result = Invoke-Check $config

		$result.Threw | Should -BeTrue
		$result.Errors | Should -Be @('Assets/Dark/Features.Old.svg is not named by TemplateWizard.json')
	}

	It 'reports a file in a folder that is not a theme variant' {
		$config = New-Config -Named 'Features.A.svg' -Files ((Get-AllVariants 'Features.A.svg') + 'Light/Features.A.svg')

		$result = Invoke-Check $config

		$result.Threw | Should -BeTrue
		$result.Errors | Should -Be @('Assets/Light/Features.A.svg is not one of the theme variants the wizards draw')
	}

	It 'reports a disabled variant of a preset card, which the wizards never draw' {
		$config = New-Config -Named 'Template_Blank.svg' -Files 'Template_Blank.svg', 'Dark/Template_Blank.svg', 'Disable/Template_Blank.svg'

		$result = Invoke-Check $config

		$result.Threw | Should -BeTrue
		$result.Errors | Should -Be @('Assets/Disable/Template_Blank.svg is not one of the theme variants the wizards draw')
	}

	It 'works with a relative template config path' {
		$config = New-Config -Named 'Features.A.svg' -Files (Get-AllVariants 'Features.A.svg')

		# Push-Location moves PowerShell, not the process directory that .NET paths resolve against.
		Push-Location (Split-Path $config)
		try { $result = Invoke-Check (Split-Path $config -Leaf) } finally { Pop-Location }

		$result.Threw | Should -BeFalse
		$result.Errors | Should -BeNullOrEmpty
	}

	It 'compares names case-sensitively' {
		$config = New-Config -Named 'Features.A.svg' -Files 'Features.A.svg', 'Dark/features.a.svg', 'Disable/Features.A.svg', 'DarkDisable/Features.A.svg'

		$result = Invoke-Check $config

		$result.Threw | Should -BeTrue
		$result.Errors | Should -Contain 'Assets/Dark/features.a.svg is not named by TemplateWizard.json'
	}

	It 'does not count a commented-out entry as naming its icon' {
		$config = New-Config -Named 'Features.A.svg' -CommentedOut 'Features.Gone.svg' -Files ((Get-AllVariants 'Features.A.svg') + 'Features.Gone.svg')

		$result = Invoke-Check $config

		$result.Threw | Should -BeTrue
		$result.Errors | Should -Be @('Assets/Features.Gone.svg is not named by TemplateWizard.json')
	}

	It 'keeps the icons of a commented-out option that is on the kept list' {
		$config = New-Config -Named 'Features.A.svg' -CommentedOut 'Features.WasmMultiThreading.svg' -Files ((Get-AllVariants 'Features.A.svg') + (Get-AllVariants 'Features.WasmMultiThreading.svg'))

		$result = Invoke-Check $config

		$result.Threw | Should -BeFalse
		$result.Errors | Should -BeNullOrEmpty
	}

	It 'needs only the light and dark variants of a preset card' {
		$config = New-Config -Named 'Template_Blank.svg' -Files 'Template_Blank.svg', 'Dark/Template_Blank.svg'

		$result = Invoke-Check $config

		$result.Threw | Should -BeFalse
		$result.Errors | Should -BeNullOrEmpty
	}

	It 'still reports a preset card without its dark variant' {
		$config = New-Config -Named 'Template_Blank.svg' -Files 'Template_Blank.svg'

		$result = Invoke-Check $config

		$result.Threw | Should -BeTrue
		$result.Errors | Should -Be @('Assets/Dark/Template_Blank.svg is missing: TemplateWizard.json names Template_Blank.svg')
	}

	It 'reports every variant an option misses' {
		$config = New-Config -Named 'Framework.NET12.svg' -Files 'Framework.NET12.svg'

		$result = Invoke-Check $config

		$result.Threw | Should -BeTrue
		$result.Errors.Count | Should -Be 3
		$result.Errors | Should -Contain 'Assets/Dark/Framework.NET12.svg is missing: TemplateWizard.json names Framework.NET12.svg'
		$result.Errors | Should -Contain 'Assets/Disable/Framework.NET12.svg is missing: TemplateWizard.json names Framework.NET12.svg'
		$result.Errors | Should -Contain 'Assets/DarkDisable/Framework.NET12.svg is missing: TemplateWizard.json names Framework.NET12.svg'
	}
}
