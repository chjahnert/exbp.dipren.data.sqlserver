
using System.Globalization;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

using Microsoft.SqlServer.Management.Smo;


namespace EXBP.Dipren.Data.SqlServer.Tests
{
    internal static class Database
    {
        private const string PATH_SCHEMA_SCRIPT_INSTALL = @"../../../../Database/install.sql";
        private const string PATH_SCHEMA_SCRIPT_REMOVE = @"../../../../Database/remove.sql";


        internal static string ConnectionStringTemplate => "Server = localhost; User Id = sa; Password = 4Laqzjn!LNYa@W63; TrustServerCertificate = True";


        internal static async Task CreateDatabaseAsync(string connectionString, string databaseName, CancellationToken cancellation = default)
        {
            string statementCreate = string.Format(CultureInfo.InvariantCulture, DatabaseResources.QueryCreateDatabase, databaseName);

            await Database.ExecuteStatementAsync(connectionString, statementCreate, cancellation);
        }

        internal static async Task DropDatabaseAsync(string connectionString, string databaseName, CancellationToken cancellation = default)
        {
            string statement = string.Format(CultureInfo.InvariantCulture, DatabaseResources.QueryDropDatabase, databaseName);

            await Database.ExecuteStatementAsync(connectionString, statement, cancellation);
        }

        internal static async Task CreateDatabaseSchemaAsync(string connectionString, CancellationToken cancellation = default)
            => await Database.ExecuteScriptAsync(connectionString, PATH_SCHEMA_SCRIPT_INSTALL, cancellation);

        internal static async Task DropDatabaseSchemaAsync(string connectionString, CancellationToken cancellation = default)
            => await Database.ExecuteScriptAsync(connectionString, PATH_SCHEMA_SCRIPT_REMOVE, cancellation);

        private static async Task ExecuteScriptAsync(string connectionString, string path, CancellationToken cancellation = default)
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

        private static async Task ExecuteStatementAsync(string connectionString, string text, CancellationToken cancellation = default)
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
