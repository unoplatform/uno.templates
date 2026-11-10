# Unit tests for the allowed prerelease pins in Verify-StablePins.ps1.
#
# A stable release can ship a prerelease on purpose, such as a dependency with no stable version
# yet. Those pins are listed with their reason; every other prerelease pin must still fail.
#
#   Invoke-Pester -Path build/Verify-StablePins.AllowedPrereleases.Tests.ps1

BeforeAll {
	$script:ScriptPath = Join-Path $PSScriptRoot 'Verify-StablePins.ps1'
	. $ScriptPath

	$script:Allowed = Get-AllowedPrereleases -Json @'
[
  { "group": "Maui", "field": "versionOverride[net11.0]", "reason": "No stable .NET 11 MAUI yet." },
  { "group": "UnoMaui", "field": "version", "reason": "Uno.Maui ships as a preview." },
  { "group": "Uno.Sdk.Updater.targets", "field": "UnoMauiVersion", "reason": "Same preview as UnoMaui." }
]
'@
}

Describe 'Get-AllowedPrereleases' {
	It 'reads every entry' {
		$Allowed.Count | Should -Be 3
		$Allowed[0].group | Should -Be 'Maui'
	}

	It 'returns an empty list for an empty array or a blank file' {
		(Get-AllowedPrereleases -Json '[]').Count | Should -Be 0
		(Get-AllowedPrereleases -Json '').Count | Should -Be 0
	}

	It 'rejects an entry without a reason' {
		{ Get-AllowedPrereleases -Json '[ { "group": "Maui", "field": "version" } ]' } | Should -Throw "*'reason'*"
	}
}

Describe 'Split-PrereleasePins' {
	It 'accepts an allowed pin with its reason' {
		$pins = @([pscustomobject]@{ Group = 'Maui'; Field = 'versionOverride[net11.0]'; Version = '11.0.0-rc.2.26505.5' })

		$split = Split-PrereleasePins -Pins $pins -Allowed $Allowed

		$split.Accepted.Count | Should -Be 1
		$split.Accepted[0].Reason | Should -Be 'No stable .NET 11 MAUI yet.'
		$split.Rejected.Count | Should -Be 0
	}

	It 'matches by group and field, whatever the version' {
		$pins = @([pscustomobject]@{ Group = 'Maui'; Field = 'versionOverride[net11.0]'; Version = '11.0.0-rc.3.1' })
		(Split-PrereleasePins -Pins $pins -Allowed $Allowed).Accepted.Count | Should -Be 1
	}

	It 'still rejects another field of an allowed group' {
		# Only the net11.0 override is allowed: a dev pin in the group default must fail.
		$pins = @([pscustomobject]@{ Group = 'Maui'; Field = 'version'; Version = '10.0.0-dev.1' })

		$split = Split-PrereleasePins -Pins $pins -Allowed $Allowed

		$split.Accepted.Count | Should -Be 0
		$split.Rejected.Count | Should -Be 1
	}

	It 'still rejects a group that is not listed' {
		$pins = @([pscustomobject]@{ Group = 'Core'; Field = 'version'; Version = '7.1.0-dev.115' })
		(Split-PrereleasePins -Pins $pins -Allowed $Allowed).Rejected.Count | Should -Be 1
	}

	It 'accepts an MSBuild property pin by file name and property' {
		$content = '<Project><PropertyGroup><UnoMauiVersion>0.1.0-dev.2</UnoMauiVersion><SdkVersion>7.1.0-dev.1</SdkVersion></PropertyGroup></Project>'
		$pins = Get-PrereleasePins -Pins (Get-MSBuildPinnedVersions -Content $content -SourceName 'Uno.Sdk.Updater.targets')

		$split = Split-PrereleasePins -Pins $pins -Allowed $Allowed

		$split.Accepted.Field | Should -Be 'UnoMauiVersion'
		$split.Rejected.Field | Should -Be 'SdkVersion'
	}

	It 'rejects every pin when nothing is allowed' {
		$pins = @([pscustomobject]@{ Group = 'Maui'; Field = 'versionOverride[net11.0]'; Version = '11.0.0-rc.2.26505.5' })
		(Split-PrereleasePins -Pins $pins -Allowed @()).Rejected.Count | Should -Be 1
	}
}

Describe 'Get-UnusedAllowedPrereleases' {
	It 'reports the entries that no longer match a prerelease pin' {
		$accepted = @([pscustomobject]@{ Group = 'Maui'; Field = 'versionOverride[net11.0]'; Version = '11.0.0-rc.2.26505.5'; Reason = 'x' })

		$unused = Get-UnusedAllowedPrereleases -Allowed $Allowed -Accepted $accepted

		$unused.Count | Should -Be 2
		$unused.group | Should -Contain 'UnoMaui'
	}

	It 'reports nothing when every entry is used' {
		$accepted = $Allowed | ForEach-Object { [pscustomobject]@{ Group = $_.group; Field = $_.field; Version = '1.0.0-x'; Reason = 'x' } }
		(Get-UnusedAllowedPrereleases -Allowed $Allowed -Accepted @($accepted)).Count | Should -Be 0
	}
}

Describe 'Verify-StablePins.ps1 with allowed prereleases' {
	BeforeAll {
		$script:Dir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
		New-Item -ItemType Directory -Path $Dir | Out-Null
		$script:Manifest = Join-Path $Dir 'packages.json'
		Set-Content -Path $Manifest -Value @'
[
  { "group": "Core", "version": "7.0.32" },
  { "group": "Maui", "version": "10.0.120", "versionOverride": { "net11.0": "11.0.0-rc.2.26505.5" } }
]
'@
		$script:AllowedPath = Join-Path $Dir 'allowed.json'
		Set-Content -Path $AllowedPath -Value '[ { "group": "Maui", "field": "versionOverride[net11.0]", "reason": "No stable .NET 11 MAUI yet." } ]'

		function script:Invoke-Verify([string]$AllowedFile) {
			$output = & pwsh -NoProfile -File $ScriptPath -Force -ManifestPath $Manifest -ReadMePath (Join-Path $Dir 'none.md') -PropertyFilePaths (Join-Path $Dir 'none.props') -AllowedPrereleasesPath $AllowedFile 2>&1
			return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join "`n") }
		}
	}

	AfterAll {
		Remove-Item -Recurse -Force $Dir
	}

	It 'passes when the only prerelease pin is allowed, and still shows it' {
		$result = Invoke-Verify $AllowedPath
		$result.ExitCode | Should -Be 0
		$result.Output | Should -Match '::warning::Allowed prerelease pin: Maui -> versionOverride\[net11.0\]'
	}

	It 'fails on the same manifest without the allowed entry' {
		$result = Invoke-Verify (Join-Path $Dir 'missing.json')
		$result.ExitCode | Should -Be 1
		$result.Output | Should -Match 'Maui -> versionOverride\[net11.0\] = 11.0.0-rc.2.26505.5'
	}
}
