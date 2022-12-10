
--
-- This script creates the database objects required by Dipren.
--

CREATE SCHEMA [dipren];
GO

CREATE TABLE [dipren].[jobs]
(
  [id] VARCHAR(256) NOT NULL,
  [created] DATETIME NOT NULL,
  [updated] DATETIME NOT NULL,
  [batch_size] INTEGER NOT NULL,
  [timeout] BIGINT NOT NULL,
  [clock_drift] BIGINT NOT NULL,
  [started] DATETIME NULL,
  [completed] DATETIME NULL,
  [state] VARCHAR(16) NOT NULL,
  [error] TEXT NULL,
  
  CONSTRAINT [pk_jobs] PRIMARY KEY ([id])
);
GO


CREATE TABLE [dipren].[partitions]
(
  [id] CHAR(36) NOT NULL,
  [job_id] VARCHAR(256) NOT NULL,
  [created] DATETIME NOT NULL,
  [updated] DATETIME NOT NULL,
  [owner] VARCHAR(256) NULL,
  [acquired] INTEGER NOT NULL DEFAULT (0),
  [first] TEXT NOT NULL,
  [last] TEXT NOT NULL,
  [is_inclusive] BIT NOT NULL,
  [position] TEXT NULL,
  [processed] BIGINT NOT NULL,
  [remaining] BIGINT NOT NULL,
  [throughput] DOUBLE PRECISION NOT NULL,
  [is_completed] BIT NOT NULL,
  [is_split_requested] BIT NOT NULL,

  CONSTRAINT [pk_partitions] PRIMARY KEY ([id]),
  CONSTRAINT [fk_partitions_to_job] FOREIGN KEY ([job_id]) REFERENCES [dipren].[jobs]([id]) ON UPDATE NO ACTION ON DELETE CASCADE
);
GO

CREATE INDEX [ix_partitions_by_job_id] ON [dipren].[partitions] ([job_id]);
GO
