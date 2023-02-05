dotnet run --project 'src/Chunkyard.Make/Chunkyard.Make.csproj' -- $args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
