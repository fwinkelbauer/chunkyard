#!/usr/bin/env bash

set -euo pipefail

dotnet run --project 'src/Chunkyard.Build/Chunkyard.Build.csproj' -- "$@"
