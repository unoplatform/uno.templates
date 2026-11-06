# Unit tests for the scope and matrix logic in Get-TemplateTestMatrix.ps1.
#
# The script guards its main body so that dot-sourcing loads only the functions. These tests
# therefore run fully offline. Requires Pester v5+.
#
#   Invoke-Pester -Path build/Get-TemplateTestMatrix.Tests.ps1

BeforeAll {
	. (Join-Path $PSScriptRoot 'Get-TemplateTestMatrix.ps1')

	# The real test list: the smoke tests must keep naming tests that exist.
	$actionPath = Join-Path $PSScriptRoot '../.github/actions/ci/generate-test-matrix/action.yml'
	$script:RealTests = Get-Content $actionPath |
		Where-Object { $_ -match '^\s*"[A-Za-z0-9_]+;[^;"]*;[^;"]*;[^"]*",?\s*$' } |
		ForEach-Object { ($_ -replace '^\s*"', '') -replace '",?\s*$', '' }

	function Get-Validations($Matrix) {
		@($Matrix | ForEach-Object { $_.validations -split "`r`n" })
	}
}

Describe 'Get-CiScope' {
	Context 'always the full matrix' {
		It 'runs it on the nightly canary and on manual runs' {
			Get-CiScope -EventName schedule | Should -Be 'full'
			Get-CiScope -EventName workflow_dispatch | Should -Be 'full'
		}

		It 'runs it on PRs to a stable release line, whatever they change' {
			Get-CiScope -EventName pull_request -BaseRef 'release/stable/6.7' -ChangedFiles @('README.md') | Should -Be 'full'
		}

		It 'runs it on pushes to a stable release line, whatever they change' {
			Get-CiScope -EventName push -Ref 'refs/heads/release/stable/6.7' -ChangedFiles @('README.md') | Should -Be 'full'
		}

		It 'runs it on a PR labeled ci/full-matrix' {
			Get-CiScope -EventName pull_request -BaseRef main -Labels @('area/sdk', 'ci/full-matrix') -ChangedFiles @('README.md') | Should -Be 'full'
		}

		It 'runs it on PRs to a servicing line, whatever they change' {
			Get-CiScope -EventName pull_request -BaseRef 'servicing/6.8' -ChangedFiles @('src/Uno.Templates/Uno.Templates.csproj') | Should -Be 'full'
		}

		It 'runs it when the changed files could not be determined' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles $null | Should -Be 'full'
			Get-CiScope -EventName push -Ref 'refs/heads/main' -ChangedFiles $null | Should -Be 'full'
		}
	}

	Context 'pull requests' {
		It 'runs nothing when nothing that ships changed' {
			$files = @(
				'README.md', 'specs/001-x/spec.md', 'build/Verify-StablePins.ps1', 'build/templates/x.json',
				'tools/Uno.Sdk.Updater/Program.cs', '.github/labeler.yml', '.github/workflows/uno-updater.yml',
				'.github/ISSUE_TEMPLATE/bug-report.yml')
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles $files | Should -Be 'none'
		}

		It 'runs nothing when the PR changed nothing' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @() | Should -Be 'none'
		}

		It 'runs the smoke tests for an ordinary template change' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('src/Uno.Templates/content/unoapp/MyExtensionsApp.1/App.xaml') | Should -Be 'smoke'
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('src/Uno.Templates/content/unoapp/.template.config/template.json') | Should -Be 'smoke'
		}

		It 'treats READMEs inside the templates as shipped' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('src/Uno.Templates/content/unoapp/ReadMe.md') | Should -Be 'smoke'
		}

		It 'treats an unknown path as shipped' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('somewhere/new.txt') | Should -Be 'smoke'
		}

		It 'checks the matrix selection itself when it changes' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('build/Get-TemplateTestMatrix.ps1') | Should -Be 'smoke'
		}

		It 'needs no Mac to add a test to the list' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('.github/actions/ci/generate-test-matrix/action.yml') | Should -Be 'smoke'
		}

		It 'needs no Mac for a wizard icon named after an Apple platform' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('src/Uno.Templates/content/unoapp/.template.config/Assets/Dark/Platforms.MacOS.svg') | Should -Be 'smoke'
		}

		It 'lets one shipped file outweigh the others' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('README.md', 'src/Uno.Templates/Uno.Templates.csproj') | Should -Be 'smoke'
		}

		It 'adds macOS when an Apple file changes' -ForEach @(
			@{ Path = 'src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Platforms/iOS/Main.iOS.cs' }
			@{ Path = 'src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Platforms/iOS/Info.plist' }
			@{ Path = 'src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Platforms/MacCatalyst/Program.cs' }
			@{ Path = 'src/Uno.Templates/content/unoapp/MyExtensionsApp.1/Assets/x.xcassets/y.json' }
		) {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @($Path) | Should -Be 'smoke-macos'
		}

		It 'adds macOS when the package versions change' -ForEach @(
			@{ Path = 'src/Uno.Sdk/packages.json' }
			@{ Path = 'src/Uno.Sdk.Updater.targets' }
			@{ Path = 'src/Directory.Build.props' }
			@{ Path = 'nuget.config' }
		) {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @($Path) | Should -Be 'smoke-macos'
		}

		It 'adds macOS when the test pipeline itself changes' -ForEach @(
			@{ Path = '.github/workflows/ci.yml' }
			@{ Path = '.github/actions/ci/run-tests/action.yml' }
			@{ Path = '.github/actions/ci/dotnet-install/action.yml' }
		) {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @($Path) | Should -Be 'smoke-macos'
		}

		It 'does not mistake a word containing ios for an Apple file' {
			Get-CiScope -EventName pull_request -BaseRef main -ChangedFiles @('src/Uno.Templates/content/unoapp/Scenarios/Radios.cs') | Should -Be 'smoke'
		}
	}

	Context 'pushes' {
		It 'runs the full matrix when something that ships changed' {
			Get-CiScope -EventName push -Ref 'refs/heads/main' -ChangedFiles @('src/Uno.Templates/Uno.Templates.csproj') | Should -Be 'full'
			Get-CiScope -EventName push -Ref 'refs/heads/servicing/6.8' -ChangedFiles @('src/Uno.Sdk/packages.json') | Should -Be 'full'
		}

		It 'runs nothing when nothing that ships changed' {
			Get-CiScope -EventName push -Ref 'refs/heads/main' -ChangedFiles @('README.md', 'build/Verify-StablePins.Tests.ps1') | Should -Be 'none'
		}
	}
}

Describe 'Get-TestMatrix' {
	BeforeAll {
		$script:SampleTests = @(
			'All;;unoapp;-skip'
			'NotMac;!macOS;unoapp;-skip -a'
			'LinuxOnly;Linux;unoapp;-skip -b'
			'WindowsOnly;Windows;unoapp;-skip -c'
			'MacOnly;macOS;unoapp;-skip -d'
			'Other;;unoapp;-skip -e'
		)
		$script:SampleSmoke = @{
			Linux   = @('All', 'LinuxOnly')
			Windows = @('All', 'WindowsOnly')
			macOS   = @('MacOnly')
			Net11   = @('All')
		}
	}

	It 'returns nothing for scope none' {
		@(Get-TestMatrix -Tests $SampleTests -Os Linux -Scope none -GroupSize 8 -SmokeTests $SampleSmoke).Count | Should -Be 0
	}

	It 'runs every applicable test on both target frameworks for the full matrix' {
		$validations = Get-Validations (Get-TestMatrix -Tests $SampleTests -Os Linux -Scope full -GroupSize 2 -SmokeTests $SampleSmoke)
		$validations | Should -HaveCount 8
		$validations | Should -Contain 'Other;;unoapp;-skip -e -tfm net11.0'
		$validations | Should -Contain 'LinuxOnly;Linux;unoapp;-skip -b -tfm net10.0'
		($validations -match '^(WindowsOnly|MacOnly);').Count | Should -Be 0
	}

	It 'filters per OS for the full matrix' {
		$validations = Get-Validations (Get-TestMatrix -Tests $SampleTests -Os macOS -Scope full -GroupSize 2 -SmokeTests $SampleSmoke)
		($validations -match '^NotMac;').Count | Should -Be 0
		($validations -match '^MacOnly;').Count | Should -Be 2
	}

	It 'runs only the smoke tests of that OS, and the smaller set on net11, for the smoke tier' {
		$validations = Get-Validations (Get-TestMatrix -Tests $SampleTests -Os Linux -Scope smoke -GroupSize 8 -SmokeTests $SampleSmoke)
		$validations | Should -HaveCount 3
		$validations | Should -Contain 'All;;unoapp;-skip -tfm net10.0'
		$validations | Should -Contain 'LinuxOnly;Linux;unoapp;-skip -b -tfm net10.0'
		$validations | Should -Contain 'All;;unoapp;-skip -tfm net11.0'
	}

	It 'skips macOS unless the scope asks for it' {
		@(Get-TestMatrix -Tests $SampleTests -Os macOS -Scope smoke -GroupSize 8 -SmokeTests $SampleSmoke).Count | Should -Be 0
		@(Get-TestMatrix -Tests $SampleTests -Os macOS -Scope smoke-macos -GroupSize 8 -SmokeTests $SampleSmoke).Count | Should -BeGreaterThan 0
	}

	It 'still runs Linux and Windows for smoke-macos' {
		@(Get-TestMatrix -Tests $SampleTests -Os Windows -Scope smoke-macos -GroupSize 8 -SmokeTests $SampleSmoke).Count | Should -BeGreaterThan 0
	}

	It 'names the groups uniquely, with their target framework and the arguments uno-check needs' {
		$matrix = @(Get-TestMatrix -Tests $SampleTests -Os Linux -Scope full -GroupSize 2 -SmokeTests $SampleSmoke)
		($matrix.groupName | Select-Object -Unique).Count | Should -Be $matrix.Count
		$matrix | Where-Object groupName -like '*_net11' | ForEach-Object { $_.unocheckArguments | Should -Be '--pre-major' }
		$matrix | Where-Object groupName -like '*_net10' | ForEach-Object { $_.unocheckArguments | Should -Be '' }
	}

	It 'adds the tests a PR adds to the smoke tier, on the OSes they apply to' {
		$validations = Get-Validations (Get-TestMatrix -Tests $SampleTests -Os Linux -Scope smoke -GroupSize 8 -SmokeTests $SampleSmoke -AddedTests @('Other', 'MacOnly'))
		$validations | Should -Contain 'Other;;unoapp;-skip -e -tfm net10.0'
		$validations | Should -Contain 'Other;;unoapp;-skip -e -tfm net11.0'
		($validations -match '^MacOnly;').Count | Should -Be 0
	}

	It 'does not run a test twice when a PR changes a smoke test' {
		$validations = Get-Validations (Get-TestMatrix -Tests $SampleTests -Os Linux -Scope smoke -GroupSize 8 -SmokeTests $SampleSmoke -AddedTests @('All'))
		($validations -match '^All;.*net10').Count | Should -Be 1
	}

	It 'rejects a smoke test that does not exist, or that the OS filters out' {
		{ Get-TestMatrix -Tests $SampleTests -Os Linux -Scope smoke -GroupSize 8 -SmokeTests @{ Linux = @('Gone'); Windows = @(); macOS = @(); Net11 = @() } } | Should -Throw '*Gone*'
		{ Get-TestMatrix -Tests $SampleTests -Os Linux -Scope smoke -GroupSize 8 -SmokeTests @{ Linux = @('MacOnly'); Windows = @(); macOS = @(); Net11 = @() } } | Should -Throw '*MacOnly*'
	}
}

Describe 'Get-AddedTestNames' {
	It 'finds added tests, and tests whose arguments changed' {
		$names = Get-AddedTestNames -Tests @('Same;;unoapp;-skip', 'New;;unoapp;-a', 'Changed;;unoapp;-b') -BaseTests @('Same;;unoapp;-skip', 'Changed;;unoapp;-c', 'Removed;;unoapp;-d')
		$names | Should -Be @('New', 'Changed')
	}

	It 'finds nothing when the list is unchanged' {
		@(Get-AddedTestNames -Tests @('Same;;unoapp;-skip') -BaseTests @('Same;;unoapp;-skip')).Count | Should -Be 0
	}
}

Describe 'The real test list' {
	It 'has no two tests with the same template and arguments on the same OS filter' {
		$keys = $RealTests | ForEach-Object {
			$fields = $_ -split ';', 4
			"$($fields[1])|$($fields[2])|$((($fields[3] -split '\s+' | Where-Object { $_ }) | Sort-Object) -join ' ')"
		}
		$duplicates = $keys | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name
		$duplicates | Should -BeNullOrEmpty
	}

	It 'contains every smoke test, on every OS that names it' -ForEach @(
		@{ Os = 'Linux' }
		@{ Os = 'Windows' }
		@{ Os = 'macOS' }
	) {
		{ Get-TestMatrix -Tests $RealTests -Os $Os -Scope smoke-macos -GroupSize 8 } | Should -Not -Throw
	}

	It 'keeps the full matrix a superset of the smoke tier' -ForEach @(
		@{ Os = 'Linux' }
		@{ Os = 'Windows' }
		@{ Os = 'macOS' }
	) {
		$full = Get-Validations (Get-TestMatrix -Tests $RealTests -Os $Os -Scope full -GroupSize 8)
		$smoke = Get-Validations (Get-TestMatrix -Tests $RealTests -Os $Os -Scope smoke-macos -GroupSize 8)
		$smoke | Where-Object { $full -notcontains $_ } | Should -BeNullOrEmpty
	}
}
