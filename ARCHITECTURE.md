# Architecture

This document should provide an overview of how Chunkyard is built.

## Concepts

- **Blob:** Binary data (e.g. the content of a file) with some meta data
- **Snapshot:** A set of BlobReferences. It describes the current state of a set
  of Blobs at a specific point in time
- **Repository:** A key/value store which Chunkyard uses to persist data
- **Chunk:** An encrypted piece of a Blob or a Snapshot
- **Chunk ID:** A hash address which can be used to retrieve Chunks from an
  IRepository
- **BlobReference:** Contains Chunk IDs and meta data which can be used to
  restore a Blob
- **LogReference:** Contains Chunk IDs and meta data which can be used to
  restore a Snapshot

## Main Components

These classes contain the most important logic:

- **IRepository.cs:** Provides a key/value store
- **IBlobSystem.cs:** Provides an abstraction to read and write Blobs
- **SnapshotStore.cs:** Chunks, encrypts, deduplicates and stores Blobs in an
  IRepository
- **Commands.cs:** Defines all verbs of the command line interface

## Backup Workflow

- Take a set of files
- Split files into encrypted chunks, store them in a repository and return a
  list of BlobReferences
- Bundle all BlobReferences into a Snapshot, store this Snapshot as encrypted
  chunks and return a LogReference

## Restore Workflow

- Retrieve a Snapshot using a LogReference
- Retrieve, decrypt and reassemble all files using their BlobReferences of the
  given Snapshot
