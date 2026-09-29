# Unit tests for the pure decision logic in Verify-NuGetDependenciesOnNuGetOrg.ps1.
#
# The script guards its main body so that when it is dot-sourced only the functions
# load (no artifact scanning, no nuget.org calls). These tests therefore run fully
# offline. Requires Pester v5+ (Should -BeTrue / -BeFalse syntax).
#
#   Invoke-Pester -Path build/Verify-NuGetDependenciesOnNuGetOrg.Tests.ps1

BeforeAll {
	. (Join-Path $PSScriptRoot 'Verify-NuGetDependenciesOnNuGetOrg.ps1')
	$script:UnoPrefixes = @('Uno.')
}

Describe 'Test-IsTrackedPackageId' {
	It 'matches Uno.* ids' {
		Test-IsTrackedPackageId -PackageId 'Uno.Material.WinUI' -IncludePrefixes $UnoPrefixes | Should -BeTrue
	}

	It 'is case-insensitive' {
		Test-IsTrackedPackageId -PackageId 'uno.sdk' -IncludePrefixes $UnoPrefixes | Should -BeTrue
	}

	It 'does not match third-party / BCL ids' {
		Test-IsTrackedPackageId -PackageId 'System.Text.Json' -IncludePrefixes $UnoPrefixes | Should -BeFalse
		Test-IsTrackedPackageId -PackageId 'Microsoft.Extensions.Logging' -IncludePrefixes $UnoPrefixes | Should -BeFalse
	}

	It 'does not partial-match a longer token (Unofficial is not Uno.)' {
		Test-IsTrackedPackageId -PackageId 'Unofficial.Thing' -IncludePrefixes $UnoPrefixes | Should -BeFalse
	}

	It 'returns false for null or empty prefixes' {
		Test-IsTrackedPackageId -PackageId 'Uno.X' -IncludePrefixes $null | Should -BeFalse
		Test-IsTrackedPackageId -PackageId 'Uno.X' -IncludePrefixes @() | Should -BeFalse
	}

	It 'supports multiple prefixes' {
		Test-IsTrackedPackageId -PackageId 'Xamarin.Foo' -IncludePrefixes @('Uno.', 'Xamarin.') | Should -BeTrue
	}
}

Describe 'Test-ShouldVerifyTransitiveDependency' {
	It 'always verifies prereleases (even third-party)' {
		Test-ShouldVerifyTransitiveDependency -PackageId 'System.Foo' -Version '1.0.0-dev.1' -IncludeStableTransitiveVersions $false -IncludePrefixes $UnoPrefixes | Should -BeTrue
	}

	It 'verifies a stable Uno.* dep (the unpublished 7.0.1 class)' {
		Test-ShouldVerifyTransitiveDependency -PackageId 'Uno.Material.WinUI' -Version '7.0.1' -IncludeStableTransitiveVersions $false -IncludePrefixes $UnoPrefixes | Should -BeTrue
		Test-ShouldVerifyTransitiveDependency -PackageId 'Uno.Simple.WinUI' -Version '7.0.1' -IncludeStableTransitiveVersions $false -IncludePrefixes $UnoPrefixes | Should -BeTrue
	}

	It 'skips a stable third-party dep by default' {
		Test-ShouldVerifyTransitiveDependency -PackageId 'System.Text.Json' -Version '9.0.0' -IncludeStableTransitiveVersions $false -IncludePrefixes $UnoPrefixes | Should -BeFalse
		Test-ShouldVerifyTransitiveDependency -PackageId 'Microsoft.Extensions.Logging' -Version '9.0.0' -IncludeStableTransitiveVersions $false -IncludePrefixes $UnoPrefixes | Should -BeFalse
	}

	It 'verifies stable third-party deps when IncludeStableTransitiveVersions is set' {
		Test-ShouldVerifyTransitiveDependency -PackageId 'System.Text.Json' -Version '9.0.0' -IncludeStableTransitiveVersions $true -IncludePrefixes $UnoPrefixes | Should -BeTrue
	}

	It 'in prerelease-only mode still verifies a prerelease dep' {
		Test-ShouldVerifyTransitiveDependency -PackageId 'Uno.Simple.WinUI' -Version '7.1.0-dev.1' -IncludeStableTransitiveVersions $false -IncludePrefixes $UnoPrefixes -PrereleaseDependenciesOnly $true | Should -BeTrue
	}

	It 'in prerelease-only mode skips a stable dep even a tracked Uno.* one' {
		Test-ShouldVerifyTransitiveDependency -PackageId 'Uno.Simple.WinUI' -Version '7.0.3' -IncludeStableTransitiveVersions $false -IncludePrefixes $UnoPrefixes -PrereleaseDependenciesOnly $true | Should -BeFalse
	}

	It 'prerelease-only overrides IncludeStableTransitiveVersions for a stable dep' {
		Test-ShouldVerifyTransitiveDependency -PackageId 'System.Text.Json' -Version '9.0.0' -IncludeStableTransitiveVersions $true -IncludePrefixes $UnoPrefixes -PrereleaseDependenciesOnly $true | Should -BeFalse
	}
}

Describe 'Test-IsPrereleaseVersion' {
	It 'is true for a dev / prerelease version' {
		Test-IsPrereleaseVersion -Version '7.1.0-dev.1' | Should -BeTrue
		Test-IsPrereleaseVersion -Version '2.9.0-dev.12' | Should -BeTrue
	}

	It 'is false for a stable version (incl. 4-part build-tools versions)' {
		Test-IsPrereleaseVersion -Version '7.0.3' | Should -BeFalse
		Test-IsPrereleaseVersion -Version '10.0.28000.2270' | Should -BeFalse
	}

	It 'is false for null / blank' {
		Test-IsPrereleaseVersion -Version $null | Should -BeFalse
		Test-IsPrereleaseVersion -Version '' | Should -BeFalse
		Test-IsPrereleaseVersion -Version '   ' | Should -BeFalse
	}
}

Describe 'Select-ChecksForVerification' {
	It 'returns the list unchanged when the switch is off (full closure)' {
		$checks = [System.Collections.Generic.List[object]]::new()
		$checks.Add([PSCustomObject]@{ Id = 'Uno.A'; Version = '7.0.3'; Source = 't' })
		$checks.Add([PSCustomObject]@{ Id = 'Uno.B'; Version = '7.1.0-dev.1'; Source = 't' })
		$result = Select-ChecksForVerification -Checks $checks -PrereleaseDependenciesOnly $false
		$result.Count | Should -Be 2
	}

	It 'keeps only prerelease coordinates when the switch is on' {
		$checks = [System.Collections.Generic.List[object]]::new()
		$checks.Add([PSCustomObject]@{ Id = 'Uno.A'; Version = '7.0.3'; Source = 't' })
		$checks.Add([PSCustomObject]@{ Id = 'Uno.B'; Version = '7.1.0-dev.1'; Source = 't' })
		$checks.Add([PSCustomObject]@{ Id = 'Uno.C'; Version = '9.0.3'; Source = 't' })
		$result = Select-ChecksForVerification -Checks $checks -PrereleaseDependenciesOnly $true
		$result.Count | Should -Be 1
		$result[0].Id | Should -Be 'Uno.B'
	}

	It 'returns an empty list when every coordinate is stable (stable-branch PR)' {
		$checks = [System.Collections.Generic.List[object]]::new()
		$checks.Add([PSCustomObject]@{ Id = 'Uno.A'; Version = '7.0.3'; Source = 't' })
		$checks.Add([PSCustomObject]@{ Id = 'Uno.C'; Version = '9.0.3'; Source = 't' })
		$result = Select-ChecksForVerification -Checks $checks -PrereleaseDependenciesOnly $true
		$result.Count | Should -Be 0
	}
}

Describe 'ConvertTo-MissingDependencyReport' {
	It 'shapes and sorts missing records (by Id) with their sources' {
		$missing = @{}
		$b = [PSCustomObject]@{ Id = 'Uno.B'; Version = '2.0.0-dev.1'; Sources = [System.Collections.Generic.List[string]]::new() }
		$b.Sources.Add('manifest:packages.json:GroupB')
		$a = [PSCustomObject]@{ Id = 'Uno.A'; Version = '1.0.0-dev.1'; Sources = [System.Collections.Generic.List[string]]::new() }
		$a.Sources.Add('manifest:packages.json:GroupA')
		$missing['Uno.B|2.0.0-dev.1'] = $b
		$missing['Uno.A|1.0.0-dev.1'] = $a

		$report = ConvertTo-MissingDependencyReport -MissingDependencies $missing
		$report.Count | Should -Be 2
		$report[0].Id | Should -Be 'Uno.A'
		$report[1].Id | Should -Be 'Uno.B'
		$report[0].Sources[0] | Should -Be 'manifest:packages.json:GroupA'
	}

	It 'returns an empty list when there are no missing deps' {
		$report = ConvertTo-MissingDependencyReport -MissingDependencies @{}
		$report.Count | Should -Be 0
	}
}

Describe 'Test-ShouldExpandCoordinate' {
	It 'expands any coordinate within TransitiveDependencyDepth' {
		Test-ShouldExpandCoordinate -PackageId 'System.Foo' -Depth 0 -TransitiveDependencyDepth 1 -MaxTrackedDepth 16 -IncludePrefixes $UnoPrefixes | Should -BeTrue
	}

	It 'does not expand a non-tracked coordinate beyond TransitiveDependencyDepth' {
		Test-ShouldExpandCoordinate -PackageId 'System.Foo' -Depth 1 -TransitiveDependencyDepth 1 -MaxTrackedDepth 16 -IncludePrefixes $UnoPrefixes | Should -BeFalse
	}

	It 'walks a tracked (Uno.*) coordinate to its full closure beyond TransitiveDependencyDepth' {
		Test-ShouldExpandCoordinate -PackageId 'Uno.Material.WinUI' -Depth 1 -TransitiveDependencyDepth 1 -MaxTrackedDepth 16 -IncludePrefixes $UnoPrefixes | Should -BeTrue
		Test-ShouldExpandCoordinate -PackageId 'Uno.Material.WinUI' -Depth 5 -TransitiveDependencyDepth 1 -MaxTrackedDepth 16 -IncludePrefixes $UnoPrefixes | Should -BeTrue
	}

	It 'stops the tracked walk at the runaway guard MaxTrackedDepth' {
		Test-ShouldExpandCoordinate -PackageId 'Uno.Material.WinUI' -Depth 16 -TransitiveDependencyDepth 1 -MaxTrackedDepth 16 -IncludePrefixes $UnoPrefixes | Should -BeFalse
	}
}

Describe 'Get-ExactDependencyVersion' {
	It 'returns an exact version unchanged' {
		Get-ExactDependencyVersion -VersionRange '7.0.1' | Should -Be '7.0.1'
		Get-ExactDependencyVersion -VersionRange '7.1.0-dev.1' | Should -Be '7.1.0-dev.1'
	}

	It 'unwraps a pinned [x] range' {
		Get-ExactDependencyVersion -VersionRange '[7.0.1]' | Should -Be '7.0.1'
	}

	It 'returns null for an open / floating range' {
		Get-ExactDependencyVersion -VersionRange '(1.0.0,2.0.0)' | Should -BeNullOrEmpty
	}
}

Describe 'Add-ChecksFromPackageGroups' {
	It 'adds a check for every package at its group base version' {
		$json = '[{"group":"Themes","version":"7.1.0-dev.1","packages":["Uno.Material.WinUI","Uno.Simple.WinUI"]}]'
		$checks = [System.Collections.Generic.List[object]]::new()
		Add-ChecksFromPackageGroups -Checks $checks -JsonContent $json -SourceLabel 'manifest:packages.json'
		$checks.Count | Should -Be 2
		($checks | Where-Object { $_.Id -eq 'Uno.Material.WinUI' }).Version | Should -Be '7.1.0-dev.1'
		($checks | Where-Object { $_.Id -eq 'Uno.Simple.WinUI' }).Version | Should -Be '7.1.0-dev.1'
	}

	It 'adds versionOverride entries in addition to the base version' {
		$json = '[{"group":"Core","version":"6.7.0-dev.869","packages":["Uno.WinUI"],"versionOverride":{"net8.0":"6.6.0"}}]'
		$checks = [System.Collections.Generic.List[object]]::new()
		Add-ChecksFromPackageGroups -Checks $checks -JsonContent $json -SourceLabel 'manifest:packages.json'
		@($checks | ForEach-Object { $_.Version } | Sort-Object -Unique) | Should -Be @('6.6.0', '6.7.0-dev.869')
	}

	It 'handles multiple groups' {
		$json = '[{"group":"A","version":"1.0.0","packages":["Uno.A"]},{"group":"B","version":"2.0.0","packages":["Uno.B","Uno.C"]}]'
		$checks = [System.Collections.Generic.List[object]]::new()
		Add-ChecksFromPackageGroups -Checks $checks -JsonContent $json -SourceLabel 'm'
		$checks.Count | Should -Be 3
	}
}

Describe 'Add-ChecksFromManifest' {
	It 'reads a manifest file and populates checks' {
		$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("manifest-" + [System.Guid]::NewGuid().ToString('N') + ".json")
		Set-Content -Path $tmp -Value '[{"group":"Themes","version":"7.1.0-dev.1","packages":["Uno.Material.WinUI"]}]'
		try {
			$checks = [System.Collections.Generic.List[object]]::new()
			Add-ChecksFromManifest -Checks $checks -ManifestPath $tmp
			$checks.Count | Should -Be 1
			$checks[0].Id | Should -Be 'Uno.Material.WinUI'
			$checks[0].Version | Should -Be '7.1.0-dev.1'
		}
		finally {
			Remove-Item $tmp -Force -ErrorAction SilentlyContinue
		}
	}

	It 'throws when the manifest file is missing' {
		$checks = [System.Collections.Generic.List[object]]::new()
		{ Add-ChecksFromManifest -Checks $checks -ManifestPath 'Z:\does\not\exist\packages.json' } | Should -Throw
	}
}

Describe 'Parameter validation' {
	BeforeAll {
		$script:ScriptPath = Join-Path $PSScriptRoot 'Verify-NuGetDependenciesOnNuGetOrg.ps1'
	}

	# MaxAttempts < 1 would skip the set-based verification loop entirely and silently
	# treat every dependency as available — ValidateRange must reject it at binding.
	It 'rejects MaxAttempts below 1' {
		{ & $ScriptPath -MaxAttempts 0 -ManifestPath 'x' } | Should -Throw
		{ & $ScriptPath -MaxAttempts -1 -ManifestPath 'x' } | Should -Throw
	}

	It 'rejects a negative RetryDelaySeconds' {
		{ & $ScriptPath -RetryDelaySeconds -1 -ManifestPath 'x' } | Should -Throw
	}

	It 'rejects HttpTimeoutSeconds below 1' {
		{ & $ScriptPath -HttpTimeoutSeconds 0 -ManifestPath 'x' } | Should -Throw
	}
}

Describe 'Add-StepSummaryLines' {
	# The success summary emits blank-line elements around the collapsible <details> block;
	# the parameter must accept empty strings (AllowEmptyString) or binding throws.
	It 'writes lines including blank lines without throwing' {
		$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("summary-" + [System.Guid]::NewGuid().ToString('N') + ".md")
		$prev = $env:GITHUB_STEP_SUMMARY
		$env:GITHUB_STEP_SUMMARY = $tmp
		try {
			{ Add-StepSummaryLines -Lines @('## Title', '', '<details><summary>x</summary>', '', '- a', '', '</details>') } | Should -Not -Throw
			$content = Get-Content $tmp -Raw
			$content | Should -Match '## Title'
			$content | Should -Match '<details>'
		}
		finally {
			$env:GITHUB_STEP_SUMMARY = $prev
			Remove-Item $tmp -Force -ErrorAction SilentlyContinue
		}
	}

	It 'is a no-op when GITHUB_STEP_SUMMARY is not set' {
		$prev = $env:GITHUB_STEP_SUMMARY
		$env:GITHUB_STEP_SUMMARY = $null
		try {
			{ Add-StepSummaryLines -Lines @('x', '') } | Should -Not -Throw
		}
		finally {
			$env:GITHUB_STEP_SUMMARY = $prev
		}
	}
}

Describe 'Get-NuGetVersionAvailability (tri-state)' {
	It 'returns Available when the exact version is listed' {
		Mock -CommandName Invoke-RestMethod -MockWith { [PSCustomObject]@{ versions = @('1.0.0', '2.0.0') } }
		Get-NuGetVersionAvailability -PackageId 'Uno.Test' -Version '2.0.0' -HttpTimeoutSeconds 5 | Should -Be 'Available'
	}

	It 'returns Absent when the id exists but the version is not listed (HTTP 200)' {
		Mock -CommandName Invoke-RestMethod -MockWith { [PSCustomObject]@{ versions = @('1.0.0') } }
		Get-NuGetVersionAvailability -PackageId 'Uno.Test' -Version '9.9.9' -HttpTimeoutSeconds 5 | Should -Be 'Absent'
	}

	It 'returns Error on a transient failure' {
		Mock -CommandName Invoke-RestMethod -MockWith { throw 'transient network error' }
		Get-NuGetVersionAvailability -PackageId 'Uno.Test' -Version '1.0.0' -HttpTimeoutSeconds 5 | Should -Be 'Error'
	}
}

Describe 'Test-NuGetVersionAvailability (retry policy)' {
	It 'does not retry a definitive Absent — a single probe even with MaxAttempts > 1' {
		Mock -CommandName Invoke-RestMethod -MockWith { [PSCustomObject]@{ versions = @('1.0.0') } }
		Test-NuGetVersionAvailability -PackageId 'Uno.Test' -Version '9.9.9' -MaxAttempts 5 -RetryDelaySeconds 0 -HttpTimeoutSeconds 5 | Should -BeFalse
		Should -Invoke Invoke-RestMethod -Times 1 -Exactly
	}

	It 'retries a transient error and then succeeds' {
		$script:calls = 0
		Mock -CommandName Invoke-RestMethod -MockWith {
			$script:calls++
			if ($script:calls -lt 2) { throw 'transient' }
			[PSCustomObject]@{ versions = @('1.0.0') }
		}
		Test-NuGetVersionAvailability -PackageId 'Uno.Test' -Version '1.0.0' -MaxAttempts 3 -RetryDelaySeconds 0 -HttpTimeoutSeconds 5 | Should -BeTrue
		Should -Invoke Invoke-RestMethod -Times 2 -Exactly
	}

	It 'returns false after exhausting retries on persistent transient errors' {
		Mock -CommandName Invoke-RestMethod -MockWith { throw 'always transient' }
		Test-NuGetVersionAvailability -PackageId 'Uno.Test' -Version '1.0.0' -MaxAttempts 3 -RetryDelaySeconds 0 -HttpTimeoutSeconds 5 | Should -BeFalse
		Should -Invoke Invoke-RestMethod -Times 3 -Exactly
	}
}
