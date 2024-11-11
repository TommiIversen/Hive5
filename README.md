# Hive5
Set data path via env: HIVE_BASE_PATH


# List pakker
dotnet list package

# EF core
setx PATH "%PATH%;%USERPROFILE%\.dotnet\tools"


dotnet tool install --global dotnet-ef

dotnet ef migrations add workerChangeLog


# Playwright
Install
NB: Kræver PT .NET SDK 8 (resten af projektet kører på .NET 9)
dotnet tool update --global PowerShell
dotnet add package Microsoft.Playwright.MSTest
pwsh bin/Debug/net9.0/playwright.ps1 install


Kør:
Start eng engine + en streamhub (kører på port 9080)
dotnet test

