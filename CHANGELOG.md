# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

- A shallow check after creating a new snapshot using the `create` command

### Changed

- A few commands to be less verbose

## 1.0.0 - 2020-09-20

### Added

- A unique ID to every repository log

### Changed

- The behavior of push/pull to abort if the log IDs of the given repositories do not match

## 0.3.0 - 2020-06-02

### Changed

- The `keep` command to only operate on the latest N snapshots

### Removed

- The `--preview` parameter from the `gc` command

## 0.2.0 - 2020-05-27

### Added

- The commands `push` and `pull` to transmit snapshots
- The `keep`command to remove snapshots

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
