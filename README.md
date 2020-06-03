# Chunkyard

Chunkyard is a backup application for Windows and Linux which stores files in a
content addressable storage with support for dynamic chunking and encryption.

The FastCdc chunking algorithm is a C# port of these libraries:

- [fastcdc-rs](https://github.com/nlfiedler/fastcdc-rs)
- [fastcdc-py](https://github.com/titusz/fastcdc-py)

I built Chunkyard for myself. You might want to consider more sophisticated
tools. Here's a list of [options](https://github.com/restic/others).

## Goals

- Favor simplicity and readability over features
- Strong encryption (AES Galois/Counter Mode using a 256 bit key)
- Ability to push/pull from other repositories
- Verify-able backups

## Not Goals

- Key management
- Compression
- File meta data preservation (e.g. creation time, flags, ...)

## Build

Run any of the below build scripts:

``` shell
./build.sh

.\build.ps1
.\build.bat
```

Create a binary in `./artifacts` by running:

``` shell
./build.sh publish
```

Install the dotnet format tool to use the `fmt` command:

``` shell
dotnet tool install -g dotnet-format
./build.sh fmt
```

## Usage

Type `chunkyard --help` to learn more.

Example:

``` shell
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

# Create and check backup
chunkyard create -r "$repo" -f ${directories[*]} --cached
chunkyard check -r "$repo" --shallow

# Keep the latest four backups
chunkyard keep -r "$repo" -l 4
chunkyard gc -r "$repo"
```
