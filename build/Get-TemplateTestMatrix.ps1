<#
.SYNOPSIS
	Decides which template tests a CI run needs and writes the per-OS test matrices.
.DESCRIPTION
	A scope says how much of the template test list runs:

	  none         Nothing that ships changed, so no template test runs.
	  smoke        The smoke tests on Linux and Windows.
	  smoke-macos  The same, plus the macOS smoke tests.
	  full         Every test, on every OS and target framework.

	See "What CI does" in the README for when each scope applies.
#>
param(
	# The "TestName;[PlatformFilter];TemplateName;Arguments" list from the generate-test-matrix action.
	[string[]]$Tests,
	[string]$EventName,
	# github.ref: the branch of a push.
	[string]$Ref,
	# github.base_ref: the target branch of a PR.
	[string]$BaseRef,
	# github.event.before: the previous head of the branch that was pushed to.
	[string]$BeforeSha,
	[string]$PullRequestNumber
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:FullMatrixLabel = 'ci/full-matrix'

# Files that are not in the packages and don't take part in building them. Anything not listed here counts
# as shipped, so a new path gets tested until someone decides otherwise.
$script:NotShippedPattern = '^(' + (@(
		'README\.md'
		'CODE_OF_CONDUCT\.md'
		'SECURITY\.md'
		'LICENSE\.md'
		'\.gitignore'
		'\.mergify\.yml'
		'\.vscode/.*'
		'specs/.*'
		# CI scripts and the updater. What the updater changes reaches the packages through the files it
		# generates, which do count as shipped.
		'build/.*'
		'tools/.*'
		'\.github/ISSUE_TEMPLATE/.*'
		'\.github/CODEOWNERS'
		'\.github/config\.yml'
		'\.github/dependabot\.yml'
		'\.github/invite-contributors\.yml'
		'\.github/labeler\.yml'
		'\.github/no-response\.yml'
		'\.github/pull_request_template\.md'
		'\.github/settings\.yml'
		'\.github/sdk-update-exclude\.json'
		'\.github/workflows/(conventional-commits|labeler|uno-updater|uno-updater-pr)\.yml'
		# Only the updater and the canary use it, and the canary runs the full matrix.
		'\.github/actions/sdk-update/.*'
	) -join '|') + ')$'

# The script that picks the tests is the one thing in build/ that must test itself.
$script:MatrixScriptPattern = '^build/Get-TemplateTestMatrix\.ps1$'

# The wizard's icons ship, but no build reads them, so Platforms.MacOS.svg needs no Mac.
$script:WizardAssetsPattern = '/\.template\.config/Assets/'

# Changes that can break macOS without showing on the Linux and Windows builds: Apple-specific files,
# package versions, and how the tests run. Both macOS-only breaks of Sep-Nov 2026 came from the last two
# (MT2390 on net11 in #2281, MSB4236 in #2229).
$script:MacOSPattern = '(?i)' + (@(
		'(^|[/._-])(ios|maccatalyst|macos|tvos|xcode)([/._-]|$)'
		'\.plist$'
		'\.entitlements$'
		'\.xcassets/'
		'^src/Uno\.Sdk/'
		'^src/Uno\.Sdk\.Updater\.targets$'
		'^src/Directory\.'
		'^nuget\.config$'
		'^\.github/workflows/ci\.yml$'
		'^\.github/actions/ci/(run-tests|dotnet-install)/'
	) -join '|')

# The template's main axes: blank, recommended and server apps, MVUX and MVVM, XAML and C# Markup, the auth
# flows, the head selections, and the templates other than unoapp. Linux has the tests that only run there;
# Windows has the Windows head and the libraries. Net11 proves the other target framework restores and builds.
# Add MAUI embedding here (MauiBlank, Issue396) when the Uno 7.0 exclusion in generate-test-matrix goes away.
$script:DefaultSmokeTests = @{
	Linux   = @(
		'DefaultArgs'
		'CSharpMarkup'
		'serverKiota'
		'Issue500'
		'Issue572'
		'WebAuthMvvmCSharp'
		'OidcAuthMvvm'
		'CustomAuthMvuxCSharp'
		'RecommendedShellMarkup'
		'MobileOnlyHeads'
		'SkiaOnlyHeads'
		'RecommendedSampleTests'
		'AndroidDevices'
		'CiGitHub'
		'CiAzure'
		'UITestLibrary'
	)
	Windows = @(
		'DefaultArgs'
		'serverKiota'
		'Issue500'
		'Issue572'
		'MobileOnlyHeads'
		'SkiaOnlyHeads'
		'reactorapp_Windows'
		'unolib_Windows'
		'unolibNative_Windows'
	)
	macOS   = @(
		'DefaultArgs'
		'serverKiota'
		'CSharpMarkup'
		'reactorapp_MacOS'
		'unolib_MacOS'
		'unolibNative_MacOS'
	)
	Net11   = @(
		'DefaultArgs'
		'serverKiota'
	)
}

function Get-CiScope {
	param(
		[Parameter(Mandatory)][string]$EventName,
		[string]$Ref = '',
		[string]$BaseRef = '',
		[string[]]$Labels = @(),
		# $null when the changed files could not be determined.
		[AllowNull()][AllowEmptyCollection()][string[]]$ChangedFiles = $null
	)

	# The nightly canary and manual runs exist to run everything.
	if ($EventName -notin 'pull_request', 'push') {
		return 'full'
	}

	# Customers are on these lines, so they keep their full gate before the merge too. A servicing bump broke
	# only Issue396 in #2298, which no smoke test would have seen.
	if ($BaseRef -like 'release/*' -or $BaseRef -like 'servicing/*' -or $Ref -like 'refs/heads/release/*') {
		return 'full'
	}

	if ($Labels -contains $script:FullMatrixLabel) {
		return 'full'
	}

	if ($null -eq $ChangedFiles) {
		return 'full'
	}

	$shipped = @($ChangedFiles | Where-Object { $_ -notmatch $script:NotShippedPattern -or $_ -match $script:MatrixScriptPattern })
	if ($shipped.Count -eq 0) {
		return 'none'
	}

	if ($EventName -eq 'push') {
		return 'full'
	}

	if ($shipped | Where-Object { $_ -match $script:MacOSPattern -and $_ -notmatch $script:WizardAssetsPattern }) {
		return 'smoke-macos'
	}

	return 'smoke'
}

function Get-TestMatrix {
	param(
		[Parameter(Mandatory)][string[]]$Tests,
		[Parameter(Mandatory)][ValidateSet('Linux', 'Windows', 'macOS')][string]$Os,
		[Parameter(Mandatory)][ValidateSet('none', 'smoke', 'smoke-macos', 'full')][string]$Scope,
		[Parameter(Mandatory)][int]$GroupSize,
		[hashtable]$SmokeTests = $script:DefaultSmokeTests,
		# Tests the PR adds or changes. The smoke tier runs them on the OSes they apply to.
		[string[]]$AddedTests = @()
	)

	if ($Scope -eq 'none' -or ($Scope -eq 'smoke' -and $Os -eq 'macOS')) {
		return @()
	}

	# Filter per OS before grouping, so no runner is spent on a group that skips most of its tests.
	$osTests = @($Tests | Where-Object {
			$filter = ($_ -split ';')[1].Trim()
			switch ($filter) {
				'' { $true }
				'!macOS' { $Os -ne 'macOS' }
				default { $filter -eq $Os }
			}
		})

	$selectTests = {
		param([string[]]$Names)
		$applicable = @($AddedTests | Where-Object { $name = $_; $osTests | Where-Object { ($_ -split ';')[0] -eq $name } })
		foreach ($name in @($Names) + $applicable | Select-Object -Unique) {
			$test = $osTests | Where-Object { ($_ -split ';')[0] -eq $name }
			if (-not $test) {
				throw "Smoke test '$name' doesn't exist on $Os. Update the smoke lists in Get-TemplateTestMatrix.ps1."
			}
			$test
		}
	}

	$tiers = if ($Scope -eq 'full') {
		@(
			@{ Name = 'net10'; UnoCheckArguments = ''; Tests = $osTests }
			@{ Name = 'net11'; UnoCheckArguments = '--pre-major'; Tests = $osTests }
		)
	}
	else {
		@(
			@{ Name = 'net10'; UnoCheckArguments = ''; Tests = @(& $selectTests $SmokeTests[$Os]) }
			@{ Name = 'net11'; UnoCheckArguments = '--pre-major'; Tests = @(& $selectTests $SmokeTests['Net11']) }
		)
	}

	$matrixGroups = @()

	foreach ($tier in $tiers) {
		$tierTests = @($tier.Tests)
		$groupCount = [Math]::Ceiling($tierTests.Count / $GroupSize)

		for ($g = 0; $g -lt $groupCount; $g++) {
			# Round-robin, so expensive neighbours in the list end up in different groups
			$groupTests = for ($i = $g; $i -lt $tierTests.Count; $i += $groupCount) {
				$tierTests[$i] + " -tfm $($tier.Name).0"
			}

			$matrixGroups += @{
				'groupName'         = "Group$($matrixGroups.Count)_$($tier.Name)"
				'unocheckArguments' = $tier.UnoCheckArguments
				'validations'       = ($groupTests -join "`r`n")
			}
		}
	}

	return $matrixGroups
}

function Get-AddedTestNames {
	param([string[]]$Tests, [string[]]$BaseTests)

	@($Tests | Where-Object { $BaseTests -notcontains $_ } | ForEach-Object { ($_ -split ';')[0] })
}

function Get-BaseBranchTests {
	# A PR run checks out GitHub's merge commit, whose first parent is the base branch.
	$action = & git show 'HEAD^1:.github/actions/ci/generate-test-matrix/action.yml' 2>$null
	if ($LASTEXITCODE -ne 0) {
		Write-Host "::warning::Could not read the base branch's test list, so tests this PR adds may not run."
		return $null
	}

	$action | Where-Object { $_ -match '^\s*"[A-Za-z0-9_]+;[^;"]*;[^;"]*;[^"]*",?\s*$' } |
		ForEach-Object { ($_ -replace '^\s*"', '') -replace '",?\s*$', '' }
}

function Get-ChangedFiles {
	param([string]$EventName, [string]$BeforeSha)

	# A PR run checks out GitHub's merge commit, whose first parent is the base branch.
	$range = if ($EventName -eq 'pull_request') {
		@('HEAD^1', 'HEAD')
	}
	elseif ($EventName -eq 'push' -and $BeforeSha -and $BeforeSha -notmatch '^0+$') {
		@($BeforeSha, 'HEAD')
	}
	else {
		return $null
	}

	$files = & git diff --name-only @range 2>$null
	if ($LASTEXITCODE -ne 0) {
		Write-Host "::warning::Could not list the changed files of $($range -join '..'), so the full test matrix runs."
		return $null
	}

	return , @($files | Where-Object { $_ })
}

function Get-PullRequestLabels {
	param([string]$PullRequestNumber)

	# Read now rather than from the event payload, so adding the label and re-running the jobs picks it up.
	$labels = & gh pr view $PullRequestNumber --json labels --jq '.labels[].name' 2>$null
	if ($LASTEXITCODE -ne 0) {
		Write-Host "::warning::Could not read the labels of PR #$PullRequestNumber, so $script:FullMatrixLabel is not honored in this run."
		return @()
	}

	return @($labels)
}

if ($MyInvocation.InvocationName -ne '.') {
	$changedFiles = Get-ChangedFiles -EventName $EventName -BeforeSha $BeforeSha
	$labels = if ($EventName -eq 'pull_request') { Get-PullRequestLabels -PullRequestNumber $PullRequestNumber } else { @() }
	$scope = Get-CiScope -EventName $EventName -Ref $Ref -BaseRef $BaseRef -Labels $labels -ChangedFiles $changedFiles

	Write-Host "Scope: $scope"

	$addedTests = @()
	if ($EventName -eq 'pull_request') {
		$baseTests = Get-BaseBranchTests
		if ($null -ne $baseTests) {
			$addedTests = Get-AddedTestNames -Tests $Tests -BaseTests $baseTests
			Write-Host "Tests this PR adds or changes: $($addedTests -join ', ')"
		}
	}

	$summary = @("### Template tests: ``$scope``", '', '| OS | Jobs | Tests |', '| --- | ---: | ---: |')

	# macOS runners are scarce and queue for a long time, so macOS uses fewer, larger groups.
	foreach ($target in @(
			@{ Os = 'Linux'; Output = 'LinuxTestMatrix'; GroupSize = 8 }
			@{ Os = 'Windows'; Output = 'WindowsTestMatrix'; GroupSize = 8 }
			@{ Os = 'macOS'; Output = 'MacOSTestMatrix'; GroupSize = 10 })) {
		$matrix = @(Get-TestMatrix -Tests $Tests -Os $target.Os -Scope $scope -GroupSize $target.GroupSize -AddedTests $addedTests)
		$testCount = @($matrix | ForEach-Object { $_.validations -split "`r`n" }).Count

		Write-Host "$($target.Os): $testCount tests, $($matrix.Count) jobs"
		$summary += "| $($target.Os) | $($matrix.Count) | $testCount |"

		"$($target.Output)=$(ConvertTo-Json -InputObject $matrix -Compress)" >> $env:GITHUB_OUTPUT
	}

	if ($scope -like 'smoke*') {
		$summary += ''
		$summary += "To run everything on this PR, add the ``$script:FullMatrixLabel`` label and re-run the jobs."
	}

	$summary >> $env:GITHUB_STEP_SUMMARY
}
