
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
    internal class SqlServerEngineDataStoreImplementation : IEngineDataStore
    {
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

        public Task InsertJobAsync(Job job, CancellationToken cancellation)
        {
            throw new NotImplementedException();
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

        public Task<Job> RetrieveJobAsync(string id, CancellationToken cancellation)
        {
            throw new NotImplementedException();
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
    }
}
