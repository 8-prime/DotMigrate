using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;
using DotMigrate.Exceptions;
using Microsoft.Data.SqlClient;

namespace DotMigrate.Databases.MsSql
{
    public class MsSqlDatabaseProvider : ADatabaseProvider
    {
        private readonly MsSqlMigrationConfiguration _sqlMigrationConfiguration;

        public MsSqlDatabaseProvider(string connectionString, MsSqlMigrationConfiguration configuration) : base(
            configuration, new SqlConnection(connectionString))
        {
            _sqlMigrationConfiguration = configuration;
        }

        public MsSqlDatabaseProvider(DbConnection connection, MsSqlMigrationConfiguration configuration) : base(
            configuration, connection)
        {
            _sqlMigrationConfiguration = configuration;
        }

        protected override string GetLockSql()
        {
            //     Lock with
            //     https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver17
            return
                $"sp_getapplock @Resource = '{_sqlMigrationConfiguration.AppLockName}', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = '{(int)_sqlMigrationConfiguration.LockTimeOut.TotalMilliseconds}'";
        }

        protected override string GetUnlockSql()
        {
            return $"sp_releaseapplock @Resource = '{_sqlMigrationConfiguration.AppLockName}', @LockOwner = 'Session'";
        }

        protected override string CreateSchemaSql()
        {
            return
                $"IF NOT EXISTS (select * from sys.schemas WHERE name ='{_sqlMigrationConfiguration.SchemaName}') EXECUTE ('CREATE SCHEMA [{_sqlMigrationConfiguration.MigrationTableName}]');";
        }

        protected override string CreateMigrationTableSql()
        {
            return
                $@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[{_sqlMigrationConfiguration.SchemaName}].[{_sqlMigrationConfiguration.MigrationTableName}]') AND type in (N'U'))
                BEGIN
                CREATE TABLE [{_sqlMigrationConfiguration.SchemaName}].[{_sqlMigrationConfiguration.MigrationTableName}](
                    [Id] [int] IDENTITY(1,1) PRIMARY KEY NOT NULL,
                    [Index] [int] NOT NULL,
                    [Name] [nvarchar(64)] NOT NULL,
                    [CreatedAt] [datetime2(7)] NOT NULL
                )
                END;";
        }

        protected override string GetLastMigrationSql()
        {
            return
                $"SELECT TOP 1 [Name] FROM [{_sqlMigrationConfiguration.SchemaName}].[{_sqlMigrationConfiguration.MigrationTableName}] ORDER BY [Id] desc;";
        }

        protected override string InsertMigrationSql()
        {
            return
                $@"INSERT INTO [{_sqlMigrationConfiguration.SchemaName}].[{_sqlMigrationConfiguration.MigrationTableName}] ([Index], [Name], [CreatedAt]) VALUES (@Index, @Name, GETDATE());";
        }
    }
}