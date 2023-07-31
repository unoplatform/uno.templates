param(
    # Version of published Uno.Templates packages
    [string]$TemplatesVersion = "255.255.255.255",

    # Version of published Uno.Extensions packages
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
    [string]$ExtensionsVersion = "2.5.0-dev.148",
=======
    [string]$ExtensionsVersion = "3.0.0-dev.2083",
>>>>>>> 00454d2 (chore: Adding maui library reference)

    # Version of published Uno.WinUI packages
    [string]$UnoVersion = "4.9.20"
=======
    [string]$ExtensionsVersion = "3.0.0-dev.2102",
=======
    [string]$ExtensionsVersion = "3.0.0-dev.2112",
>>>>>>> 8cdb088 (chore: Fixing build errors)

    # Version of published Uno.WinUI packages
    [string]$UnoVersion = "5.0.0-dev.1732"
>>>>>>> fe9e68d (feat: Adding Maui content to app)
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

$nugetPath = Join-Path -Path $PSScriptRoot -ChildPath 'bin' -AdditionalChildPath @('Release',"Uno.Templates.$TemplatesVersion.nupkg") 
dotnet new install $nugetPath

