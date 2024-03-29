﻿
using System.Collections.Concurrent;
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

        private const int COLUMN_JOB_NAME_LENGTH = 256;
        private const int COLUMN_JOB_STATE_LENGTH = 16;
        private const int COLUMN_PARTITION_OWNER_LENGTH = 256;


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


        /// <summary>
        ///   Returns the number of incomplete partitions for the specified job.
        /// </summary>
        /// <param name="jobId">
        ///   The unique identifier of the job for which to retrieve the number of incomplete partitions.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> or <see cref="long"/> that represents the asynchronous operation and can
        ///   be used to access the result.
        /// </returns>
        public async Task<long> CountIncompletePartitionsAsync(string jobId, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(jobId, nameof(jobId));

            long result = 0L;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryCountIncompletePartitions;
                command.CommandType = CommandType.Text;

                SqlParameter paramJobId = command.Parameters.Add("@job_id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);

                paramJobId.Value = jobId;

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    await reader.ReadAsync(cancellation);

                    long jobCount = reader.GetInt64("job_count");

                    if (jobCount == 0L)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    result = reader.GetInt64("partition_count");
                }
            }

            return result;
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
                command.CommandType = CommandType.Text;

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);
                SqlParameter paramCreated = command.Parameters.Add("@created", SqlDbType.DateTime2);
                SqlParameter paramUpdated = command.Parameters.Add("@updated", SqlDbType.DateTime2);
                SqlParameter paramBatchSize = command.Parameters.Add("@batch_size", SqlDbType.Int);
                SqlParameter paramTimeout = command.Parameters.Add("@timeout", SqlDbType.BigInt);
                SqlParameter paramClockDrift = command.Parameters.Add("@clock_drift", SqlDbType.BigInt);
                SqlParameter paramStarted = command.Parameters.Add("@started", SqlDbType.DateTime2);
                SqlParameter paramCompleted = command.Parameters.Add("@completed", SqlDbType.DateTime2);
                SqlParameter paramState = command.Parameters.Add("@state", SqlDbType.VarChar, COLUMN_JOB_STATE_LENGTH);
                SqlParameter paramError = command.Parameters.Add("@error", SqlDbType.Text);

                paramId.Value = job.Id;
                paramCreated.Value = DateTime.SpecifyKind(job.Created, DateTimeKind.Unspecified);
                paramUpdated.Value = DateTime.SpecifyKind(job.Updated, DateTimeKind.Unspecified);
                paramBatchSize.Value = job.BatchSize;
                paramTimeout.Value = job.Timeout.Ticks;
                paramClockDrift.Value = job.ClockDrift.Ticks;
                paramStarted.Value = ((job.Started != null) ? DateTime.SpecifyKind(job.Started.Value, DateTimeKind.Unspecified) : DBNull.Value);
                paramCompleted.Value = ((job.Completed != null) ? DateTime.SpecifyKind(job.Completed.Value, DateTimeKind.Unspecified) : DBNull.Value);
                paramState.Value = this.ToJobStateName(job.State);
                paramError.Value = ((job.Error != null) ? job.Error : DBNull.Value);

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

            SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.UniqueIdentifier);
            SqlParameter paramJobId = command.Parameters.Add("@job_id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);
            SqlParameter paramCreated = command.Parameters.Add("@created", SqlDbType.DateTime2);
            SqlParameter paramUpdated = command.Parameters.Add("@updated", SqlDbType.DateTime2);
            SqlParameter paramOwner = command.Parameters.Add("@owner", SqlDbType.VarChar, COLUMN_PARTITION_OWNER_LENGTH);
            SqlParameter paramFirst = command.Parameters.Add("@first", SqlDbType.Text);
            SqlParameter paramLast = command.Parameters.Add("@last", SqlDbType.Text);
            SqlParameter paramIsInclusive = command.Parameters.Add("@is_inclusive", SqlDbType.Bit);
            SqlParameter paramPosition = command.Parameters.Add("@position", SqlDbType.Text);
            SqlParameter paramProcessed = command.Parameters.Add("@processed", SqlDbType.BigInt);
            SqlParameter paramRemaining = command.Parameters.Add("@remaining", SqlDbType.BigInt);
            SqlParameter paramThroughput = command.Parameters.Add("@throughput", SqlDbType.Float);
            SqlParameter paramIsCompleted = command.Parameters.Add("@is_completed", SqlDbType.Bit);
            SqlParameter paramSplitRequester = command.Parameters.Add("@split_requester", SqlDbType.VarChar, COLUMN_PARTITION_OWNER_LENGTH);

            paramId.Value = partition.Id;
            paramJobId.Value = partition.JobId;
            paramCreated.Value = DateTime.SpecifyKind(partition.Created, DateTimeKind.Unspecified);
            paramUpdated.Value = DateTime.SpecifyKind(partition.Updated, DateTimeKind.Unspecified);
            paramOwner.Value = ((object) partition.Owner) ?? DBNull.Value;
            paramFirst.Value = partition.First;
            paramLast.Value = partition.Last;
            paramIsInclusive.Value = partition.IsInclusive;
            paramPosition.Value = ((object) partition.Position) ?? DBNull.Value;
            paramProcessed.Value = partition.Processed;
            paramRemaining.Value = partition.Remaining;
            paramThroughput.Value = partition.Throughput;
            paramIsCompleted.Value = partition.IsCompleted;
            paramSplitRequester.Value = ((object) partition.SplitRequester) ?? DBNull.Value;

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

        /// <summary>
        ///   Inserts a split off partition while updating the split partition as an atomic operation.
        /// </summary>
        /// <param name="partitionToUpdate">
        ///   The partition to update.
        /// </param>
        /// <param name="partitionToInsert">
        ///   The partition to insert.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> object that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Argument <paramref name="partitionToUpdate"/> or argument <paramref name="partitionToInsert"/> is a
        ///   <see langword="null"/> reference.
        /// </exception>
        /// <exception cref="UnknownIdentifierException">
        ///   The partition to update does not exist in the data store.
        /// </exception>
        /// <exception cref="DuplicateIdentifierException">
        ///   The partition to insert already exists in the data store.
        /// </exception>
        public async Task InsertSplitPartitionAsync(Partition partitionToUpdate, Partition partitionToInsert, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(partitionToUpdate, nameof(partitionToUpdate));
            Assert.ArgumentIsNotNull(partitionToInsert, nameof(partitionToInsert));

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlTransaction transaction = (SqlTransaction) await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellation);

                await using SqlCommand command = new SqlCommand
                {
                    CommandText = SqlServerEngineDataStoreImplementationResources.QueryUpdateSplitPartition,
                    CommandType = CommandType.Text,
                    Connection = connection,
                    Transaction = transaction
                };

                SqlParameter paramId = command.Parameters.Add("@partition_id", SqlDbType.UniqueIdentifier);
                SqlParameter paramOwner = command.Parameters.Add("@owner", SqlDbType.VarChar, COLUMN_PARTITION_OWNER_LENGTH);
                SqlParameter paramUpdated = command.Parameters.Add("@updated", SqlDbType.DateTime2);
                SqlParameter paramLast = command.Parameters.Add("@last", SqlDbType.Text);
                SqlParameter paramIsInclusive = command.Parameters.Add("@is_inclusive", SqlDbType.Bit);
                SqlParameter paramPosition = command.Parameters.Add("@position", SqlDbType.Text);
                SqlParameter paramProcessed = command.Parameters.Add("@processed", SqlDbType.BigInt);
                SqlParameter paramRemaining = command.Parameters.Add("@remaining", SqlDbType.BigInt);
                SqlParameter paramThroughput = command.Parameters.Add("@throughput", SqlDbType.Float);
                SqlParameter paramSplitRequester = command.Parameters.Add("@split_requester", SqlDbType.VarChar, COLUMN_PARTITION_OWNER_LENGTH);

                paramId.Value = partitionToUpdate.Id;
                paramOwner.Value = ((object) partitionToUpdate.Owner) ?? DBNull.Value;
                paramUpdated.Value = DateTime.SpecifyKind(partitionToUpdate.Updated, DateTimeKind.Unspecified);
                paramLast.Value = partitionToUpdate.Last;
                paramIsInclusive.Value = partitionToUpdate.IsInclusive;
                paramPosition.Value = ((object) partitionToUpdate.Position) ?? DBNull.Value;
                paramProcessed.Value = partitionToUpdate.Processed;
                paramRemaining.Value = partitionToUpdate.Remaining;
                paramThroughput.Value = partitionToUpdate.Throughput;
                paramSplitRequester.Value = ((object) partitionToUpdate.SplitRequester) ?? DBNull.Value;

                int affected = await command.ExecuteNonQueryAsync(cancellation);

                if (affected != 1)
                {
                    bool exists = await this.DoesPartitionExistAsync(transaction, partitionToUpdate.Id, cancellation);

                    if (exists == false)
                    {
                        this.RaiseErrorUnknownPartitionIdentifier();
                    }
                    else
                    {
                        this.RaiseErrorLockNoLongerHeld();
                    }
                }

                await this.InsertPartitionAsync(connection, transaction, partitionToInsert, cancellation);

                transaction.Commit();
            }
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

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);
                SqlParameter paramTimestamp = command.Parameters.Add("@timestamp", SqlDbType.DateTime2);
                SqlParameter paramState = command.Parameters.Add("@state", SqlDbType.VarChar, COLUMN_JOB_STATE_LENGTH);

                paramId.Value = id;
                paramTimestamp.Value = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                paramState.Value = this.ToJobStateName(JobState.Completed);

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

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);
                SqlParameter paramTimestamp = command.Parameters.Add("@timestamp", SqlDbType.DateTime2);
                SqlParameter paramState = command.Parameters.Add("@state", SqlDbType.VarChar, COLUMN_JOB_STATE_LENGTH);
                SqlParameter paramError = command.Parameters.Add("@error", SqlDbType.Text);

                paramId.Value = id;
                paramTimestamp.Value = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                paramState.Value = this.ToJobStateName(JobState.Failed);
                paramError.Value = ((object) error) ?? DBNull.Value;

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

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);
                SqlParameter paramTimestamp = command.Parameters.Add("@timestamp", SqlDbType.DateTime2);
                SqlParameter paramState = command.Parameters.Add("@state", SqlDbType.VarChar, COLUMN_JOB_STATE_LENGTH);

                paramId.Value = id;
                paramTimestamp.Value = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                paramState.Value = this.ToJobStateName(JobState.Ready);

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

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);
                SqlParameter paramTimestamp = command.Parameters.Add("@timestamp", SqlDbType.DateTime2);
                SqlParameter paramState = command.Parameters.Add("@state", SqlDbType.VarChar, COLUMN_JOB_STATE_LENGTH);

                paramId.Value = id;
                paramTimestamp.Value = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                paramState.Value = this.ToJobStateName(JobState.Processing);

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
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryReportProgress;
                command.CommandType = CommandType.Text;
                command.Connection = connection;

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.UniqueIdentifier);
                SqlParameter paramOwner = command.Parameters.Add("@owner", SqlDbType.VarChar, COLUMN_PARTITION_OWNER_LENGTH);
                SqlParameter paramUpdated = command.Parameters.Add("@updated", SqlDbType.DateTime2);
                SqlParameter paramPosition = command.Parameters.Add("@position", SqlDbType.Text);
                SqlParameter paramProcessed = command.Parameters.Add("@processed", SqlDbType.BigInt);
                SqlParameter paramRemaining = command.Parameters.Add("@remaining", SqlDbType.BigInt);
                SqlParameter paramCompleted = command.Parameters.Add("@completed", SqlDbType.Bit);
                SqlParameter paramThroughput = command.Parameters.Add("@throughput", SqlDbType.Float);

                paramId.Value = id;
                paramOwner.Value = owner;
                paramUpdated.Value = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                paramPosition.Value = ((object) position) ?? DBNull.Value;
                paramProcessed.Value = processed;
                paramRemaining.Value = remaining;
                paramCompleted.Value = completed;
                paramThroughput.Value = throughput;

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool found = await reader.ReadAsync(cancellation);

                    if (found == true)
                    {
                        result = this.ReadPartition(reader);
                    }
                    else
                    {
                        await reader.NextResultAsync(cancellation);

                        bool exists = await reader.ReadAsync(cancellation);

                        if (exists == false)
                        {
                            this.RaiseErrorUnknownPartitionIdentifier();
                        }
                        else
                        {
                            this.RaiseErrorLockNoLongerHeld();
                        }
                    }
                }
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

            SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.UniqueIdentifier);

            paramId.Value = id;

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
                command.CommandType = CommandType.Text;

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);

                paramId.Value = id;

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

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.UniqueIdentifier);

                paramId.Value = id;

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

        /// <summary>
        ///   Gets a status report for the job with the specified identifier.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier of the job.
        /// </param>
        /// <param name="timestamp">
        ///   The current date and time, expressed in UTC time.
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
        public async Task<StatusReport> RetrieveJobStatusReportAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(id, nameof(id));

            StatusReport result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryRetrieveJobStatusReport;
                command.CommandType = CommandType.Text;

                SqlParameter paramId = command.Parameters.Add("@id", SqlDbType.VarChar, 256);
                SqlParameter paramTimestamp = command.Parameters.Add("@timestamp", SqlDbType.DateTime2);

                paramId.Value = id;
                paramTimestamp.Value = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool found = await reader.ReadAsync(cancellation);

                    if (found == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    Job job = this.ReadJob(reader);

                    result = new StatusReport
                    {
                        Id = job.Id,
                        Timestamp = timestamp,
                        Created = job.Created,
                        Updated = job.Updated,
                        BatchSize = job.BatchSize,
                        Timeout = job.Timeout,
                        Started = job.Started,
                        Completed = job.Completed,
                        State = job.State,
                        Error = job.Error,

                        LastActivity = reader.GetDateTime("last_activity"),
                        OwnershipChanges = reader.GetInt64("ownership_changes"),
                        PendingSplitRequests = reader.GetInt64("split_requests_pending"),
                        CurrentThroughput = reader.GetDouble("current_throughput"),

                        Partitions = new StatusReport.PartitionsReport
                        {
                            Untouched = reader.GetInt64("partitons_untouched"),
                            InProgress = reader.GetInt64("partitons_in_progress"),
                            Completed = reader.GetInt64("partitions_completed")
                        },

                        Progress = new StatusReport.ProgressReport
                        {
                            Remaining = reader.GetNullableInt64("keys_remaining"),
                            Completed = reader.GetNullableInt64("keys_completed")
                        }
                    };
                }
            }

            return result;
        }

        /// <summary>
        ///   Tries to acquire a free or abandoned partition.
        /// </summary>
        /// <param name="jobId">
        ///   The unique identifier of the distributed processing job.
        /// </param>
        /// <param name="requester">
        ///   The identifier of the processing node trying to acquire a partition.
        /// </param>
        /// <param name="timestamp">
        ///   The current timestamp.
        /// </param>
        /// <param name="active">
        ///   A <see cref="DateTime"/> value that is used to determine if a partition is actively being processed.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="Partition"/> object that represents the asynchronous
        ///   operation. The <see cref="Task{TResult}.Result"/> property contains the acquired partition if succeeded;
        ///   otherwise, <see langword="null"/>.
        /// </returns>
        /// <exception cref="UnknownIdentifierException">
        ///   A job with the specified unique identifier does not exist in the data store.
        /// </exception>
        public async Task<Partition> TryAcquirePartitionAsync(string jobId, string requester, DateTime timestamp, DateTime active, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(jobId, nameof(jobId));
            Assert.ArgumentIsNotNull(requester, nameof(requester));

            Partition result = null;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = new SqlCommand
                {
                    CommandText = SqlServerEngineDataStoreImplementationResources.QueryTryAcquirePartition,
                    CommandType = CommandType.Text,
                    Connection = connection
                };

                SqlParameter paramJobId = command.Parameters.Add("@job_id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);
                SqlParameter paramOwner = command.Parameters.Add("@owner", SqlDbType.VarChar, COLUMN_PARTITION_OWNER_LENGTH);
                SqlParameter paramUpdated = command.Parameters.Add("@updated", SqlDbType.DateTime2);
                SqlParameter paramActive = command.Parameters.Add("@active", SqlDbType.DateTime2);

                paramJobId.Value = jobId;
                paramOwner.Value = requester;
                paramUpdated.Value = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
                paramActive.Value = DateTime.SpecifyKind(active, DateTimeKind.Unspecified);

                await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool exists = await reader.ReadAsync();

                    if (exists == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    await reader.NextResultAsync(cancellation);

                    bool found = await reader.ReadAsync(cancellation);

                    if (found == true)
                    {
                        result = this.ReadPartition(reader);
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///   Requests an existing partition to be split.
        /// </summary>
        /// <param name="jobId">
        ///   The unique identifier of the distributed processing job.
        /// </param>
        /// <param name="requester">
        ///   The unique identifier of the processing node trying to request a split.
        /// </param>
        /// <param name="active">
        ///   A <see cref="DateTime"/> value that is used to determine whether a partition is being processed.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="bool"/> object that represents the asynchronous
        ///   operation. The <see cref="Task{TResult}.Result"/> property contains a value indicating whether a split
        ///   was requested.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Argument <paramref name="jobId"/> or argument <paramref name="requester"/> is a <see langword="null"/>
        ///   reference.
        /// </exception>
        /// <exception cref="UnknownIdentifierException">
        ///   A job with the specified unique identifier does not exist in the data store.
        /// </exception>
        public async Task<bool> TryRequestSplitAsync(string jobId, string requester, DateTime active, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(jobId, nameof(jobId));
            Assert.ArgumentIsNotNull(requester, nameof(requester));

            bool result = false;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = new SqlCommand
                {
                    CommandText = SqlServerEngineDataStoreImplementationResources.QueryTryRequestSplit,
                    CommandType = CommandType.Text,
                    Connection = connection
                };

                SqlParameter paramJobId = command.Parameters.Add("@job_id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH);
                SqlParameter paramRequester = command.Parameters.Add("@requester", SqlDbType.VarChar, COLUMN_PARTITION_OWNER_LENGTH);
                SqlParameter paramActive = command.Parameters.Add("@active", SqlDbType.DateTime2);

                paramJobId.Value = jobId;
                paramRequester.Value = requester;
                paramActive.Value = DateTime.SpecifyKind(active, DateTimeKind.Unspecified);

                int affected = -1;

                await using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellation))
                {
                    bool exists = await reader.ReadAsync(cancellation);

                    if (exists == false)
                    {
                        this.RaiseErrorUnknownJobIdentifier();
                    }

                    await reader.NextResultAsync(cancellation);

                    affected = reader.RecordsAffected;
                }

                result = (affected > 0);
            }

            return result;
        }

        /// <summary>
        ///   Determines whether a split request for the specified requester is still pending.
        /// </summary>
        /// <param name="jobId">
        ///   The unique identifier of the distributed processing job.
        /// </param>
        /// <param name="requester">
        ///   The unique identifier of the processing node that requested a split.
        /// </param>
        /// <param name="cancellation">
        ///   The <see cref="CancellationToken"/> used to propagate notifications that the operation should be
        ///   canceled.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> of <see cref="bool"/> object that represents the asynchronous
        ///   operation. The <see cref="Task{TResult}.Result"/> property contains a value indicating whether a split
        ///   request is pending.
        /// </returns>
        public async Task<bool> IsSplitRequestPendingAsync(string jobId, string requester, CancellationToken cancellation)
        {
            Assert.ArgumentIsNotNull(jobId, nameof(jobId));
            Assert.ArgumentIsNotNull(requester, nameof(requester));

            bool result = false;

            await using (SqlConnection connection = await this.OpenConnectionAsync(cancellation))
            {
                await using SqlCommand command = connection.CreateCommand();

                command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryIsSplitRequestPending;
                command.CommandType = CommandType.Text;

                SqlParameter paramJobId = command.Parameters.Add("@job_id", SqlDbType.VarChar, COLUMN_JOB_NAME_LENGTH, jobId);
                SqlParameter paramRequester = command.Parameters.Add("@requester", SqlDbType.VarChar, COLUMN_PARTITION_OWNER_LENGTH, requester);

                paramJobId.Value = jobId;
                paramRequester.Value = requester;

                int value = (int) await command.ExecuteScalarAsync(cancellation);

                result = (value > 0);
            }

            return result;
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

            Guid id = reader.GetGuid("id");
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
            string requester = reader.GetNullableString("split_requester");

            created = DateTime.SpecifyKind(created, DateTimeKind.Utc);
            updated = DateTime.SpecifyKind(updated, DateTimeKind.Utc);

            Partition result = new Partition(id, jobId, created, updated, first, last, inclusive, position, processed, remaining, owner, completed, throughput, requester);

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
