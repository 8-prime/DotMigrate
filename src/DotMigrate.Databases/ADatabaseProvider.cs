using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;
using DotMigrate.Exceptions;

namespace DotMigrate.Databases
{
    public abstract class ADatabaseProvider(AMigrationConfiguration migrationConfiguration, DbConnection connection)
        : IDatabaseProvider

    {
        protected readonly AMigrationConfiguration MigrationConfiguration = migrationConfiguration;
        protected readonly DbConnection Connection = connection;

        public virtual void GetLock()
        {
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            using var command = Connection.CreateCommand();
            command.CommandText = GetLockSql();
            command.CommandTimeout = 0;
            command.ExecuteNonQuery();
        }

        public virtual async Task GetLockAsync(CancellationToken cancellationToken = default)
        {
            if (Connection.State != ConnectionState.Open)
            {
                await Connection.OpenAsync(cancellationToken);
            }

            await using var command = Connection.CreateCommand();
            command.CommandText = GetLockSql();
            command.CommandTimeout = 0;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public virtual void ReleaseLock()
        {
            if (Connection.State != ConnectionState.Open) Connection.Open();

            using var command = Connection.CreateCommand();
            command.CommandText = GetUnlockSql();
            command.ExecuteNonQuery();
        }

        public virtual async Task ReleaseLockAsync(CancellationToken cancellationToken = default)
        {
            if (Connection.State != ConnectionState.Open) await Connection.OpenAsync(cancellationToken);

            await using var command = Connection.CreateCommand();
            command.CommandText = GetUnlockSql();
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public virtual string GetVersion()
        {
            var version = string.Empty;
            using var command = Connection.CreateCommand();
            command.CommandText = GetLastMigrationSql();
            command.Transaction = null;
            var result = command.ExecuteScalar();

            if (result == null) return version;
            try
            {
                version = Convert.ToString(result);
            }
            catch
            {
                throw new MigrationException(
                    "Database Provider returns a value for the current version which isn't a string");
            }

            return version ?? throw new MigrationException("Version returned a null value");
        }

        public virtual async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            var version = string.Empty;
            await using var command = Connection.CreateCommand();
            command.CommandText = GetLastMigrationSql();
            command.Transaction = null;
            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result == null) return version;
            try
            {
                version = Convert.ToString(result);
            }
            catch
            {
                throw new MigrationException(
                    "Database Provider returns a value for the current version which isn't a string");
            }

            return version ?? throw new MigrationException("Version returned a null value");
        }

        public virtual void ApplyMigration(IMigration migration)
        {
            using (var command = Connection.CreateCommand())
            {
                command.Transaction = null;
                command.CommandText = InsertMigrationSql();

                var versionParam = command.CreateParameter();
                versionParam.ParameterName = "Index";
                versionParam.Value = migration.Index;
                command.Parameters.Add(versionParam);

                var oldVersionParam = command.CreateParameter();
                oldVersionParam.ParameterName = "Name";
                oldVersionParam.Value = migration.Name;
                command.Parameters.Add(oldVersionParam);
                command.ExecuteNonQuery();
            }

            using (var command = Connection.CreateCommand())
            {
                command.CommandText = migration.Command;
                command.Transaction = null;
                command.ExecuteNonQuery();
            }
        }

        public virtual async Task ApplyMigrationAsync(IMigration migration,
            CancellationToken cancellationToken = default)
        {
            await using (var command = Connection.CreateCommand())
            {
                command.Transaction = null;
                command.CommandText = InsertMigrationSql();

                var versionParam = command.CreateParameter();
                versionParam.ParameterName = "Index";
                versionParam.Value = migration.Index;
                command.Parameters.Add(versionParam);

                var oldVersionParam = command.CreateParameter();
                oldVersionParam.ParameterName = "Name";
                oldVersionParam.Value = migration.Name;
                command.Parameters.Add(oldVersionParam);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var command = Connection.CreateCommand())
            {
                command.CommandText = migration.Command;
                command.Transaction = null;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        protected abstract string InsertMigrationSql();
        protected abstract string GetLastMigrationSql();
        protected abstract string GetLockSql();
        protected abstract string GetUnlockSql();
        protected abstract string CreateSchemaSql();
        protected abstract string CreateMigrationTableSql();

        private void EnsureReady()
        {
            if (Connection.State != ConnectionState.Open) Connection.Open();

            GetLock();
            try
            {
                using (var command = Connection.CreateCommand())
                {
                    command.CommandText = CreateSchemaSql();
                    command.Transaction = null; //TODO transaction?
                    command.ExecuteNonQuery();
                }

                using (var command = Connection.CreateCommand())
                {
                    command.CommandText = CreateMigrationTableSql();
                    command.Transaction = null; //TODO transaction?
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                ReleaseLock();
            }
        }

        private async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
        {
            if (Connection.State != ConnectionState.Open) await Connection.OpenAsync(cancellationToken);

            await GetLockAsync(cancellationToken);
            try
            {
                await using (var command = Connection.CreateCommand())
                {
                    command.CommandText = CreateSchemaSql();
                    command.Transaction = null; //TODO transaction?
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var command = Connection.CreateCommand())
                {
                    command.CommandText = CreateMigrationTableSql();
                    command.Transaction = null; //TODO transaction?
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            finally
            {
                await ReleaseLockAsync(cancellationToken);
            }
        }
    }
}