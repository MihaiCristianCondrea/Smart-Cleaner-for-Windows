$ErrorActionPreference = "Stop"
$proj = "src/EmptyFolderCleaner.WinUI/EmptyFolderCleaner.WinUI.csproj"
dotnet restore $proj
dotnet publish $proj -c Release -p:PublishProfile=Win-x64-SelfContained
$pub = Get-ChildItem "src/EmptyFolderCleaner.WinUI/bin/Release/*/win-x64/publish" -Directory | Select-Object -Last 1
Compress-Archive -Force -Path "$($pub.FullName)\*" -DestinationPath "$($pub.FullName).zip"
Write-Host "Published to: $($pub.FullName)"
Write-Host "ZIP: $($pub.FullName).zip"
