#!/usr/bin/env pwsh
# Generates an Uno app for each presentation framework, adds every item template,
# then builds the app (Desktop head) to prove the items compile.
param(
    [string]$WorkDir = (Join-Path $env:TEMP "uno-item-template-tests")
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$content = Join-Path $repoRoot "src/Uno.Templates/content"

# $ErrorActionPreference = "Stop" does NOT trap non-zero exit codes from native
# executables, so every dotnet invocation must be checked explicitly.
# NOTE: this is a simple (non-advanced) function on purpose. Declaring a param()
# with [Parameter(...)] would add common parameters (-OutVariable/-OutBuffer/...),
# and an arg like `-o` would bind to those instead of passing through to dotnet.
# Using the automatic $args avoids that.
function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed (exit $LASTEXITCODE)" }
}

# Install every item template from source.
$items = @(
    "item-page", "item-window", "item-usercontrol", "item-contentdialog",
    "item-resourcedictionary", "item-resourcedictionary-codebehind", "item-resw",
    "item-templatedcontrol", "item-mvvm-page", "item-mvux-page"
)
foreach ($i in $items) {
    Invoke-Dotnet new install (Join-Path $content $i) --force
}

# The generated namespace comes from a bind to the host project's RootNamespace,
# so a passing build alone would not catch the binding silently reverting to the
# "UnoApp" fallback. Assert the namespace explicitly.
function Assert-Namespace {
    param([string]$File, [string]$Expected)

    $m = Select-String -Path $File -Pattern '^\s*namespace\s+([A-Za-z0-9_.]+)' | Select-Object -First 1
    if (-not $m) { throw "No namespace declaration found in $File" }

    $actual = $m.Matches[0].Groups[1].Value
    if ($actual -ne $Expected) {
        throw "Expected namespace '$Expected' in $File, found '$actual'"
    }
}

function Test-Items {
    param([string]$Name, [string]$Preset, [string]$Presentation, [string]$Markup)

    $appDir = Join-Path $WorkDir $Name
    Remove-Item -Recurse -Force $appDir -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $appDir | Out-Null

    # The app/project name must be a valid C# identifier. Using the combo name
    # directly (e.g. "mvvm-xaml") makes dotnet new infer a hyphenated project
    # name, which Uno's source generators turn into `namespace mvvm-xaml` and the
    # build fails. Derive a PascalCase identifier instead.
    $appName = (($Name -split '[^A-Za-z0-9]') | Where-Object { $_ } |
        ForEach-Object { $_.Substring(0, 1).ToUpper() + $_.Substring(1) }) -join ''

    Push-Location $appDir
    try {
        Invoke-Dotnet new unoapp -preset $Preset -presentation $Presentation -markup $Markup -platforms desktop -n $appName -o . --force

        $markupArgs = @("-markup", $Markup)

        Invoke-Dotnet new uno-page -n SampleItemPage @markupArgs
        Invoke-Dotnet new uno-window -n SampleItemWindow @markupArgs
        Invoke-Dotnet new uno-usercontrol -n SampleItemControl @markupArgs
        Invoke-Dotnet new uno-contentdialog -n SampleItemDialog @markupArgs
        Invoke-Dotnet new uno-resourcedictionary -n SampleDictionary
        Invoke-Dotnet new uno-resourcedictionary-codebehind -n SampleDictionaryCb
        Invoke-Dotnet new uno-resw -n SampleStrings
        Invoke-Dotnet new uno-templatedcontrol -n SampleTemplatedControl

        if ($Presentation -eq "mvvm") { Invoke-Dotnet new uno-mvvm-page -n SampleMvvmPage }
        if ($Presentation -eq "mvux") { Invoke-Dotnet new uno-mvux-page -n SampleMvuxPage }

        # C# Markup renames BlankPage.xaml.cs to BlankPage.cs, so the code-behind
        # suffix depends on the markup mode.
        $codeBehind = if ($Markup -eq "csharp") { ".cs" } else { ".xaml.cs" }

        # Inferred from the host project, and overridable.
        Assert-Namespace -File "SampleItemPage$codeBehind" -Expected $appName

        Invoke-Dotnet new uno-page -n SampleNamespacedPage -ns "Contoso.Custom" @markupArgs
        Assert-Namespace -File "SampleNamespacedPage$codeBehind" -Expected "Contoso.Custom"

        Invoke-Dotnet build -f net10.0-desktop
        Write-Host "PASS: $Name" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

Test-Items -Name "mvvm-xaml"   -Preset recommended -Presentation mvvm -Markup xaml
Test-Items -Name "mvux-xaml"   -Preset recommended -Presentation mvux -Markup xaml
Test-Items -Name "mvvm-csharp" -Preset recommended -Presentation mvvm -Markup csharp

Write-Host "All item template integration tests passed." -ForegroundColor Green
