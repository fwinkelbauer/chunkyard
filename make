#!/usr/bin/env bash

set -euo pipefail

dotnet run --project 'src/Chunkyard.Make/Chunkyard.Make.csproj' -- "$@"
