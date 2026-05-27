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

function Test-Items {
    param([string]$Name, [string]$Preset, [string]$Presentation, [string]$Markup)

    $appDir = Join-Path $WorkDir $Name
    Remove-Item -Recurse -Force $appDir -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $appDir | Out-Null

    Push-Location $appDir
    try {
        Invoke-Dotnet new unoapp -preset $Preset -presentation $Presentation -markup $Markup -platforms desktop -o . --force

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
