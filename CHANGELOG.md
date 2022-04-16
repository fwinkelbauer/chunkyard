# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog][changelog] and this project adheres to
[Semantic Versioning][semver].

## Unreleased

### Removed

- Schema information from snapshot references
- "sha256://" prefix when printing chunks

## 14.0.1 - 2022-04-03

### Fixed

- A concurrency issue when using the `store` command which was caused by an
  attempt to improve performance

## 14.0.0 - 2022-03-12

### Added

- A `--preview` flag to the commands `create` and `mirror`

### Changed

- All flags named `--content` to `--chunk`
- All flags named `--content-only` to `--chunks-only`
- The storage format by renaming `ContentUris` to `ChunkIds`
- The `create` command to `store`

### Removed

- The `preview` command

## 13.0.0 - 2022-03-07

### Added

- A new command called `mirror`, which is a combination of `restore` and
  `clean`. This command can be used to restore a snapshot in a way that the file
  system matches the exact content of a snapshot. As a result, `mirror` can
  overwrite and delete existing files

### Changed

- The `restore` command to fail if it would overwrite a file

### Removed

- The `clean` command

## 12.2.1 - 2022-02-03

### Fixed

- An error when checking for directory traversals

## 12.2.0 - 2022-01-29

### Added

- A schema information back to snapshot references. Chunkyard will again stop
  processing if a given schema version is not supported

## 12.1.0 - 2022-01-09

### Added

- The `clean` command to delete files which are not found in a snapshot

### Changed

- The `store` command to improve performance

## 12.0.0 - 2022-01-08

### Changed

- The storage format by
  - removing the ID property from snapshots
  - changing how a blob reference is structured
- The `copy` command to now require a password prompt

### Removed

- The `--mirror` flag of the `copy` command

## 11.11.2 - 2021-12-19

### Fixed

- An error when checking for directory traversals

## 11.11.1 - 2021-12-15

### Fixed

- The file persistence layer to prevent unintended directory traversal

## 11.11.0 - 2021-11-12

### Changed

- The .NET version from 5 to 6:
  - Windows releases can now be shipped using a single binary
  - The overall binary size was cut in half

## 11.10.0 - 2021-11-06

### Removed

- The `--snapshot` option from the `preview` command

## 11.9.0 - 2021-10-30

### Changed

- The `diff` command to include an `--include` option. The `--content-only` flag
  will now print the actual content URIs
- The `show` command to also include a `--content-only` option

## 11.8.0 - 2021-10-10

### Changed

- Internal parts of the architecture to improve test-ability

## 11.7.0 - 2021-10-03

### Changed

- The chunking algorithm to avoid rereading blobs from disk

## 11.6.1 - 2021-09-30

### Fixed

- A scenario in which an empty blob could be stored before a password prompt

## 11.6.0 - 2021-09-27

### Removed

- The `-c` option when using the `diff` command. `--content-only` should be used
  instead

## 11.5.0 - 2021-09-09

### Changed

- The error message when failing to restore parts of a snapshot
- The `restore` command to also update the file meta data (last write time). A
  file will be overwritten, if its meta data does not match the data found in a
  snapshot

### Removed

- The `--scan` option when using `create`
- The `--overwrite` option when using `restore` as this option is now always
  active

## 11.4.1 - 2021-08-09

### Fixed

- The order of operations when performing a `copy --mirror` command to avoid
  snapshot corruption if the operation is canceled

## 11.4.0 - 2021-08-09

### Added

- A `--mirror` flag to the `copy` command. This flag will copy newer data from a
  source repository to a destination while also deleting any files that do not
  exist in the source repository

## 11.3.0 - 2021-06-03

### Added

- A feature to retrieve a password from a shell command using the environment
  variable `CHUNKYARD_PASSCMD`

## 11.2.0 - 2021-05-30

### Added

- An optional `--snapshot` parameter to the `preview` command
- Parallelism to the `create` command when writing large blobs

## 11.1.0 - 2021-05-28

### Added

- The optional argument `--content-only` to the `diff` command to compare the
  content of two snapshots without their meta data

### Changed

- The storage format by removing an unused field from all snapshot references

## 11.0.0 - 2021-05-25

### Added

- The `cat` command to decrypt and print a particular set of content URIs

### Changed

- The `preview` command to show what a `create` would do by adding a
  `--repository` parameter

## 10.0.0 - 2021-05-20

### Changed

- The storage format by simplifying snapshot references

## 9.3.0 - 2021-05-17

### Changed

- The order of blob references in a snapshot by sorting them by name
- The storage format by removing the CreationTimeUtc property

### Fixed

- The behavior of the `restore` command when using `--overwrite`

### Removed

- Setting the LastWriteTimeUtc and CreationTimeUtc fields of restored files when
  using the `restore` command

## 9.2.1 - 2021-05-09

### Fixed

- A problem when restoring large files

## 9.2.0 - 2021-05-03

### Changed

- A compiler option when publishing Chunkyard to cut the binary size in half
- The file fetching algorithm to improve its performance

## 9.1.0 - 2021-04-17

### Changed

- The `restore` command to set the LastWriteTimeUtc and CreationTimeUtc fields
  of restored files
- The snapshot creation time to be stored in UTC

## 9.0.0 - 2021-04-15

### Added

- The `diff` command to outline changes between two snapshots

### Changed

- The `copy` command to no longer need a password prompt
- The storage format by adding snapshot IDs and renaming directories

## 8.1.0 - 2021-04-04

### Changed

- The fuzzy pattern parameter of the `check`, `restore`, `show` command into a
  collection
- The `--cached` parameter of the `create` command to a fuzzy patterns parameter
  called `--scan`. This parameter can be used inspect files without comparing
  them to the latest stored snapshot

### Fixed

- An error which prevented the `copy` command to append to an existing
  repository

### Removed

- The shallow check performed by the `create` command

## 8.0.0 - 2021-03-26

### Changed

- The `restore` command to check data before restoring it

### Removed

- The `dot` command. The shallow check is again part of the `create` command

## 7.0.0 - 2021-03-12

### Changed

- The storage format by removing cryptographic tags

### Removed

- Chunk size settings from all CLI verbs

## 6.1.0 - 2021-03-09

### Changed

- The storage format by removing the length property from blob references

### Fixed

- The `dot` command to perform a shallow instead of a full check

## 6.0.0 - 2021-03-07

### Added

- Parallelism for several read/write operations

### Changed

- The `dot` command to search for two default files (`config/chunkyard.json` and
  `.chunkyard`)
- The storage format to include more meta data

### Removed

- The external caching directory. This feature is now integrated into the
  storage format

## 5.0.0 - 2021-02-23

### Added

- The `dot` command

### Changed

- The name of the argument `log-position` to `snapshot` for all CLI verbs

### Removed

- A shallow check after creating a new snapshot using the `create` command. This
  feature is now part of the `dot` command.

## 4.0.0 - 2020-11-29

### Changed

- The storage format by
  - removing the unique repository identifier
  - adding cryptographic details to every piece of content to enable disaster
    recovery
- The `copy` command to only work if both repositories contain at least one
  overlapping snapshot

## 3.0.0 - 2020-11-16

### Changed

- The target framework to .NET 5 and reduced the binary file size
- The storage format by
  - removing the schema information from a snapshot
  - removing the ID from a log reference
  - modifying all content names around a common parent path
  - adding a unique identifier to a repository

### Fixed

- The shallow check after using the `create` command

## 2.0.0 - 2020-10-25

### Changed

- The `create` command to always write a snapshot
- Most commands to require an existing repository
- The commands `push` and `pull` by merging them into a single `copy` command
- The storage format. Chunkyard will stop processing a snapshot if the schema
  version is not supported

## 1.2.0 - 2020-10-06

### Added

- Checks when using the `push` and `pull` commands to ensure that common reflog
  items (snapshots) do not differ

### Changed

- The `list` command to display dates in the "yyyy-MM-dd HH:mm:ss" format
- The `create` command to only write a snapshot if files changed

## 1.1.1 - 2020-10-02

### Fixed

- The duplicate password prompt when using the `push` and `pull` commands

## 1.1.0 - 2020-09-30

### Added

- A shallow check after creating a new snapshot using the `create` command

### Changed

- A few commands to be less verbose

## 1.0.0 - 2020-09-20

### Added

- A unique ID to every repository log

### Changed

- The behavior of push/pull to abort if the log IDs of the given repositories do
  not match

## 0.3.0 - 2020-06-02

### Changed

- The `keep` command to only operate on the latest N snapshots

### Removed

- The `--preview` parameter from the `gc` command

## 0.2.0 - 2020-05-27

### Added

- The commands `push` and `pull` to transmit snapshots
- The `keep` command to remove snapshots

### Changed

- The names of a few command line verbs
- The `create` command to accept chunk size parameters

### Fixed

- Missing content items when using a cache

### Removed

- Branches from repositories

## 0.1.0 - 2020-05-23

### Added

- Initial release

[changelog]: http://keepachangelog.com/en/1.0.0
[semver]: http://semver.org/spec/v2.0.0.html
