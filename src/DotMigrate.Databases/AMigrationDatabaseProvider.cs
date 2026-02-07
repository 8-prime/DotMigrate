using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;
using DotMigrate.Exceptions;

namespace DotMigrate.Databases;

public abstract class AMigrationDatabaseProvider(
    AMigrationConfiguration migrationConfiguration,
    DbConnection connection
) : IMigrationDatabaseProvider
{
    protected readonly DbConnection Connection = connection;
    protected readonly AMigrationConfiguration MigrationConfiguration = migrationConfiguration;
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    public virtual void GetLock()
    {
        EnsureOpen();

        using var command = Connection.CreateCommand();
        command.CommandText = GetLockSql();
        command.CommandTimeout = 0;
        command.ExecuteNonQuery();
    }

    public virtual async Task GetLockAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);

        await using var command = Connection.CreateCommand();
        command.CommandText = GetLockSql();
        command.CommandTimeout = 0;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public virtual void ReleaseLock()
    {
        EnsureOpen();

        using var command = Connection.CreateCommand();
        command.CommandText = GetUnlockSql();
        command.ExecuteNonQuery();
    }

    public virtual async Task ReleaseLockAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);

        await using var command = Connection.CreateCommand();
        command.CommandText = GetUnlockSql();
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public virtual int? GetVersion()
    {
        EnsureReady();

        using var command = Connection.CreateCommand();
        command.CommandText = GetLastMigrationSql();
        command.Transaction = null;
        var result = command.ExecuteScalar();

        if (result == null)
            return null;
        try
        {
            return Convert.ToInt32(result);
        }
        catch
        {
            throw new MigrationException(
                "Database Provider returns a value for the current version which isn't a string"
            );
        }
    }

    public virtual async Task<int?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken);

        await using var command = Connection.CreateCommand();
        command.CommandText = GetLastMigrationSql();
        command.Transaction = null;
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result == null)
            return null;
        try
        {
            return Convert.ToInt32(result);
        }
        catch
        {
            throw new MigrationException(
                "Database Provider returns a value for the current version which isn't a string"
            );
        }
    }

    public virtual void ApplyMigration(IMigration migration)
    {
        EnsureReady();

        GetLock();
        try
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
        finally
        {
            ReleaseLock();
        }
    }

    public virtual async Task ApplyMigrationAsync(
        IMigration migration,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureReadyAsync(cancellationToken);

        await GetLockAsync(cancellationToken);
        try
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
        finally
        {
            await ReleaseLockAsync(cancellationToken);
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
        if (_isInitialized)
            return;

        _initializationLock.Wait();
        try
        {
            if (_isInitialized)
                return;

            EnsureOpen();

            GetLock();
            try
            {
                using (var command = Connection.CreateCommand())
                {
                    command.CommandText = CreateSchemaSql();
                    command.Transaction = null;
                    command.ExecuteNonQuery();
                }

                using (var command = Connection.CreateCommand())
                {
                    command.CommandText = CreateMigrationTableSql();
                    command.Transaction = null;
                    command.ExecuteNonQuery();
                }

                _isInitialized = true;
            }
            finally
            {
                ReleaseLock();
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return;

            await EnsureOpenAsync(cancellationToken);

            await GetLockAsync(cancellationToken);
            try
            {
                await using (var command = Connection.CreateCommand())
                {
                    command.CommandText = CreateSchemaSql();
                    command.Transaction = null;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var command = Connection.CreateCommand())
                {
                    command.CommandText = CreateMigrationTableSql();
                    command.Transaction = null;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                _isInitialized = true;
            }
            finally
            {
                await ReleaseLockAsync(cancellationToken);
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private void EnsureOpen()
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }
    }

    private async Task EnsureOpenAsync(CancellationToken cancellationToken = default)
    {
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken);
        }
    }
}