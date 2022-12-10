
using System.Globalization;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

using NUnit.Framework;


namespace EXBP.Dipren.Data.SqlServer.Tests
{
    internal class SqlServerEngineDataStoreTests
    {
        private const string CONNECTION_STRING = "Server = localhost; Database = master; User Id = sa; Password = 4Laqzjn!LNYa@W63; TrustServerCertificate = True";

        private const string PATH_SCHEMA_SCRIPT_INSTALL = @"../../../../Database/install.sql";
        private const string PATH_SCHEMA_SCRIPT_REMOVE = @"../../../../Database/remove.sql";

        private const string DATABASE_NAME_MASTER = "master";
        private const string DATABASE_NAME_DIPREN = "dipren";


        [Test]
        public void Dummy()
        {
        }

        [OneTimeSetUp]
        public async Task BeforeFirstTestCaseAsync()
        {
            string statement = string.Format(CultureInfo.InvariantCulture, SqlServerEngineDataStoreResources.QueryCreateDatabase, DATABASE_NAME_DIPREN);

            await this.ExecuteStatementAsync(DATABASE_NAME_MASTER, statement, CancellationToken.None);
        }

        [OneTimeTearDown]
        public async Task AfterLastTestCaseAsync()
        {
            string statement = string.Format(CultureInfo.InvariantCulture, SqlServerEngineDataStoreResources.QueryDropDatabase, DATABASE_NAME_DIPREN);

            await this.ExecuteStatementAsync(DATABASE_NAME_MASTER, statement, CancellationToken.None);
        }

        [SetUp]
        public async Task BeforeEachTestCaseAsync()
        {
            await this.DropDatabaseSchemaAsync(CancellationToken.None);
            await this.CreateDatabaseSchemaAsync(CancellationToken.None);
        }

        [OneTimeTearDown]
        public async Task AfterTestFixtureAsync()
        {
            await this.DropDatabaseSchemaAsync(CancellationToken.None);
        }

        private async Task CreateDatabaseSchemaAsync(CancellationToken cancellation)
            => await this.ExecuteScriptAsync(DATABASE_NAME_DIPREN, PATH_SCHEMA_SCRIPT_INSTALL, cancellation);

        private async Task DropDatabaseSchemaAsync(CancellationToken cancellation)
            => await this.ExecuteScriptAsync(DATABASE_NAME_DIPREN, PATH_SCHEMA_SCRIPT_REMOVE, cancellation);

        private async Task ExecuteScriptAsync(string database, string path, CancellationToken cancellation)
        {
            string script = await File.ReadAllTextAsync(path, cancellation);

            await using SqlConnection clientConnection = new SqlConnection(CONNECTION_STRING);

            await clientConnection.OpenAsync(cancellation);
            await clientConnection.ChangeDatabaseAsync(database, cancellation);

            ServerConnection serverConnection = new ServerConnection(clientConnection);
            Server server = new Server(serverConnection);

            server.ConnectionContext.BeginTransaction();
            server.ConnectionContext.ExecuteNonQuery(script);
            server.ConnectionContext.CommitTransaction();
        }

        private async Task ExecuteStatementAsync(string database, string text, CancellationToken cancellation)
        {
            await using SqlConnection connection = new SqlConnection(CONNECTION_STRING);

            await connection.OpenAsync(cancellation);
            await connection.ChangeDatabaseAsync(database, cancellation);

            await using SqlCommand command = new SqlCommand
            {
                CommandText = text,
                Connection = connection
            };

            await command.ExecuteNonQueryAsync(cancellation);
        }
    }
}
