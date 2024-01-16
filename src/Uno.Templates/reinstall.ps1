param(
    # Version of published Uno.Templates packages
    [string]$TemplatesVersion = "255.255.255.255",

    # Version of published Uno.Extensions packages
    [string]$ExtensionsVersion = "4.0.0-dev.212",

    # Version of published Uno.WinUI packages
    [string]$UnoVersion = "5.1.0-dev.768"
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

