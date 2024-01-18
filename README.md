# Chunkyard

Chunkyard is a fast backup application for Windows and Linux which stores files
in a content addressable storage with support for dynamic chunking and
encryption.

The [FastCDC][fastcdc] chunking algorithm is a C# port of the Rust crate
[fastcdc-rs][fastcdc-rs].

I built Chunkyard for myself. You might want to consider more sophisticated
tools. Here's a list of [options][backup-tools].

## Goals

- Cross platform support. Chunkyard is shipped as two binaries `chunkyard`
  (Linux) and `chunkyard.exe` (Windows) and they work without having to install
  .NET on your computer
- Favor simplicity and readability over features and elaborate performance
  tricks
- Strong symmetric encryption (AES Galois/Counter Mode using a 256 bit key)
- Ability to copy from/to other repositories
- Verifiable backups
- No third-party dependencies

## Non-Goals

- Key management
- Asymmetric encryption
- Compression
- Extended file system features such as OS specific flags or links
- Extended "version control" features such as branching or tagging
- Hiding chunk sizes to prevent [CDC fingerprint attacks][borg]
- Concurrent operations on a single repository using more than one Chunkyard
  process (e.g. creating a new backup while garbage collecting unused data)

## Development

- You can learn more about a few basic concepts by reading `ARCHITECTURE.md`

## Build

- Install the .NET SDK
- Run `./pls build` to build and test Chunkyard

`./pls` runs the project `src/Chunkyard.Build`, a CLI tool which can be used to
build, test and publish Chunkyard. You can type commands such as `./pls help` or
`./pls build --help` to learn more.

## Publish

- Commit all your work
- Update `CHANGELOG.md` and add a new version header. Run `./pls release` to
  create a tagged commit containing the latest version found in `CHANGELOG.md`
- Run `git push --follow-tags`
- Run `./pls publish` to create Linux and Windows binaries in the `./artifacts`
  directory

## Usage

Type `chunkyard help` to see a list of all available commands. You can add
`--help` to any command to get more information about what parameters it
expects.

Example:

``` shell
# List all available commands
chunkyard help

# Learn more about the store command
chunkyard store --help

# See which files chunkyard would backup
chunkyard store --repository '../repository' --paths 'Music' --include '!Desktop\.ini' '!thumbs\.db' --preview

# Store a backup
chunkyard store --repository '../repository' --paths 'Music' --include '!Desktop\.ini' '!thumbs\.db'

# Check if the latest backup is valid
chunkyard check --repository '../repository'

# Restore parts of the latest backup
chunkyard restore --repository '../repository' --destination . --include 'mp3$'
```

Here's an example of a bash script:

``` shell
#!/bin/bash

set -euo pipefail

repo=/backup/location

directories=(
    ~/Music
    ~/Pictures
    ~/Videos
)

# Optional: Show error traces
# export DEBUG='1'

# Store backup
chunkyard store --repository "$repo" --paths "${directories[@]}"

# Optional: Prevent password prompts and use more threads
# export CHUNKYARD_PASSWORD='my secret password'
# chunkyard store --repository "$repo" --paths "${directories[@]}" --prompt Environment --parallel 2

# Keep the latest four backups
chunkyard keep --repository "$repo" --latest 4
chunkyard gc --repository "$repo"
```

And here is the same script written in PowerShell:

``` powershell
$repo = 'D:\backup\location'

$directories = @(
    "$env:UserProfile\Music",
    "$env:UserProfile\Pictures",
    "$env:UserProfile\Videos"
)

# Optional: Show error traces
# $env:DEBUG = '1'

# Store backup
chunkyard store --repository $repo --paths $directories
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Optional: Prevent password prompts and use more threads
# $env:CHUNKYARD_PASSWORD = 'my secret password'
# chunkyard store --repository $repo --paths $directories --prompt Environment --parallel 2
# if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Keep the latest four backups
chunkyard keep --repository $repo --latest 4
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

chunkyard gc --repository $repo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

[fastcdc]: https://www.usenix.org/system/files/conference/atc16/atc16-paper-xia.pdf
[fastcdc-rs]: https://github.com/nlfiedler/fastcdc-rs
[backup-tools]: https://github.com/restic/others
[borg]: https://borgbackup.readthedocs.io/en/stable/internals/security.html#fingerprinting
