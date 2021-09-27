$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

try {
    Push-Location $root

    $project = git ls-files '*.Build.csproj'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet run --project $project -- $args
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
