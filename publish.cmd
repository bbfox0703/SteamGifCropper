@echo off
dotnet publish SteamGifCropper.csproj -c Release -r win-x64 --self-contained false -o publish
echo.
echo Published to .\publish\
pause
