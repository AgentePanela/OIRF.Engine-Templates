@echo off
cls

echo ==============================
echo      Select one export option
echo ==============================
echo 1 - Windows x64
echo 2 - Linux x64
echo 3 - MacOS osx-x64
echo 4 - MacOS osx-arm64
echo ==============================

set /p c=Choose: 

set PROJECT=MyGame\MyGame.csproj
set DIR=Publish

if "%c%"=="1" (
    dotnet publish %PROJECT% -c Release -r win-x64 -o %DIR%\win-x64 --self-contained -p:PublishReadyToRun=false -p:TieredCompilation=false
) else if "%c%"=="2" (
    dotnet publish %PROJECT% -c Release -r linux-x64 -o %DIR%\linux-x64 --self-contained -p:PublishReadyToRun=false -p:TieredCompilation=false
) else if "%c%"=="3" (
    dotnet publish %PROJECT% -c Release -r osx-x64 -o %DIR%\osx-x64 --self-contained -p:PublishReadyToRun=false -p:TieredCompilation=false
) else if "%c%"=="4" (
    dotnet publish %PROJECT% -c Release -r osx-arm64 -o %DIR%\osx-arm64 --self-contained -p:PublishReadyToRun=false -p:TieredCompilation=false
) else (
    echo Invalid option!
)

pause