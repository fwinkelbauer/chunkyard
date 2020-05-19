param(
    [string]$Target = 'build',
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64'
)

$ExecutionPolicy = 'Stop'

dotnet run --project 'src/Chunkyard.Build/Chunkyard.Build.csproj' -- -t $Target -c $Configuration -r $Runtime
