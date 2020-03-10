# Chunkyard

An experimental application (Windows and Linux) for archiving files in a content
addressable storage with support for dynamic chunking and encryption.

## Build

``` powershell
cd rust
cargo build
cd ../csharp
dotnet build
```

The `chunker` binary (created by the Rust source code) must be on the path in
order for the `chunkyard` binary to work.

## Concepts

- **Content Reference:** A description of how a file is archived in the
  underlying storage
- **Content URI:** The address of a single piece of a file (which might be
  compressed, encrypted, etc.). Example:
  `sha256://0ec7f158103de762a32f215490298c6bc47578f511955795df7d1a2a07343e3b`
- **Reference Log:** A structure to store content references in an append-only
  log
- **Reference Log URI:** The address of a reference in a log. Example:
  `log://master` (the latest element), `log://master?id=0` (the first element),
  `log://master?id=-1` (the second latest element)

## Usage

Type `chunkyard --help` to learn more.
