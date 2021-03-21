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
- Minimal dependencies. The Chunkyard binary only utilizes the
  `commandlineparser` package to create its command line interface

## Not Goals

- Key management
- Asymmetric encryption
- Compression
- File meta data preservation (e.g. creation time, flags, ...)
- Extended features such as branching or tagging
- Obfuscating/Hiding chunk sizes to prevent [CDC fingerprint attacks][borg]
- Concurrent operations on a single repository using more than one Chunkyard
  process (e.g. creating a new backup while garbage collecting unused data)

## Build

Run `./csake setup` to install all necessary dotnet tools globally.

Run `./csake build` to build the solution.

## Publish

- Update `CHANGELOG.md` and add a new version header
- Run `./csake release` to create a tagged commit containing the latest
  version found in `CHANGELOG.md`
- Run `git push --follow-tags`
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

export CHUNKYARD_PASSWORD="my secret password"

# Create backup (and also run a shallow check)
chunkyard create -r "$repo" -f ${directories[*]} --cached

# Keep the latest four backups
chunkyard keep -r "$repo" --latest 4
chunkyard gc -r "$repo"
```

[fastcdc-rs]: https://github.com/nlfiedler/fastcdc-rs
[fastcdc-py]: https://github.com/titusz/fastcdc-py
[backup-tools]: https://github.com/restic/others
[borg]: https://borgbackup.readthedocs.io/en/stable/internals/security.html#fingerprinting
