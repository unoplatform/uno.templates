#!/usr/bin/env pwsh
# Generates an Uno app for each presentation framework, adds every item template,
# then builds the app (Desktop head) to prove the items compile.
param(
    [string]$WorkDir = (Join-Path $env:TEMP "uno-item-template-tests")
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$content = Join-Path $repoRoot "src/Uno.Templates/content"

# Install every item template from source.
$items = @(
    "item-page","item-window","item-usercontrol","item-contentdialog",
    "item-resourcedictionary","item-resourcedictionary-codebehind","item-resw",
    "item-templatedcontrol","item-mvvm-page","item-mvux-page"
)
foreach ($i in $items) {
    dotnet new install (Join-Path $content $i) --force
    if ($LASTEXITCODE -ne 0) { throw "Failed to install $i" }
}

function Test-Items {
    param([string]$Name, [string]$Preset, [string]$Presentation, [string]$Markup)

    $appDir = Join-Path $WorkDir $Name
    Remove-Item -Recurse -Force $appDir -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $appDir | Out-Null

    Push-Location $appDir
    try {
        dotnet new unoapp -preset $Preset -presentation $Presentation -markup $Markup -platforms desktop -o . --force
        if ($LASTEXITCODE -ne 0) { throw "unoapp generation failed for $Name" }

        $itemArgs = "-markup $Markup"
        dotnet new uno-page -n SampleItemPage $itemArgs.Split(' ')
        dotnet new uno-window -n SampleItemWindow $itemArgs.Split(' ')
        dotnet new uno-usercontrol -n SampleItemControl $itemArgs.Split(' ')
        dotnet new uno-contentdialog -n SampleItemDialog $itemArgs.Split(' ')
        dotnet new uno-resourcedictionary -n SampleDictionary
        dotnet new uno-resourcedictionary-codebehind -n SampleDictionaryCb
        dotnet new uno-resw -n SampleStrings
        dotnet new uno-templatedcontrol -n SampleTemplatedControl

        if ($Presentation -eq "mvvm") { dotnet new uno-mvvm-page -n SampleMvvmPage }
        if ($Presentation -eq "mvux") { dotnet new uno-mvux-page -n SampleMvuxPage }

        dotnet build -f net10.0-desktop
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $Name" }
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
