# Hive5
Set data path via env: HIVE_BASE_PATH


# List pakker
dotnet list package

# EF core
setx PATH "%PATH%;%USERPROFILE%\.dotnet\tools"


dotnet tool install --global dotnet-ef

dotnet ef migrations add addEventsTable

