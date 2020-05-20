dotnet run --project 'src/Chunkyard.Build' -- $args

if ($LASTEXITCODE -ne 0) {
    throw 'Build failure'
}
