### Microsoft SQL Srverver Engine Data Store for Dipren

This package contains a Microsoft SQL Server Engine Data Store for **DIPREN**. This implementation is suitable for processing clusters with hundreds or thousands of processing nodes.

The SQL script for creating the required database objects is included in the package. The script creates the `dipren` schema which contains all object required by this component. Use your favorite database management tool to run the script.

Pass the connection string to the constructor of the `SqlServerEngineDataStore` type when the distributed processing job is defined.