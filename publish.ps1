try {
    Push-Location $PSScriptRoot
    dotnet run --no-launch-profile --project 'src/Publish/Publish.csproj' -- $args
    if ($LASTEXITCODE -ne 0) { throw }
}
finally {
    Pop-Location
}
