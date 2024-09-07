try {
    Push-Location $PSScriptRoot
    dotnet run --project 'src/Publish/Publish.csproj' -- $args
}
finally {
    Pop-Location
}

exit $LASTEXITCODE
