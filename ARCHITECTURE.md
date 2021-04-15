# Architecture

This document should provide an overview of how Chunkyard is built.

## Concepts

- **Content:** A piece of data
  - **Blob:** Binary data (e.g. the content of a file)
  - **Document:** A serialized C# object
- **Repository:** A key/value store which Chunkyard uses to persists data
- **ContentReference:** A reference which can be used to retrieve content from a
  ContentStore
  - **BlobReference:** Contains additional meta data such as a file name
  - **DocumentReference:** Contains no additional meta data
- **Snapshot:** A set of BlobReferences. This can be seen as a snapshot of the
  file system at a given point in time
- **SnapshotReference:** A reference to a DocumentReference which contains a
  Snapshot

## Main Components

These classes contain the most important logic:

- **IRepository.cs:** Provides a key/value store
- **Commands.cs:** Defines all verbs of the command line interface
- **SnapshotStore.cs:** Provides a set of operations to work with snapshots
  (e.g. create new snapshots, validate a snapshot or restore files from a
  snapshot). Snapshots are stored in an IRepository using an int key
- **ContentStore.cs:** Encrypts, deduplicates and stores content in an
  IRepository using a URI key

## Basic Backup Workflow

- Take a set of files
- Split files into encrypted chunks, store them as blobs and return a list of
  BlobReferences
- Bundle all BlobReferences into a Snapshot and store this Snapshot as a
  document (which creates a DocumentReference)
- Store a SnapshotReference which points to the DocumentReference of the created
  snapshot

## Basic Restore Workflow

- Read a SnapshotReference
- Retrieve a Snapshot using the DocumentReference found in the SnapshotReference
- Retrieve blobs using the BlobReferences of the given Snapshot
- Decrypt and assemble all files using their BlobReferences
