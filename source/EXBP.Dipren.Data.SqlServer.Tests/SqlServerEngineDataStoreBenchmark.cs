
using EXBP.Dipren.Data.Postgres.Tests;
using EXBP.Dipren.Data.Tests;

using Microsoft.Data.SqlClient;

using NUnit.Framework;


namespace EXBP.Dipren.Data.SqlServer.Tests
{
    [Explicit]
    [TestFixture]
    internal class SqlServerEngineDataStoreBenchmark
    {
        private const string REPORT_DIRECTORY = "../benchmarks/branch";

        private const string DATABASE_NAME_MASTER = "master";
        private const string DATABASE_NAME_DIPREN = "dipren";


        private readonly string _connectionStringMaster;
        private readonly string _connectionStringDipren;


        public SqlServerEngineDataStoreBenchmark()
        {
            SqlConnectionStringBuilder builderMaster = new SqlConnectionStringBuilder(Database.ConnectionStringTemplate);
            SqlConnectionStringBuilder builderDipren = new SqlConnectionStringBuilder(Database.ConnectionStringTemplate);

            builderMaster.InitialCatalog = DATABASE_NAME_MASTER;
            builderDipren.InitialCatalog = DATABASE_NAME_DIPREN;

            this._connectionStringMaster = builderMaster.ToString();
            this._connectionStringDipren = builderDipren.ToString();
        }


        [OneTimeSetUp]
        public async Task BeforeFirstTestCaseAsync()
        {
            await Database.DropDatabaseAsync(this._connectionStringMaster, DATABASE_NAME_DIPREN);
            await Database.CreateDatabaseAsync(this._connectionStringMaster, DATABASE_NAME_DIPREN);
        }

        [SetUp]
        public async Task BeforeEachTestCaseAsync()
        {
            await Database.DropDatabaseSchemaAsync(this._connectionStringDipren);
            await Database.CreateDatabaseSchemaAsync(this._connectionStringDipren);
        }

        [TearDown]
        public async Task AfterTestFixtureAsync()
        {
            await Database.DropDatabaseSchemaAsync(this._connectionStringDipren);
        }

        [OneTimeTearDown]
        public async Task AfterLastTestCaseAsync()
        {
            await Database.DropDatabaseAsync(this._connectionStringMaster, DATABASE_NAME_DIPREN);
        }


        [Test]
        [Order(1)]
        [Repeat(1)]
        public async Task Benchmark_Tiny()
        {
            await this.RunBenchmarkAsync(EngineDataStoreBenchmarkSettings.Tiny);
        }

        [Test]
        [Order(2)]
        [Repeat(1)]
        public async Task Benchmark_Small()
        {
            await this.RunBenchmarkAsync(EngineDataStoreBenchmarkSettings.Small);
        }

        [Test]
        [Order(3)]
        [Repeat(1)]
        public async Task Benchmark_Medium()
        {
            await this.RunBenchmarkAsync(EngineDataStoreBenchmarkSettings.Medium);
        }

        [Test]
        [Order(4)]
        [Repeat(1)]
        public async Task Benchmark_Large()
        {
            await this.RunBenchmarkAsync(EngineDataStoreBenchmarkSettings.Large);
        }

        [Test]
        [Order(5)]
        [Repeat(1)]
        public async Task Benchmark_Huge()
        {
            await this.RunBenchmarkAsync(EngineDataStoreBenchmarkSettings.Huge);
        }


        private async Task<EngineDataStoreBenchmarkRecording> RunBenchmarkAsync(EngineDataStoreBenchmarkSettings settings)
        {
            IEngineDataStoreFactory factory = new SqlServerEngineDataStoreFactory(this._connectionStringDipren);
            EngineDataStoreBenchmark benchmark = new EngineDataStoreBenchmark(factory, settings);

            EngineDataStoreBenchmarkRecording result = await benchmark.RunAsync();

            await EngineDataStoreBenchmarkReport.GenerateAsync(REPORT_DIRECTORY, result);

            TestContext.WriteLine($"{result.Duration.TotalSeconds}");

            return result;
        }


        private class SqlServerEngineDataStoreFactory : IEngineDataStoreFactory
        {
            private readonly string _connectionString;

            public SqlServerEngineDataStoreFactory(string connectionString)
            {
                this._connectionString = connectionString;
            }

            public Task<IEngineDataStore> CreateAsync(CancellationToken cancellation)
                => Task.FromResult<IEngineDataStore>(new SqlServerEngineDataStore(this._connectionString));
        }
    }
}
