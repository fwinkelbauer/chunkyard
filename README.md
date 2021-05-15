# Chunkyard

Chunkyard is a backup application for Windows and Linux which stores files in a
content addressable storage with support for dynamic chunking and encryption.

The FastCdc chunking algorithm is a C# port of these libraries:

- [fastcdc-rs][fastcdc-rs]
- [fastcdc-py][fastcdc-py]

I built Chunkyard for myself. You might want to consider more sophisticated
tools. Here's a list of [options][backup-tools].

**Note:** A backup operation will fail if the target backup medium runs out of
space. The repository might contain unreferenced files as the snapshot
information will only be written after a successful backup operation. These
unreferenced files can be deleted using the `chunkyard gc` command.

## Goals

- Cross platform support
- Favor simplicity and readability over features and performance
- Strong symmetric encryption (AES Galois/Counter Mode using a 256 bit key)
- Ability to copy from/to other repositories
- Verifiable backups
- Minimal NuGet dependencies. The Chunkyard binary only utilizes the
  [commandlineparser][commandlineparser] package to create its command line
  interface

## Not Goals

- Key management
- Asymmetric encryption
- Compression
- Extended file system features such as OS specific flags or link
- Extended version control features such as branching or tagging
- Obfuscating/Hiding chunk sizes to prevent [CDC fingerprint attacks][borg]
- Concurrent operations on a single repository using more than one Chunkyard
  process (e.g. creating a new backup while garbage collecting unused data)

## Build

Run `./csake setup` to install all necessary dotnet tools globally.

Run `./csake build` to build the solution.

## Publish

- Commit all your work
- Update `CHANGELOG.md` and add a new version header. You do not have to create
  a commit for this change
- Run `./csake release` to create and push a tagged commit containing the latest
  version found in `CHANGELOG.md`
- Run `./csake publish` to create a binary in the `./artifacts` directory

## Usage

Type `chunkyard --help` to see a list of all available commands. You can add
`--help` to any command to get more information on its usage.

Example:

``` shell
# List all available commands
chunkyard --help

# Learn more about the preview command
chunkyard preview --help

# See which files chunkyard would backup
chunkyard preview -f "~/Music" -e "Desktop\.ini" "thumbs\.db"

# Create a backup
chunkyard create -r "../repository" -f "~/Music" -e "Desktop\.ini" "thumbs\.db"

# Check if the backup is uncorrupted
chunkyard check -r "../repository"

# Restore parts of the backup
chunkyard restore -r "../repository" -d . -i "mp3$"
```

Here's an example of a bash script which can be used in an automated process:

``` shell
set -euo pipefail

repo=/backup/location

directories=(
    ~/Music
    ~/Pictures
    ~/Videos
)

# Optional: Prevent password prompts
# export CHUNKYARD_PASSWORD="my secret password"

# Create backup
chunkyard create -r "$repo" -f ${directories[*]}

# Keep the latest four backups
chunkyard keep -r "$repo" --latest 4
chunkyard gc -r "$repo"
```

And here is the same script written in PowerShell:

``` powershell
function exec {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$ScriptBlock,
        [int[]]$ValidExitCodes = @(0)
    )

    $global:LASTEXITCODE = 0

    & $ScriptBlock

    if (-not ($global:LASTEXITCODE -in $ValidExitCodes)) {
        throw "Invalid exit code: $($global:LASTEXITCODE)"
    }
}

$repo = 'D:\backup\location'

$directories = @(
    "$env:UserProfile\Music",
    "$env:UserProfile\Pictures",
    "$env:UserProfile\Videos"
)

# Optional: Prevent password prompts
# $env:CHUNKYARD_PASSWORD = 'my secret password'

# Create backup
exec { chunkyard create --repository $repo --files $directories }

# Keep the latest four backups
exec { chunkyard keep --repository $repo --latest 4 }
exec { chunkyard gc --repository $repo }
```

[fastcdc-rs]: https://github.com/nlfiedler/fastcdc-rs
[fastcdc-py]: https://github.com/titusz/fastcdc-py
[backup-tools]: https://github.com/restic/others
[commandlineparser]: https://www.nuget.org/packages/CommandLineParser
[borg]: https://borgbackup.readthedocs.io/en/stable/internals/security.html#fingerprinting
