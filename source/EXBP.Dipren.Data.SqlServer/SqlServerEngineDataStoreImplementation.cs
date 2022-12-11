﻿
using System.Data;
using System.Data.Common;
using System.Diagnostics;

using EXBP.Dipren.Diagnostics;

using Microsoft.Data.SqlClient;


namespace EXBP.Dipren.Data.SqlServer
{
    /// <summary>
    ///   Implements an <see cref="IEngineDataStore"/> that uses Microsoft SQL Server as its storage engine.
    /// </summary>
    /// <remarks>
    ///   The implementation assumes that the required schema and table structure is already deployed.
    /// </remarks>
    internal class SqlServerEngineDataStoreImplementation : EngineDataStore, IEngineDataStore
    {
        private const int SQL_ERROR_PRIMARY_KEY_VIOLATION = 2627;
        private const int SQL_ERROR_FOREIGN_KEY_VIOLATION = 547;


        private readonly string _connectionString;


        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlServerEngineDataStoreImplementation"/> class.
        /// </summary>
        /// <param name="connectionString">
        ///   The connection string to use when connecting to the database.
        /// </param>
        internal SqlServerEngineDataStoreImplementation(string connectionString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            this._connectionString = builder.ToString();
        }


        public Task<long> CountIncompletePartitionsAsync(string id, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Returns the number of distributed processing jobs in the current data store.
        /// </summary>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> or <see cref="long"/> that represents the asynchronous operation and can
        ///   be used to access the result.
        /// </returns>
        public async Task<long> CountJobsAsync(CancellationToken cancellation)
        {
            long result = 0L;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryCountJobs;
                command.CommandType = CommandType.Text;

                result = (long) await command.ExecuteScalarAsync(cancellation);
            }

            return result;
        }

        /// <summary>
        ///   Inserts a new job entry into the data store.
        /// </summary>
        /// <param name="job">
        ///   The job entry to insert.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> object that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Argument <paramref name="job"/> is a <see langword="null"/> reference.
        /// </exception>
        /// <exception cref="DuplicateIdentifierException">
        ///   A job with the specified unique identifier already exists in the data store.
        /// </exception>
        public async Task InsertJobAsync(Job job, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(job, nameof(job));

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryInsertJob;
                command.CommandType = System.Data.CommandType.Text;

                DateTime uktsCreated = DateTime.SpecifyKind(job.Created, DateTimeKind.Unspecified);
                DateTime uktsUpdated = DateTime.SpecifyKind(job.Updated, DateTimeKind.Unspecified);
                string stateName = this.ToJobStateName(job.State);
                object uktsStarted = ((job.Started != null) ? DateTime.SpecifyKind(job.Started.Value, DateTimeKind.Unspecified) : DBNull.Value);
                object uktsCompleted = ((job.Completed != null) ? DateTime.SpecifyKind(job.Completed.Value, DateTimeKind.Unspecified) : DBNull.Value);
                object error = ((job.Error != null) ? job.Error : DBNull.Value);

                command.Parameters.AddWithValue("@id", job.Id);
                command.Parameters.AddWithValue("@created", uktsCreated);
                command.Parameters.AddWithValue("@updated", uktsUpdated);
                command.Parameters.AddWithValue("@batch_size", job.BatchSize);
                command.Parameters.AddWithValue("@timeout", job.Timeout.Ticks);
                command.Parameters.AddWithValue("@clock_drift", job.ClockDrift.Ticks);
                command.Parameters.AddWithValue("@started", uktsStarted);
                command.Parameters.AddWithValue("@completed", uktsCompleted);
                command.Parameters.AddWithValue("@state", stateName);
                command.Parameters.AddWithValue("@error", error);

                try
                {
                    await command.ExecuteNonQueryAsync(cancellation);
                }
                catch (SqlException ex) when (ex.Number == SQL_ERROR_PRIMARY_KEY_VIOLATION)
                {
                    this.RaiseErrorDuplicateJobIdentifier(ex);
                }
            }
        }

        /// <summary>
        ///   Inserts a new partition entry into the data store.
        /// </summary>
        /// <param name="partition">
        ///   The new partition entry to insert.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> object that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="DuplicateIdentifierException">
        ///   A partition with the specified unique identifier already exists in the data store.
        /// </exception>
        /// <exception cref="InvalidReferenceException">
        ///   The job referenced by the partition does not exist within the data store.
        /// </exception>
        public async Task InsertPartitionAsync(Partition partition, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(partition, nameof(partition));

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await this.InsertPartitionAsync(connection, null, partition, cancellation);
            }
        }

        /// <summary>
        ///   Inserts a new partition entry into the data store.
        /// </summary>
        /// <param name="connection">
        ///   The open database connection to use.
        /// </param>
        /// <param name="transaction">
        ///   The active transaction to use.
        /// </param>
        /// <param name="partition">
        ///   The new partition entry to insert.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> object that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="DuplicateIdentifierException">
        ///   A partition with the specified unique identifier already exists in the data store.
        /// </exception>
        /// <exception cref="InvalidReferenceException">
        ///   The job referenced by the partition does not exist within the data store.
        /// </exception>
        private async Task InsertPartitionAsync(SqlConnection connection, SqlTransaction transaction, Partition partition, CancellationToken cancellation)
        {
            Debug.Assert(connection != null);
            Debug.Assert(partition != null);

            await using SqlCommand command = new SqlCommand
            {
                CommandText = SqlServerEngineDataStoreImplementationResources.QueryInsertPartition,
                CommandType = CommandType.Text,
                Connection = connection,
                Transaction = transaction
            };

            string id = partition.Id.ToString("d");
            DateTime uktsCreated = DateTime.SpecifyKind(partition.Created, DateTimeKind.Unspecified);
            DateTime uktsUpdated = DateTime.SpecifyKind(partition.Updated, DateTimeKind.Unspecified);

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@job_id", partition.JobId);
            command.Parameters.AddWithValue("@created", uktsCreated);
            command.Parameters.AddWithValue("@updated", uktsUpdated);
            command.Parameters.AddWithValue("@owner", ((object) partition.Owner) ?? DBNull.Value);
            command.Parameters.AddWithValue("@first", partition.First);
            command.Parameters.AddWithValue("@last", partition.Last);
            command.Parameters.AddWithValue("@is_inclusive", partition.IsInclusive);
            command.Parameters.AddWithValue("@position", ((object) partition.Position) ?? DBNull.Value);
            command.Parameters.AddWithValue("@processed", partition.Processed);
            command.Parameters.AddWithValue("@remaining", partition.Remaining);
            command.Parameters.AddWithValue("@throughput", partition.Throughput);
            command.Parameters.AddWithValue("@is_completed", partition.IsCompleted);
            command.Parameters.AddWithValue("@is_split_requested", partition.IsSplitRequested);

            try
            {
                await command.ExecuteNonQueryAsync(cancellation);
            }
            catch (SqlException ex) when (ex.Number == SQL_ERROR_FOREIGN_KEY_VIOLATION)
            {
                this.RaiseErrorInvalidJobReference();
            }
            catch (SqlException ex) when (ex.Number == SQL_ERROR_PRIMARY_KEY_VIOLATION)
            {
                this.RaiseErrorDuplicatePartitionIdentifier(ex);
            }
        }

        public Task InsertSplitPartitionAsync(Partition partitionToUpdate, Partition partitionToInsert, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Marks a job as completed.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the job to update.
        /// </param>
        /// <param name="timestamp">
        ///   The current date and time value.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="Job"/> object that represents the asynchronous operation and
        ///   provides access to the result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Argument <paramref name="id"/> is a <see langword="null"/> reference.
        /// </exception>
        /// <exception cref="UnknownIdentifierException">
        ///   A job with the specified unique identifier does not exist in the data store.
        /// </exception>
        public async Task<Job> MarkJobAsCompletedAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(id, nameof(id));

            Job result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryMarkJobAsCompleted;
                command.CommandType = CommandType.Text;

                DateTime uktsTimestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                string stateName = this.ToJobStateName(JobState.Completed);

                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@timestamp", uktsTimestamp);
                command.Parameters.AddWithValue("@state", stateName);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool found = await reader.ReadAsync(cancellation);

                    if (found == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    result = this.ReadJob(reader);
                }
            }

            return result;
        }

        /// <summary>
        ///   Marks a job as failed.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the job to update.
        /// </param>
        /// <param name="timestamp">
        ///   The current date and time value.
        /// </param>
        /// <param name="error">
        ///   The description of the error that caused the job to fail; or <see langword="null"/> if not available.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="Job"/> object that represents the asynchronous operation and
        ///   provides access to the result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Argument <paramref name="id"/> is a <see langword="null"/> reference.
        /// </exception>
        /// <exception cref="UnknownIdentifierException">
        ///   A job with the specified unique identifier does not exist in the data store.
        /// </exception>
        public async Task<Job> MarkJobAsFailedAsync(string id, DateTime timestamp, string error, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(id, nameof(id));

            Job result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryMarkJobAsFailed;
                command.CommandType = CommandType.Text;

                DateTime uktsTimestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                string stateName = this.ToJobStateName(JobState.Failed);

                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@timestamp", uktsTimestamp);
                command.Parameters.AddWithValue("@state", stateName);
                command.Parameters.AddWithValue("@error", ((object) error) ?? DBNull.Value);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool found = await reader.ReadAsync(cancellation);

                    if (found == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    result = this.ReadJob(reader);
                }
            }

            return result;
        }

        /// <summary>
        ///   Marks a job as ready.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the job to update.
        /// </param>
        /// <param name="timestamp">
        ///   The current date and time value.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="Job"/> object that represents the asynchronous operation and
        ///   provides access to the result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Argument <paramref name="id"/> is a <see langword="null"/> reference.
        /// </exception>
        /// <exception cref="UnknownIdentifierException">
        ///   A job with the specified unique identifier does not exist in the data store.
        /// </exception>
        public async Task<Job> MarkJobAsReadyAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(id, nameof(id));

            Job result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryMarkJobAsReady;
                command.CommandType = CommandType.Text;

                DateTime uktsTimestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                string stateName = this.ToJobStateName(JobState.Ready);

                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@timestamp", uktsTimestamp);
                command.Parameters.AddWithValue("@state", stateName);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool found = await reader.ReadAsync(cancellation);

                    if (found == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    result = this.ReadJob(reader);
                }
            }

            return result;
        }

        /// <summary>
        ///   Marks a job as started.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the job to update.
        /// </param>
        /// <param name="timestamp">
        ///   The current date and time value.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="Job"/> object that represents the asynchronous operation and
        ///   provides access to the result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Argument <paramref name="id"/> is a <see langword="null"/> reference.
        /// </exception>
        /// <exception cref="UnknownIdentifierException">
        ///   A job with the specified unique identifier does not exist in the data store.
        /// </exception>
        public async Task<Job> MarkJobAsStartedAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(id, nameof(id));

            Job result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryMarkJobAsStarted;
                command.CommandType = CommandType.Text;

                DateTime uktsTimestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                string stateName = this.ToJobStateName(JobState.Processing);

                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@timestamp", uktsTimestamp);
                command.Parameters.AddWithValue("@state", stateName);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool found = await reader.ReadAsync(cancellation);

                    if (found == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    result = this.ReadJob(reader);
                }
            }

            return result;
        }

        /// <summary>
        ///   Updates a partition with the progress made.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the partition.
        /// </param>
        /// <param name="owner">
        ///   The unique identifier of the processing node reporting the progress.
        /// </param>
        /// <param name="timestamp">
        ///   The current timestamp.
        /// </param>
        /// <param name="position">
        ///   The key of the last item processed in the key range of the partition.
        /// </param>
        /// <param name="processed">
        ///   The total number of items processed in this partition.
        /// </param>
        /// <param name="remaining">
        ///   The total number of items remaining in this partition.
        /// </param>
        /// <param name="completed">
        ///   <see langword="true"/> if the partition is completed; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="throughput">
        ///   The number of items processed per second.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="Partition"/> object that represents the asynchronous
        ///   operation. The <see cref="Task{TResult}.Result"/> property contains the updated partition.
        /// </returns>
        /// <exception cref="LockException">
        ///   The specified <paramref name="owner"/> no longer holds the lock on the partition.
        /// </exception>
        /// <exception cref="UnknownIdentifierException">
        ///   A partition with the specified unique identifier does not exist.
        /// </exception>
        public async Task<Partition> ReportProgressAsync(Guid id, string owner, DateTime timestamp, string position, long processed, long remaining, bool completed, double throughput, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(owner, nameof(owner));
            Assert.ArgumentIsNotNull(position, nameof(position));

            Partition result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlTransaction transaction = (SqlTransaction) await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellation);

                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryReportProgress;
                command.CommandType = CommandType.Text;
                command.Connection = connection;
                command.Transaction = transaction;

                string sid = id.ToString("d");
                DateTime uktsTimestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);

                command.Parameters.AddWithValue("@updated", uktsTimestamp);
                command.Parameters.AddWithValue("@position", ((object) position) ?? DBNull.Value);
                command.Parameters.AddWithValue("@processed", processed);
                command.Parameters.AddWithValue("@remaining", remaining);
                command.Parameters.AddWithValue("@completed", completed);
                command.Parameters.AddWithValue("@id", sid);
                command.Parameters.AddWithValue("@owner", owner);
                command.Parameters.AddWithValue("@throughput", throughput);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool found = await reader.ReadAsync(cancellation);

                    if (found == true)
                    {
                        result = this.ReadPartition(reader);
                    }
                }

                if (result == null)
                {
                    bool exists = await this.DoesPartitionExistAsync(transaction, id, cancellation);

                    if (exists == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }
                    else
                    {
                        this.RaiseErrorLockNoLongerHeld();
                    }
                }

                transaction.Commit();
            }

            return result;
        }

        /// <summary>
        ///   Determines if a partition with the specified unique identifier exists.
        /// </summary>
        /// <param name="transaction">
        ///   The transaction to participate in.
        /// </param>
        /// <param name="id">
        ///   The unique identifier of the partition to check.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="bool"/> object that represents the asynchronous
        ///   operation.
        /// </returns>
        private async Task<bool> DoesPartitionExistAsync(SqlTransaction transaction, Guid id, CancellationToken cancellation)
        {
            Debug.Assert(transaction != null);

            await using SqlCommand command = new SqlCommand
            {
                CommandText = SqlServerEngineDataStoreImplementationResources.QueryDoesPartitionExist,
                CommandType = CommandType.Text,
                Connection = transaction.Connection,
                Transaction = transaction
            };

            string sid = id.ToString("d");

            command.Parameters.AddWithValue("@id", sid);

            int count = (int) await command.ExecuteScalarAsync(cancellation);

            return (count > 0);
        }

        /// <summary>
        ///   Retrieves the job with the specified identifier from the data store.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the job.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="Job"/> object that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="UnknownIdentifierException">
        ///   A job with the specified unique identifier does not exist.
        /// </exception>
        public async Task<Job> RetrieveJobAsync(string id, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(id, nameof(id));

            Job result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryRetrieveJobById;
                command.CommandType = System.Data.CommandType.Text;

                command.Parameters.AddWithValue("@id", id);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool exists = await reader.ReadAsync(cancellation);

                    if (exists == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    result = this.ReadJob(reader);
                }
            }

            return result;
        }

        public Task<StatusReport> RetrieveJobStatusReportAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Retrieves the partition with the specified identifier from the data store.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the partition.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="Partition"/> object that represents the asynchronous
        ///   operation.
        /// </returns>
        /// <exception cref="UnknownIdentifierException">
        ///   A partition with the specified unique identifier does not exist.
        /// </exception>
        public async Task<Partition> RetrievePartitionAsync(Guid id, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(id, nameof(id));

            Partition result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryRetrievePartitionById;
                command.CommandType = CommandType.Text;

                string sid = id.ToString("d");

                command.Parameters.AddWithValue("@id", sid);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool exists = await reader.ReadAsync(cancellation);

                    if (exists == false)
                    {
                        this.RaiseErrorUnknownPartitionIdentifier();
                    }

                    result = this.ReadPartition(reader);
                }
            }

            return result;
        }

        public Task<Partition> TryAcquirePartitionAsync(string jobId, string requester, DateTime timestamp, DateTime active, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryRequestSplitAsync(string jobId, DateTime active, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Returns an open database connection.
        /// </summary>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   An open <see cref="SqlConnection"/> object representing the connection to the database.
        /// </returns>
        private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellation)
        {
            SqlConnection result = new SqlConnection(this._connectionString);

            await result.OpenAsync(cancellation);

            return result;
        }

        /// <summary>
        ///   Constructs a <see cref="Job"/> object from the values read from the current position of the specified
        ///   reader.
        /// </summary>
        /// <param name="reader">
        ///   The <see cref="DbDataReader"/> to read from.
        /// </param>
        /// <returns>
        ///   The <see cref="Job"/> constructed from the values read from the reader.
        /// </returns>
        private Job ReadJob(DbDataReader reader)
        {
            Debug.Assert(reader != null);

            string id = reader.GetString("id");
            DateTime created = reader.GetDateTime("created");
            DateTime updated = reader.GetDateTime("updated");
            DateTime? started = reader.GetNullableDateTime("started");
            DateTime? completed = reader.GetNullableDateTime("completed");
            string stateName = reader.GetString("state");
            int batchSize = reader.GetInt32("batch_size");
            long ticksTimeout = reader.GetInt64("timeout");
            long ticksClockDrift = reader.GetInt64("clock_drift");
            string error = reader.GetNullableString("error");

            created = DateTime.SpecifyKind(created, DateTimeKind.Utc);
            updated = DateTime.SpecifyKind(updated, DateTimeKind.Utc);
            started = (started != null) ? DateTime.SpecifyKind(started.Value, DateTimeKind.Utc) : null;
            completed = (completed != null) ? DateTime.SpecifyKind(completed.Value, DateTimeKind.Utc) : null;

            TimeSpan timeout = TimeSpan.FromTicks(ticksTimeout);
            TimeSpan clockDrift = TimeSpan.FromTicks(ticksClockDrift);
            JobState state = this.ToJobStateValue(stateName);

            Job result = new Job(id, created, updated, state, batchSize, timeout, clockDrift, started, completed, error);

            return result;
        }

        /// <summary>
        ///   Constructs a <see cref="Partition"/> object from the values read from the current position of the
        ///   specified reader.
        /// </summary>
        /// <param name="reader">
        ///   The <see cref="DbDataReader"/> to read from.
        /// </param>
        /// <returns>
        ///   The <see cref="Partition"/> constructed from the values read from the reader.
        /// </returns>
        private Partition ReadPartition(DbDataReader reader)
        {
            Debug.Assert(reader != null);

            string sid = reader.GetString("id");
            string jobId = reader.GetString("job_id");
            DateTime created = reader.GetDateTime("created");
            DateTime updated = reader.GetDateTime("updated");
            string owner = reader.GetNullableString("owner");
            string first = reader.GetString("first");
            string last = reader.GetString("last");
            bool inclusive = reader.GetBoolean("is_inclusive");
            string position = reader.GetNullableString("position");
            long processed = reader.GetInt64("processed");
            long remaining = reader.GetInt64("remaining");
            double throughput = reader.GetDouble("throughput");
            bool completed = reader.GetBoolean("is_completed");
            bool split = reader.GetBoolean("is_split_requested");

            Guid id = Guid.ParseExact(sid, "d");

            created = DateTime.SpecifyKind(created, DateTimeKind.Utc);
            updated = DateTime.SpecifyKind(updated, DateTimeKind.Utc);

            Partition result = new Partition(id, jobId, created, updated, first, last, inclusive, position, processed, remaining, owner, completed, throughput, split);

            return result;
        }

        /// <summary>
        ///   Converts the specified job state name to a <see cref="JobState"/> value.
        /// </summary>
        /// <param name="value">
        ///   The value to convert.
        /// </param>
        /// <returns>
        ///   The converted value.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   The specified value could not be converted.
        /// </exception>
        private JobState ToJobStateValue(string value)
        {
            JobState result;

            switch (value.ToUpperInvariant())
            {
                case "INITIALIZING":
                    result = JobState.Initializing;
                    break;

                case "READY":
                    result = JobState.Ready;
                    break;

                case "PROCESSING":
                    result = JobState.Processing;
                    break;

                case "COMPLETED":
                    result = JobState.Completed;
                    break;

                case "FAILED":
                    result = JobState.Failed;
                    break;

                default:
                    throw new NotSupportedException();
            }

            return result;
        }

        /// <summary>
        ///   Converts the specified job state value to a job state name.
        /// </summary>
        /// <param name="value">
        ///   The value to convert.
        /// </param>
        /// <returns>
        ///   The converted value.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   The specified state could not be converted.
        /// </exception>
        private string ToJobStateName(JobState value)
        {
            string result;

            switch (value)
            {
                case JobState.Initializing:
                    result = "initializing";
                    break;

                case JobState.Ready:
                    result = "ready";
                    break;

                case JobState.Processing:
                    result = "processing";
                    break;

                case JobState.Completed:
                    result = "completed";
                    break;

                case JobState.Failed:
                    result = "failed";
                    break;

                default:
                    throw new NotSupportedException();
            }

            return result;
        }
    }
}
