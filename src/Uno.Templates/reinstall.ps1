param(
    # Version of published Uno.Templates packages
    [string]$TemplatesVersion = "255.255.255.255",

    # Version of published Uno.Extensions packages
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
    [string]$ExtensionsVersion = "2.5.0-dev.148",
=======
    [string]$ExtensionsVersion = "3.0.0-dev.2178",
>>>>>>> 3c9bb56 (chore: Bumping extensions version number for maui fixes)
=======
    [string]$ExtensionsVersion = "3.0.0-dev.2180",
>>>>>>> 231f0fe (chore(deps): Bump extensions version)
=======
    [string]$ExtensionsVersion = "3.0.0-dev.2192",
>>>>>>> e06a5e0 (feat: Update template)

    # Version of published Uno.WinUI packages
    [string]$UnoVersion = "4.9.20"
)

function RemoveNuGetPackage {
    param (
        [string]$PackageName
    )
    
    $installed = (dotnet new uninstall) -join ","
    if ($installed -like "*$PackageName*") {
        dotnet new uninstall $PackageName
    }
}

RemoveNuGetPackage -PackageName "Uno.Templates"

# Ensure legacy pacakges are removed
RemoveNuGetPackage -PackageName "Uno.Extensions.Templates"
RemoveNuGetPackage -PackageName "Uno.ProjectTemplates.Dotnet"

# Remove artifacts from previous builds
Get-ChildItem .\ -Include bin,obj -Recurse | ForEach-Object ($_) { Remove-Item $_.Fullname -Force -Recurse }

dotnet build -p:Version=$TemplatesVersion -p:UnoVersion=$UnoVersion -p:UnoExtensionsVersion=$ExtensionsVersion -c Release
if($LASTEXITCODE -ne 0) {
    Write-Error "Building NuGet Package failed."
    exit $LASTEXITCODE
}

$nugetPath = Join-Path -Path $PSScriptRoot -ChildPath 'bin'
$nugetPath = Join-Path -Path $nugetPath -ChildPath 'Release'
$nugetPath = Join-Path -Path $nugetPath -ChildPath ("Uno.Templates.$TemplatesVersion.nupkg")
dotnet new install $nugetPath

