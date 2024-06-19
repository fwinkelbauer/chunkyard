#!/bin/bash -eu

dotnet run --project 'src/Chunkyard.Build/Chunkyard.Build.csproj' -- "$@"
