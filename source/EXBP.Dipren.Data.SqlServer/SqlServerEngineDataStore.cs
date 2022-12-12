
using EXBP.Dipren.Diagnostics;
using EXBP.Dipren.Resilience;


namespace EXBP.Dipren.Data.SqlServer
{
    /// <summary>
    ///   Implements an <see cref="IEngineDataStore"/> that uses Microsoft SQL Server as its storage engine and uses a
    ///   backoff retry policy for resilience. The default retry strategy uses an exponential backoff delay.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <see cref="SqlServerEngineDataStore"/> implements a retry mechanism for resilience against transient
    ///     errors. The built in retry mechanism uses an exponential backoff retry strategy that retries operations
    ///     failing due to transient errors up to 12 times with exponentially growing wait times between each attempt.
    ///     It starts with 5 milliseconds, then 10, 20, 40, 80, and so on up to a maximum of 12 retry attempts. This
    ///     adds up to about 20 seconds in total before the operation is failed permanently.
    ///   </para>
    ///   <para>
    ///     In case the built in retry mechanism does not meet your needs, you can inject you can implement your own
    ///     retry strategy either from scratch or use an external framework such as
    ///     <see href="http://www.thepollyproject.org/">The Polly Project</see>. An example is provided in the
    ///     <see href="http://documentation">documentation</see>.
    ///   </para>
    ///   <para>
    ///     The database schema has to be deployed before using this class.
    ///   </para>
    ///   <para>
    ///     This class is thread-safe.
    ///   </para>
    /// </remarks>
    public class SqlServerEngineDataStore : ResilientEngineDataStore
    {
        private const int DEFAULT_RETRY_LIMIT = 12;
        private const int DEFAULT_RETRY_DELAY = 5;


        private static ITransientErrorDetector DefaultTransientErrorDetector = new DbTransientErrorDetector(false);

        private readonly SqlServerEngineDataStoreImplementation _store;
        private readonly IAsyncRetryStrategy _strategy;


        /// <summary>
        ///   Gets the engine data store instance being wrapped.
        /// </summary>
        /// <value>
        ///   The <see cref="IEngineDataStore"/> instance being wrapped.
        /// </value>
        protected override IEngineDataStore Store => this._store;

        /// <summary>
        ///   Gets the <see cref="IAsyncRetryStrategy"/> object that implements the retry strategy to use.
        /// </summary>
        /// <value>
        ///   The <see cref="IAsyncRetryStrategy"/> instance that implements the retry strategy to use.
        /// </value>
        protected override IAsyncRetryStrategy Strategy => this._strategy;


        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlServerEngineDataStore"/> class.
        /// </summary>
        /// <param name="connectionString">
        ///   The connection string to use when connecting to the database.
        /// </param>
        /// <param name="retryLimit">
        ///   The number of retry attempts to perform in case a transient error occurs. The default value is 12.
        /// </param>
        public SqlServerEngineDataStore(string connectionString, int retryLimit = DEFAULT_RETRY_LIMIT) : this(connectionString, retryLimit, TimeSpan.FromMilliseconds(DEFAULT_RETRY_DELAY))
        {
            TimeSpan retryDelay = TimeSpan.FromMilliseconds(DEFAULT_RETRY_DELAY);
            IBackoffDelayProvider backoffDelayProvider = new ExponentialBackoffDelayProvider(retryDelay);

            this._store = new SqlServerEngineDataStoreImplementation(connectionString);
            this._strategy = new BackoffRetryStrategy(retryLimit, backoffDelayProvider, DefaultTransientErrorDetector);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlServerEngineDataStore"/> class.
        /// </summary>
        /// <param name="connectionString">
        ///   The connection string to use when connecting to the database.
        /// </param>
        /// <param name="retryLimit">
        ///   The number of retry attempts to perform in case a transient error occurs.
        /// </param>
        /// <param name="retryDelay">
        ///   The duration to wait before the first retry attempt. The value is doubled for each subsequent retry
        ///   attempt.
        /// </param>
        public SqlServerEngineDataStore(string connectionString, int retryLimit, TimeSpan retryDelay)
        {
            IBackoffDelayProvider backoffDelayProvider = new ExponentialBackoffDelayProvider(retryDelay);

            this._store = new SqlServerEngineDataStoreImplementation(connectionString);
            this._strategy = new BackoffRetryStrategy(retryLimit, backoffDelayProvider, DefaultTransientErrorDetector);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlServerEngineDataStore"/> class.
        /// </summary>
        /// <param name="connectionString">
        ///   The connection string to use when connecting to the database.
        /// </param>
        /// <param name="retryStrategy">
        ///   The <see cref="IAsyncRetryStrategy"/> instance that implements the retry strategy to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   Argument <paramref name="retryStrategy"/> is a <see langword="null"/> reference.
        /// </exception>
        public SqlServerEngineDataStore(string connectionString, IAsyncRetryStrategy retryStrategy)
        {
            Assert.ArgumentIsNotNull(retryStrategy, nameof(retryStrategy));

            this._store = new SqlServerEngineDataStoreImplementation(connectionString);
            this._strategy = retryStrategy;
        }
    }
}
