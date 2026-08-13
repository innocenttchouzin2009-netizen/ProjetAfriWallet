Write-Host ""
Write-Host "AfriWallet Operations Platform"
Write-Host ""

dotnet build `
backend/src/OperationsPlatform/Production/Operations.Production.csproj `
-c Release

dotnet run `
--project backend/src/OperationsPlatform/Production/Operations.Production.csproj
