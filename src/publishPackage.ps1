$PackageRepoLoc = Read-Host -Prompt 'Input location of package repository -- leave blank if want standard CSO location' 
if (!$PackageRepoLoc) { $PackageRepoLoc =  'https://nuget.pkg.github.com/CSOIreland/index.json' }
Write-Host $PackageRepoLoc

$ReleaseType = Read-Host -Prompt 'Enter package release type e.g. Debug\Release i.e the folder where the package is'
$PackageName = Read-Host -Prompt 'Enter package name'

$apiKey = Read-Host -Prompt 'Input your api key'


$curDir = Get-Location

Set-Location $curDir\API.Library\bin\$ReleaseType
$curDir1 = Get-Location


dotnet nuget push $PackageName  --api-key $apiKey --source "https://nuget.pkg.github.com/CSOIreland/index.json"

pause
