# Architecture

This document should provide an overview of how Chunkyard is built.

## Concepts

- **Content:** A piece of data
  - **Blob:** Binary data (e.g. the content of a file)
  - **Document:** A serialized C# object
- **Repository:** The place where Chunkyard persists data. A repository contains
  two storage systems:
  - A content defined storage to store random content
  - A log storage to store ordered documents
- **ContentReference:** A reference which can be used to retrieve content from a
  Chunkyard repository. This can be compared to the key in a key/value store
  - **BlobReference:** Contains additional meta data such as a file name
  - **DocumentReference:** Contains no additional meta data
- **Snapshot:** A set of BlobReferences. This can be seen as a snapshot of the
  file system at a given point in time
- **LogReference:** A reference to a DocumentReference

## Main Components

These classes contain the most important logic:

- **Commands.cs:** Defines all verbs of the command line interface
- **SnapshotStore.cs:** Provides a set of operations to work with snapshots
  (e.g. create new snapshots, validate a snapshot or restore files from a
  snapshot)
- **ContentStore.cs:** Encrypts and deduplicates content and stores it in a
  Chunkyard repository

## Basic Backup Workflow

- Take a set of files
- Split files into encrypted chunks, store them in a repository and return a
  list of BlobReferences
- Bundle all BlobReferences into a Snapshot and store this Snapshot as a
  document (which creates a DocumentReference)
- Store a LogReference which points to the DocumentReference of the created snapshot

## Basic Restore Workflow

- Read a LogReference
- Retrieve a Snapshot using the DocumentReference found in the LogReference
- Retrieve blobs using the BlobReferences of the given Snapshot
- Decrypt and assembly all files using their BlobReferences
