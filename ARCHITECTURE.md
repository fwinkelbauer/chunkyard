# Architecture

This document should provide an overview of how Chunkyard is built.

## Concepts

- **Blob:** Binary data (e.g. the content of a file) with some meta data
- **Snapshot:** A set of BlobReferences. This can be seen as a snapshot of a set
  of Blobs at a given point in time
- **Repository:** A key/value store which Chunkyard uses to persist data
- **Content:** An encrypted piece (chunk) of a Blob or a Snapshot
- **IContentReference:** A reference which can be used to retrieve content from
  an IRepository using a set of hashes
  - **BlobReference:** Contains meta data which can be used to restore a Blob
  - **SnapshotReference:** Contains meta data which can be used to restore a Snapshot

## Main Components

These classes contain the most important logic:

- **IRepository.cs:** Provides a key/value store
- **SnapshotStore.cs:** Chunks, encrypts and deduplicates Blobs and their meta
  data in an IRepository using a set of URI hash keys
- **Commands.cs:** Defines all verbs of the command line interface

## Backup Workflow

- Take a set of files
- Split files into encrypted chunks, store them and return a list of
  BlobReferences
- Bundle all BlobReferences into a Snapshot, store this Snapshot as encrypted
  chunks and return a SnapshotReference

## Restore Workflow

- Retrieve a Snapshot using a SnapshotReference
- Retrieve, decrypt and assemble all files using their BlobReferences of the
  given Snapshot
