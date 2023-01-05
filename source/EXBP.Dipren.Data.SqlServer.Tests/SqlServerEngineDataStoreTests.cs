
using EXBP.Dipren.Data.Tests;

using Microsoft.Data.SqlClient;

using NUnit.Framework;


namespace EXBP.Dipren.Data.SqlServer.Tests
{
    internal class SqlServerEngineDataStoreTests : EngineDataStoreTests
    {
        private const string DATABASE_NAME_MASTER = "master";
        private const string DATABASE_NAME_DIPREN = "dipren";

        private readonly string _connectionStringMaster;
        private readonly string _connectionStringDipren;


        public SqlServerEngineDataStoreTests()
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

        protected override Task<IEngineDataStore> OnCreateEngineDataStoreAsync()
            => Task.FromResult<IEngineDataStore>(new SqlServerEngineDataStore(this._connectionStringDipren));
    }
}
