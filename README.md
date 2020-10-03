# Chunkyard

Chunkyard is a backup application for Windows and Linux which stores files in a
content addressable storage with support for dynamic chunking and encryption.

The FastCdc chunking algorithm is a C# port of these libraries:

- [fastcdc-rs](https://github.com/nlfiedler/fastcdc-rs)
- [fastcdc-py](https://github.com/titusz/fastcdc-py)

I built Chunkyard for myself. You might want to consider more sophisticated
tools. Here's a list of [options](https://github.com/restic/others).

## Goals

- Favor simplicity and readability over features and performance
- Strong encryption (AES Galois/Counter Mode using a 256 bit key)
- Ability to push/pull from other repositories
- Verifiable backups
- Minimal dependencies. Currently the Chunkyard binary utilizes two packages:
  - `commandlineparser`to create the command line interface
  - `Newtonsoft.Json` to work with JSON data until I can switch to
    `System.Text.Json`

**Warning:** Chunkyard is vulnerable to CDC fingerprinting attacks. By observing
the sizes of chunks in a repository, an attacker might be able to prove that a
specific file exists in the backup.

## Not Goals

- Key management
- Compression
- File meta data preservation (e.g. creation time, flags, ...)
- Extended features such as branching

## Build

Run any of the below build scripts to create a binary in `./artifacts`:

``` shell
./make.sh

.\make.ps1
.\make.bat
```

Install the dotnet format tool to use the `fmt` command:

``` shell
dotnet tool install -g dotnet-format
./make.sh fmt
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

# Create backup (and also run a shallow check)
chunkyard create -r "$repo" -f ${directories[*]} --cached

# Keep the latest four backups
chunkyard keep -r "$repo" -l 4
chunkyard gc -r "$repo"
```
