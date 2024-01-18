dotnet run --project 'src/Chunkyard.Build/Chunkyard.Build.csproj' -- $args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
