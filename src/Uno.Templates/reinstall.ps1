﻿param(
    # Version of published Uno.Extensions packages
    [string]$Version = "255.255.255.255"
)
dotnet new uninstall Uno.Extensions.Templates

# Remove artifacts from previous builds
Get-ChildItem .\ -Include bin,obj -Recurse | ForEach-Object ($_) { Remove-Item $_.Fullname -Force -Recurse }

dotnet build -p:Version=$Version -c Release
if($LASTEXITCODE -ne 0) {
    Write-Error "Building NuGet Package failed."
    exit $LASTEXITCODE
}
dotnet new install $PSScriptRoot\bin\Uno.Extensions.Templates\Release\Uno.Extensions.Templates.$Version.nupkg
