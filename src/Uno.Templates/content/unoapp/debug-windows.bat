@echo off

echo Setting target framework
Set OverrideTargetFrameworks=$baseTargetFramework$;$baseTargetFramework$-windows10.0.19041.0

echo Cleaning up obj and bin folders
for /f %%i in ('dir /s /b obj') do ( rmdir /s /q "%%i" )
for /f %%i in ('dir /s /b bin') do ( rmdir /s /q "%%i" )

echo Restoring packages
dotnet restore MyExtensionsApp._1-windows.slnf -v q

if (%1) == () (
    echo No arguments passed, launching solution
    MyExtensionsApp._1-windows.slnf
) 