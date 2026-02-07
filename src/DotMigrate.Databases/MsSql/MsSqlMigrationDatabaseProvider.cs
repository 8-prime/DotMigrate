using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DotMigrate.Databases.MsSql;

public partial class MsSqlMigrationDatabaseProvider : AMigrationDatabaseProvider
{
    private readonly MsSqlMigrationConfiguration _sqlMigrationConfiguration;

    public MsSqlMigrationDatabaseProvider(
        string connectionString,
        MsSqlMigrationConfiguration configuration
    )
        : base(configuration, new SqlConnection(connectionString))
    {
        _sqlMigrationConfiguration = configuration;
    }

    public MsSqlMigrationDatabaseProvider(
        DbConnection connection,
        MsSqlMigrationConfiguration configuration
    )
        : base(configuration, connection)
    {
        _sqlMigrationConfiguration = configuration;
    }

    protected override string GetLockSql()
    {
        //     Lock with
        //     https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver17
        return $"sp_getapplock @Resource = '{_sqlMigrationConfiguration.AppLockName}', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = '{(int)_sqlMigrationConfiguration.LockTimeOut.TotalMilliseconds}'";
    }

    protected override string GetUnlockSql()
    {
        return $"sp_releaseapplock @Resource = '{_sqlMigrationConfiguration.AppLockName}', @LockOwner = 'Session'";
    }

    protected override string CreateSchemaSql()
    {
        return $"IF NOT EXISTS (select * from sys.schemas WHERE name ='{_sqlMigrationConfiguration.SchemaName}') EXECUTE ('CREATE SCHEMA [{_sqlMigrationConfiguration.SchemaName}]');";
    }

    protected override string CreateMigrationTableSql()
    {
        return $"""
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[{_sqlMigrationConfiguration.SchemaName}].[{_sqlMigrationConfiguration.MigrationTableName}]') AND type in (N'U'))
                            BEGIN
                            CREATE TABLE [{_sqlMigrationConfiguration.SchemaName}].[{_sqlMigrationConfiguration.MigrationTableName}](
                                [Id] [int] IDENTITY(1,1) PRIMARY KEY NOT NULL,
                                [Index] [int] NOT NULL,
                                [Name] [nvarchar](64) NOT NULL,
                                [CreatedAt] [datetime2](7) NOT NULL
                            )
                            END;
            """;
    }

    protected override string InjectSchemaInformation(string sql)
    {
        if (string.IsNullOrEmpty(_sqlMigrationConfiguration.SchemaName))
            return sql;

        sql = CreateTableRegex()
            .Replace(sql, $"CREATE TABLE [{_sqlMigrationConfiguration.SchemaName}].$1");
        sql = AlterTableRegex()
            .Replace(sql, $"ALTER TABLE [{_sqlMigrationConfiguration.SchemaName}].$1");
        sql = DropTableRegex()
            .Replace(sql, $"DROP TABLE IF EXISTS [{_sqlMigrationConfiguration.SchemaName}].$1");
        sql = CreateIndexRegex().Replace(sql, $"ON [{_sqlMigrationConfiguration.SchemaName}].$1");

        return sql;
    }

    protected override string GetLastMigrationSql()
    {
        return $"SELECT TOP 1 [Index] FROM [{_sqlMigrationConfiguration.SchemaName}].[{_sqlMigrationConfiguration.MigrationTableName}] ORDER BY [Id] desc;";
    }

    protected override string InsertMigrationSql()
    {
        return $"INSERT INTO [{_sqlMigrationConfiguration.SchemaName}].[{_sqlMigrationConfiguration.MigrationTableName}] ([Index], [Name], [CreatedAt]) VALUES (@Index, @Name, GETDATE());";
    }

    [GeneratedRegex(@"\bON\s+(?:\[)?(?![\w\.]+\.)(\w+)(?:\])?", RegexOptions.IgnoreCase)]
    private static partial Regex CreateIndexRegex();

    [GeneratedRegex(
        @"\bDROP\s+TABLE\s+(?:IF\s+EXISTS\s+)?(?:\[)?(?![\w\.]+\.)(\w+)(?:\])?",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex DropTableRegex();

    [GeneratedRegex(@"\bALTER\s+TABLE\s+(?:\[)?(?![\w\.]+\.)(\w+)(?:\])?", RegexOptions.IgnoreCase)]
    private static partial Regex AlterTableRegex();

    [GeneratedRegex(
        @"\bCREATE\s+TABLE\s+(?:\[)?(?![\w\.]+\.)(\w+)(?:\])?",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex CreateTableRegex();
}
