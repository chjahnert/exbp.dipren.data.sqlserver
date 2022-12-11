
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

        public Task<long> CountJobsAsync(CancellationToken cancellation)
        {
            throw new NotImplementedException();
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

            await using SqlConnection connection = await this.OpenConnectionAsync(cancellation);
            await using SqlCommand command = connection.CreateCommand();

            command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryInsertJob;
            command.CommandType = System.Data.CommandType.Text;

            DateTime uktsCreated = DateTime.SpecifyKind(job.Created, DateTimeKind.Unspecified);
            DateTime uktsUpdated = DateTime.SpecifyKind(job.Updated, DateTimeKind.Unspecified);
            string stateName = this.GetJobStateName(job.State);
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

        public Task InsertPartitionAsync(Partition partition, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public Task InsertSplitPartitionAsync(Partition partitionToUpdate, Partition partitionToInsert, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public Task<Job> MarkJobAsCompletedAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public Task<Job> MarkJobAsFailedAsync(string id, DateTime timestamp, string error, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public Task<Job> MarkJobAsReadyAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public Task<Job> MarkJobAsStartedAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public Task<Partition> ReportProgressAsync(Guid id, string owner, DateTime timestamp, string position, long processed, long remaining, bool completed, double throughput, CancellationToken cancellation)
        {
            throw new NotImplementedException();
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

            await using SqlConnection connection = await this.OpenConnectionAsync(cancellation);
            await using SqlCommand command = connection.CreateCommand();

            command.CommandText = SqlServerEngineDataStoreImplementationResources.QueryRetrieveJobById;
            command.CommandType = System.Data.CommandType.Text;

            command.Parameters.AddWithValue("@id", id);

            Job result = null;

            await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellation))
            {
                bool exists = await reader.ReadAsync(cancellation);

                if (exists == false)
                {
                    this.RaiseErrorUnknownJobIdentifier();
                }

                result = this.ReadJob(reader);
            }

            return result;
        }

        public Task<StatusReport> RetrieveJobStatusReportAsync(string id, DateTime timestamp, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public Task<Partition> RetrievePartitionAsync(Guid id, CancellationToken cancellation)
        {
            throw new NotImplementedException();
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
            JobState state = this.GetJobStateValue(stateName);

            Job result = new Job(id, created, updated, state, batchSize, timeout, clockDrift, started, completed, error);

            return result;
        }

        private JobState GetJobStateValue(string value)
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

        private string GetJobStateName(JobState value)
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
