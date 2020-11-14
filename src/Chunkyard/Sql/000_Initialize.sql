CREATE TABLE Content
(
  ContentId          INTEGER       PRIMARY KEY AUTOINCREMENT,
  ContentUri         TEXT          UNIQUE,
  Value              BLOB          NOT NULL
);

CREATE UNIQUE INDEX Content_ContentUri
ON Content(ContentUri);

CREATE TABLE Reflog
(
  LogId              INTEGER       PRIMARY KEY AUTOINCREMENT,
  Value              BLOB          NOT NULL
);

CREATE TABLE Repository
(
  RepositoryId       INTEGER       PRIMARY KEY,
  Guid               TEXT          NOT NULL
);
