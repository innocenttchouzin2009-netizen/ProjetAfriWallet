$ErrorActionPreference='Stop';Write-Host 'Compliance architecture boundary verification'
$required=@('backend/src/CompliancePlatform/ComplianceProfile.Domain','backend/src/Compliance/IdentityVerification.Domain','backend/src/Compliance/Screening.Domain','backend/src/Compliance/TransactionMonitoring.Domain','backend/src/Compliance/RiskScoring.Domain','backend/src/Compliance/CaseManagement.Domain')
foreach($path in $required){if(!(Test-Path $path)){throw "Missing boundary: $path"};Write-Host "$path PASS"}
$forbidden=@('api.ofac.gov','production-worldcheck','production-dowjones');$files=Get-ChildItem backend/src/Compliance -Recurse -File|Where-Object{$_.FullName -notmatch '\\(bin|obj)\\'}
foreach($token in $forbidden){if($files|Select-String -Pattern $token -SimpleMatch -ErrorAction SilentlyContinue){throw "Forbidden production integration detected: $token"}}
Write-Host 'Compliance architecture boundaries PASS'