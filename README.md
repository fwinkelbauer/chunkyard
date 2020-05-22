# Chunkyard

An experimental backup application (Windows and Linux) for archiving files in a
content addressable storage with support for dynamic chunking and encryption.

The FastCdc chunking algorithm is a C# port of these libraries:

- [fastcdc-rs](https://github.com/nlfiedler/fastcdc-rs)
- [fastcdc-py](https://github.com/titusz/fastcdc-py)

## Build

Install the dotnet format tool:

``` shell
dotnet tool install -g dotnet-format
```

Run any of the below build scripts:

``` shell
./build.sh

.\build.ps1
.\build.bat
```

## Concepts

- **Content Reference:** A description of how a file is archived in the
  underlying storage
- **Content URI:** The address of an encrypted piece of a file. Example:
  `sha256://0ec7f158103de762a32f215490298c6bc47578f511955795df7d1a2a07343e3b`
- **Reference Log:** A structure to store references in an append-only log

## Usage

Type `chunkyard --help` to learn more.

Example:

``` shell
chunkyard create -r "../repository" -f "~/Music" -e "Desktop\.ini" "thumbs\.db"
chunkyard check -r "../repository"
```
