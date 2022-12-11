
using System.Globalization;

using EXBP.Dipren.Data.Tests;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

using NUnit.Framework;


namespace EXBP.Dipren.Data.SqlServer.Tests
{
    internal class SqlServerEngineDataStoreTests : EngineDataStoreTests
    {
        private const string CONNECTION_STRING_TEMPLATE = "Server = localhost; User Id = sa; Password = 4Laqzjn!LNYa@W63; TrustServerCertificate = True";

        private const string PATH_SCHEMA_SCRIPT_INSTALL = @"../../../../Database/install.sql";
        private const string PATH_SCHEMA_SCRIPT_REMOVE = @"../../../../Database/remove.sql";

        private const string DATABASE_NAME_MASTER = "master";
        private const string DATABASE_NAME_DIPREN = "dipren";

        private readonly string _connectionStringMaster;
        private readonly string _connectionStringDipren;


        public SqlServerEngineDataStoreTests()
        {
            SqlConnectionStringBuilder builderMaster = new SqlConnectionStringBuilder(CONNECTION_STRING_TEMPLATE);
            SqlConnectionStringBuilder builderDipren = new SqlConnectionStringBuilder(CONNECTION_STRING_TEMPLATE);

            builderMaster.InitialCatalog = DATABASE_NAME_MASTER;
            builderDipren.InitialCatalog = DATABASE_NAME_DIPREN;

            this._connectionStringMaster = builderMaster.ToString();
            this._connectionStringDipren = builderDipren.ToString();
        }


        protected override Task<IEngineDataStore> OnCreateEngineDataStoreAsync()
            => Task.FromResult<IEngineDataStore>(new SqlServerEngineDataStoreImplementation(this._connectionStringDipren));


        [OneTimeSetUp]
        public async Task BeforeFirstTestCaseAsync()
        {
            await this.DropDatabaseAsync();
            await this.CreateDatabaseAsync();
        }

        [SetUp]
        public async Task BeforeEachTestCaseAsync()
        {
            await this.DropDatabaseSchemaAsync();
            await this.CreateDatabaseSchemaAsync();
        }

        [TearDown]
        public async Task AfterTestFixtureAsync()
        {
            await this.DropDatabaseSchemaAsync();
        }

        [OneTimeTearDown]
        public async Task AfterLastTestCaseAsync()
        {
            await this.DropDatabaseAsync();
        }


        private async Task CreateDatabaseAsync(CancellationToken cancellation = default)
        {
            string statementCreate = string.Format(CultureInfo.InvariantCulture, SqlServerEngineDataStoreResources.QueryCreateDatabase, DATABASE_NAME_DIPREN);

            await this.ExecuteStatementAsync(this._connectionStringMaster, statementCreate, cancellation);
        }

        private async Task DropDatabaseAsync(CancellationToken cancellation = default)
        {
            string statement = string.Format(CultureInfo.InvariantCulture, SqlServerEngineDataStoreResources.QueryDropDatabase, DATABASE_NAME_DIPREN);

            await this.ExecuteStatementAsync(this._connectionStringMaster, statement, cancellation);
        }

        private async Task CreateDatabaseSchemaAsync(CancellationToken cancellation = default)
            => await this.ExecuteScriptAsync(this._connectionStringDipren, PATH_SCHEMA_SCRIPT_INSTALL, cancellation);

        private async Task DropDatabaseSchemaAsync(CancellationToken cancellation = default)
            => await this.ExecuteScriptAsync(this._connectionStringDipren, PATH_SCHEMA_SCRIPT_REMOVE, cancellation);

        private async Task ExecuteScriptAsync(string connectionString, string path, CancellationToken cancellation = default)
        {
            string script = await File.ReadAllTextAsync(path, cancellation);

            await using SqlConnection clientConnection = new SqlConnection(connectionString);

            await clientConnection.OpenAsync(cancellation);

            ServerConnection serverConnection = new ServerConnection(clientConnection);
            Server server = new Server(serverConnection);

            server.ConnectionContext.BeginTransaction();
            server.ConnectionContext.ExecuteNonQuery(script);
            server.ConnectionContext.CommitTransaction();
        }

        private async Task ExecuteStatementAsync(string connectionString, string text, CancellationToken cancellation = default)
        {
            await using SqlConnection connection = new SqlConnection(connectionString);

            await connection.OpenAsync(cancellation);

            await using SqlCommand command = new SqlCommand
            {
                CommandText = text,
                Connection = connection
            };

            await command.ExecuteNonQueryAsync(cancellation);
        }
    }
}
