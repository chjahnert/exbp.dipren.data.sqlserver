
--
-- This script removes the database objects installed by Dipren.
--

DROP TABLE IF EXISTS [dipren].[progress];
DROP TABLE IF EXISTS [dipren].[partitions];
DROP TABLE IF EXISTS [dipren].[jobs];
GO

DROP SCHEMA IF EXISTS [dipren];
GO
