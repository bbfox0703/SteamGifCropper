@echo off
setlocal

REM Force a fully fresh publish so it never uses stale compiled data.
REM Mirrors an IDE "Clean": delete the previous publish output plus the project's
REM bin/obj, then publish (which re-restores and fully recompiles from scratch).
if exist publish rmdir /s /q publish
if exist obj rmdir /s /q obj
if exist bin rmdir /s /q bin

REM -p:DebugType=none -p:DebugSymbols=false suppresses the .pdb so it is neither emitted nor
REM shipped in the publish output (overrides the csproj Release "pdbonly" default).
dotnet publish SteamGifCropper.csproj -c Release -r win-x64 --self-contained false -p:DebugType=none -p:DebugSymbols=false -o publish
if errorlevel 1 (
    echo.
    echo Publish FAILED.
    exit /b 1
)

echo.
echo Published to .\publish\