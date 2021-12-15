# Chunkyard

Chunkyard is a backup application for Windows and Linux which stores files in a
content addressable storage with support for dynamic chunking and encryption.

The [FastCDC][fastcdc] chunking algorithm is a C# port of these libraries:

- [fastcdc-rs][fastcdc-rs]
- [fastcdc-py][fastcdc-py]

I built Chunkyard for myself. You might want to consider more sophisticated
tools. Here's a list of [options][backup-tools].

## Goals

- Cross platform support
- Favor simplicity and readability over features and performance
- Strong symmetric encryption (AES Galois/Counter Mode using a 256 bit key)
- Ability to copy from/to other repositories
- Verifiable backups
- Minimal NuGet dependencies. The Chunkyard binary only utilizes the
  [commandlineparser][commandlineparser] package to create its command line
  interface

## Non-Goals

- Key management
- Asymmetric encryption
- Compression
- Extended file system features such as OS specific flags or links
- Extended "version control" features such as branching or tagging
- Obfuscating/Hiding chunk sizes to prevent [CDC fingerprint attacks][borg]
- Concurrent operations on a single repository using more than one Chunkyard
  process (e.g. creating a new backup while garbage collecting unused data)

## Development

- You can learn more about a few basic Chunkyard topics by reading the
  `ARCHITECTURE.md` file

## Build

- Install the .NET 6 SDK
- Run `./csake ci` to build and test the solution

The `./csake` script runs the project `./build/Chunkyard.Build`, a CLI tool
which can be used to build, test and publish Chunkyard. You can type commands
such as `./csake help` or `./csake build --help` to learn more.

## Publish

- Commit all your work
- Update `CHANGELOG.md` and add a new version header. Run `./csake release` to
  create a tagged commit containing the latest version found in `CHANGELOG.md`
- Run `git push --follow-tags`
- Run `./csake publish` to create Linux and Windows binaries in the
  `./artifacts` directory

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
chunkyard preview -r "../repository" -f "Music" -e "Desktop\.ini" "thumbs\.db"

# Create a backup
chunkyard create -r "../repository" -f "Music" -e "Desktop\.ini" "thumbs\.db"

# Check if the backup is valid
chunkyard check -r "../repository"

# Restore parts of the backup
chunkyard restore -r "../repository" -d . -i "mp3$"
```

Here's an example of a bash script:

``` shell
set -euo pipefail

repo=/backup/location

directories=(
    ~/Music
    ~/Pictures
    ~/Videos
)

# Optional: Prevent password prompts using one of these methods
# export CHUNKYARD_PASSWORD="my secret password"
# export CHUNKYARD_PASSCMD="echo \"my secret password\""

# Create backup
chunkyard create -r "$repo" -f ${directories[*]}

# Keep the latest four backups
chunkyard keep -r "$repo" --latest 4
chunkyard gc -r "$repo"
```

And here is the same script written in PowerShell:

``` powershell
$repo = 'D:\backup\location'

$directories = @(
    "$env:UserProfile\Music",
    "$env:UserProfile\Pictures",
    "$env:UserProfile\Videos"
)

# Optional: Prevent password prompts using one of these methods
# $env:CHUNKYARD_PASSWORD = 'my secret password'
# $env:CHUNKYARD_PASSCMD = 'echo "my secret password"'

# Create backup
chunkyard create --repository $repo --files $directories
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Keep the latest four backups
chunkyard keep --repository $repo --latest 4
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

chunkyard gc --repository $repo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

[fastcdc]: https://www.usenix.org/system/files/conference/atc16/atc16-paper-xia.pdf
[fastcdc-rs]: https://github.com/nlfiedler/fastcdc-rs
[fastcdc-py]: https://github.com/titusz/fastcdc-py
[backup-tools]: https://github.com/restic/others
[commandlineparser]: https://www.nuget.org/packages/CommandLineParser
[borg]: https://borgbackup.readthedocs.io/en/stable/internals/security.html#fingerprinting
