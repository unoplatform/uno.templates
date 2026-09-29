# Unit tests for the pure decision logic in Verify-StablePins.ps1.
#
# The script guards its main body so that dot-sourcing loads only the functions (no manifest
# reading, no exits). These tests therefore run fully offline. Requires Pester v5+.
#
#   Invoke-Pester -Path build/Verify-StablePins.Tests.ps1

BeforeAll {
	. (Join-Path $PSScriptRoot 'Verify-StablePins.ps1')
}

Describe 'Test-IsPrereleaseVersion' {
	It 'flags dev pins' {
		Test-IsPrereleaseVersion -Version '10.1.0-dev.214' | Should -BeTrue
		Test-IsPrereleaseVersion -Version '1.13.0-dev.17' | Should -BeTrue
		Test-IsPrereleaseVersion -Version '6.4.0-dev.13' | Should -BeTrue
	}

	It 'accepts stable versions' {
		Test-IsPrereleaseVersion -Version '10.0.96' | Should -BeFalse
		Test-IsPrereleaseVersion -Version '1.12.1' | Should -BeFalse
		Test-IsPrereleaseVersion -Version '6.7.135' | Should -BeFalse
	}

	It 'does not treat a long stable build number as prerelease' {
		# WinAppSdk ships versions like this; they contain no hyphen and must pass.
		Test-IsPrereleaseVersion -Version '1.7.250909003' | Should -BeFalse
		Test-IsPrereleaseVersion -Version '10.0.28000.2705' | Should -BeFalse
	}

	It 'ignores build metadata after +' {
		Test-IsPrereleaseVersion -Version '6.7.135+1e094106' | Should -BeFalse
		Test-IsPrereleaseVersion -Version '6.7.0-dev.1+abc' | Should -BeTrue
	}

	It 'treats blank as not prerelease' {
		Test-IsPrereleaseVersion -Version '' | Should -BeFalse
	}
}

Describe 'Test-IsStableReleaseBranch' {
	It 'matches stable release branches' {
		Test-IsStableReleaseBranch -Branch 'release/stable/6.7' | Should -BeTrue
		Test-IsStableReleaseBranch -Branch 'refs/heads/release/stable/6.6' | Should -BeTrue
	}

	It 'does not match main or servicing lines' {
		Test-IsStableReleaseBranch -Branch 'main' | Should -BeFalse
		Test-IsStableReleaseBranch -Branch 'servicing/6.8' | Should -BeFalse
		Test-IsStableReleaseBranch -Branch 'refs/heads/main' | Should -BeFalse
	}

	It 'does not match a feature branch that merely mentions release' {
		Test-IsStableReleaseBranch -Branch 'dev/agzi/release-notes' | Should -BeFalse
	}

	It 'treats blank as not a release branch' {
		Test-IsStableReleaseBranch -Branch '' | Should -BeFalse
	}
}

Describe 'Get-ManifestPinnedVersions' {
	BeforeAll {
		$script:Manifest = @'
[
  { "group": "Core", "version": "6.7.135", "packages": [ "Uno.WinUI" ] },
  { "group": "WasmBootstrap", "version": "9.0.23", "packages": [ "Uno.Wasm.Bootstrap" ],
    "versionOverride": { "net10.0": "10.1.0-dev.214" } },
  { "group": "Resizetizer", "version": "1.13.0-dev.17", "packages": [ "Uno.Resizetizer" ] }
]
'@ | ConvertFrom-Json
	}

	It 'returns one record per group version' {
		$pins = Get-ManifestPinnedVersions -Manifest $Manifest
		($pins | Where-Object { $_.Field -eq 'version' }).Count | Should -Be 3
	}

	It 'also returns versionOverride entries' {
		$pins = Get-ManifestPinnedVersions -Manifest $Manifest
		$override = $pins | Where-Object { $_.Field -eq 'versionOverride[net10.0]' }
		$override.Group | Should -Be 'WasmBootstrap'
		$override.Version | Should -Be '10.1.0-dev.214'
	}
}

Describe 'Get-PrereleasePins' {
	It 'finds a dev pin hidden in a versionOverride' {
		$manifest = @'
[
  { "group": "Core", "version": "6.7.135" },
  { "group": "WasmBootstrap", "version": "9.0.23", "versionOverride": { "net10.0": "10.1.0-dev.214" } }
]
'@ | ConvertFrom-Json

		$found = Get-PrereleasePins -Pins (Get-ManifestPinnedVersions -Manifest $manifest)

		$found.Count | Should -Be 1
		$found[0].Group | Should -Be 'WasmBootstrap'
		$found[0].Field | Should -Be 'versionOverride[net10.0]'
	}

	It 'returns nothing for an all-stable manifest' {
		$manifest = @'
[
  { "group": "Core", "version": "6.7.135" },
  { "group": "Resizetizer", "version": "1.12.2" },
  { "group": "WinAppSdk", "version": "1.7.250909003" }
]
'@ | ConvertFrom-Json

		(Get-PrereleasePins -Pins (Get-ManifestPinnedVersions -Manifest $manifest)).Count | Should -Be 0
	}
}

Describe 'Test-ReadMeMatchesManifest' {
	BeforeAll {
		$script:ManifestJson = @'
[
  { "group": "Core", "version": "6.7.135" }
]
'@
	}

	It 'accepts a ReadMe whose fenced block matches, ignoring formatting' {
		$readMe = "# Uno.Sdk`n`n" + '```json' + "`n[ {`"group`":`"Core`",`"version`":`"6.7.135`"} ]`n" + '```'
		Test-ReadMeMatchesManifest -ManifestJson $ManifestJson -ReadMeContent $readMe | Should -BeTrue
	}

	It 'rejects a ReadMe left on a stale version' {
		$readMe = "# Uno.Sdk`n`n" + '```json' + "`n[ {`"group`":`"Core`",`"version`":`"6.7.103`"} ]`n" + '```'
		Test-ReadMeMatchesManifest -ManifestJson $ManifestJson -ReadMeContent $readMe | Should -BeFalse
	}

	It 'rejects a ReadMe with no fenced manifest at all' {
		Test-ReadMeMatchesManifest -ManifestJson $ManifestJson -ReadMeContent '# Uno.Sdk' | Should -BeFalse
	}
}
