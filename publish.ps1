try {
    Push-Location $PSScriptRoot
    dotnet run --no-launch-profile --project 'src/Publish/Publish.csproj' -- $args
}
finally {
    Pop-Location
}

exit $LASTEXITCODE
