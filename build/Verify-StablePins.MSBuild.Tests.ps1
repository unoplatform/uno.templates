# Unit tests for the MSBuild property-file reader in Verify-StablePins.ps1.
#
# The updater writes the same pins into .props/.targets as MSBuild properties, so a dev version
# can ship through those files even when packages.json is clean. Kept in a separate file from the
# manifest tests so each stays readable.
#
#   Invoke-Pester -Path build/Verify-StablePins.MSBuild.Tests.ps1

BeforeAll {
	. (Join-Path $PSScriptRoot 'Verify-StablePins.ps1')
}

Describe 'Get-MSBuildPinnedVersions' {
	BeforeAll {
		$script:Targets = @'
<Project>
  <PropertyGroup>
    <UnoVersion>6.7.135</UnoVersion>
    <UnoWasmBootstrapVersionNet9>9.0.23</UnoWasmBootstrapVersionNet9>
    <UnoWasmBootstrapVersionNet10>10.1.0-dev.214</UnoWasmBootstrapVersionNet10>
    <UnoDspTasksVersion>1.4.0</UnoDspTasksVersion>
    <NotAPin>something</NotAPin>
  </PropertyGroup>
</Project>
'@
	}

	It 'reads every element whose name contains Version' {
		$pins = Get-MSBuildPinnedVersions -Content $Targets -SourceName 'Uno.Sdk.Updater.targets'
		$pins.Count | Should -Be 4
	}

	It 'ignores elements that are not version pins' {
		$pins = Get-MSBuildPinnedVersions -Content $Targets -SourceName 'x'
		($pins | Where-Object { $_.Field -eq 'NotAPin' }) | Should -BeNullOrEmpty
	}

	It 'captures a suffixed property name, not just ones ending in Version' {
		$pins = Get-MSBuildPinnedVersions -Content $Targets -SourceName 'x'
		$net10 = $pins | Where-Object { $_.Field -eq 'UnoWasmBootstrapVersionNet10' }
		$net10.Version | Should -Be '10.1.0-dev.214'
	}

	It 'records the source so the error message names the file' {
		$pins = Get-MSBuildPinnedVersions -Content $Targets -SourceName 'Uno.Sdk.Updater.targets'
		$pins[0].Group | Should -Be 'Uno.Sdk.Updater.targets'
	}

	It 'returns an empty array for a file with no pins' {
		$pins = Get-MSBuildPinnedVersions -Content '<Project></Project>' -SourceName 'x'
		$pins.Count | Should -Be 0
	}

	It 'trims surrounding whitespace from the value' {
		$pins = Get-MSBuildPinnedVersions -Content "<Project><SdkVersion>  6.7.135  </SdkVersion></Project>" -SourceName 'x'
		$pins[0].Version | Should -Be '6.7.135'
	}
}

Describe 'Get-PrereleasePins over MSBuild properties' {
	It 'finds the dev pin and leaves the stable ones alone' {
		$content = @'
<Project>
  <PropertyGroup>
    <UnoVersion>6.7.135</UnoVersion>
    <UnoWasmBootstrapVersionNet10>10.1.0-dev.214</UnoWasmBootstrapVersionNet10>
  </PropertyGroup>
</Project>
'@
		$found = Get-PrereleasePins -Pins (Get-MSBuildPinnedVersions -Content $content -SourceName 'targets')

		$found.Count | Should -Be 1
		$found[0].Field | Should -Be 'UnoWasmBootstrapVersionNet10'
	}

	It 'passes an all-stable property file' {
		$content = '<Project><PropertyGroup><UnoVersion>6.7.135</UnoVersion><SdkVersion>6.7.135</SdkVersion></PropertyGroup></Project>'
		(Get-PrereleasePins -Pins (Get-MSBuildPinnedVersions -Content $content -SourceName 'x')).Count | Should -Be 0
	}
}
