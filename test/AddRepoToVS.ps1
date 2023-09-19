$PackageRepoLoc = Read-Host -Prompt 'Input location of package repository -- leave blank if want standard CSO location' 
if (!$PackageRepoLoc) { $PackageRepoLoc =  'https://nuget.pkg.github.com/CSOIreland/index.json' }
Write-Host $PackageRepoLoc
$githubUsername = Read-Host -Prompt 'Enter github username'
$apiKey = Read-Host -Prompt 'Input your api key'

dotnet nuget add source --username $githubUsername --password $apiKey --name github $PackageRepoLoc

pause
