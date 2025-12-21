using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;
using DotMigrate.Exceptions;

namespace DotMigrate;

public sealed class Migrator<TMigrator> : IMigrator<TMigrator>
{
    private readonly MigrationOptions<TMigrator> _migrationOptions;

    public Migrator(MigrationOptions<TMigrator> migrationOptions)
    {
        _migrationOptions = migrationOptions;
    }

    public void Run()
    {
        switch (_migrationOptions.Configuration.Mode)
        {
            case MigrationMode.Validate:
                if (!AllMigrationsApplied()) throw new MigrationException("Not all migrations applied");

                break;
            case MigrationMode.Migrate:
                if (_migrationOptions.Configuration.FromVersion is null &&
                    _migrationOptions.Configuration.ToVersion is null)
                    MigrateOutstanding();

                if (_migrationOptions.Configuration.FromVersion.HasValue &&
                    _migrationOptions.Configuration.ToVersion is null)
                    MigrateFrom(_migrationOptions.Configuration.FromVersion.Value);

                break;
            default:
                throw new MigrationConfigurationException(
                    $"Invalid migration mode. Mode {_migrationOptions.Configuration.Mode}");
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        switch (_migrationOptions.Configuration.Mode)
        {
            case MigrationMode.Validate:
                if (!await AllMigrationsAppliedAsync(cancellationToken))
                    throw new MigrationException("Not all migrations applied");

                break;
            case MigrationMode.Migrate:
                if (_migrationOptions.Configuration.FromVersion is null &&
                    _migrationOptions.Configuration.ToVersion is null)
                    await MigrateOutstandingAsync(cancellationToken);

                if (_migrationOptions.Configuration.FromVersion.HasValue &&
                    _migrationOptions.Configuration.ToVersion is null)
                    await MigrateFromAsync(_migrationOptions.Configuration.FromVersion.Value, cancellationToken);

                break;
            default:
                throw new MigrationConfigurationException(
                    $"Invalid migration mode. Mode {_migrationOptions.Configuration.Mode}");
        }
    }

    public bool AllMigrationsApplied()
    {
        var latestMigration = _migrationOptions.Provider.GetVersion();
        return _migrationOptions.Source.Latest()?.Name == latestMigration;
    }

    public async Task<bool> AllMigrationsAppliedAsync(CancellationToken cancellationToken = default)
    {
        var latestMigration = await _migrationOptions.Provider.GetVersionAsync(cancellationToken);
        return (await _migrationOptions.Source.LatestAsync(cancellationToken))?.Name == latestMigration;
    }


    public void MigrateOutstanding()
    {
        var latest = _migrationOptions.Provider.GetVersion();
        var appliedMigration = _migrationOptions.Source.GetMigrations().FirstOrDefault(m => m.Name == latest);
        if (appliedMigration is null) throw new MigrationException("Unknown migration applied in database");

        var outstandingMigrations =
            _migrationOptions.Source.GetMigrations().Where(m => m.Index > appliedMigration.Index)
                .OrderBy(m => m.Index);

        foreach (var migration in outstandingMigrations) _migrationOptions.Provider.ApplyMigration(migration);
    }

    public async Task MigrateOutstandingAsync(CancellationToken cancellationToken = default)
    {
        var latest = await _migrationOptions.Provider.GetVersionAsync(cancellationToken);
        var appliedMigration =
            (await _migrationOptions.Source.GetMigrationsAsync(cancellationToken)).FirstOrDefault(m =>
                m.Name == latest);
        if (appliedMigration is null) throw new MigrationException("Unknown migration applied in database");

        var outstandingMigrations =
            (await _migrationOptions.Source.GetMigrationsAsync(cancellationToken))
            .Where(m => m.Index > appliedMigration.Index)
            .OrderBy(m => m.Index);

        foreach (var migration in outstandingMigrations)
            await _migrationOptions.Provider.ApplyMigrationAsync(migration, cancellationToken);
    }

    public void MigrateFrom(int fromVersion)
    {
        if (_migrationOptions.Source.GetMigrations().All(m => m.Index != fromVersion))
            throw new MigrationException("Unknown migration specified as from version");

        var latest = _migrationOptions.Provider.GetVersion();
        var appliedMigration = _migrationOptions.Source.GetMigrations().FirstOrDefault(m => m.Name == latest);
        if (appliedMigration is null) throw new MigrationException("Unknown migration applied in database");

        var applyFrom = Math.Max(appliedMigration.Index, fromVersion);

        var outstandingMigrations =
            _migrationOptions.Source.GetMigrations().Where(m => m.Index > applyFrom).OrderBy(m => m.Index);

        foreach (var migration in outstandingMigrations) _migrationOptions.Provider.ApplyMigration(migration);
    }

    public async Task MigrateFromAsync(int fromVersion, CancellationToken cancellationToken = default)
    {
        if ((await _migrationOptions.Source.GetMigrationsAsync(cancellationToken)).All(m => m.Index != fromVersion))
            throw new MigrationException("Unknown migration specified as from version");

        var latest = await _migrationOptions.Provider.GetVersionAsync(cancellationToken);
        var appliedMigration =
            (await _migrationOptions.Source.GetMigrationsAsync(cancellationToken)).FirstOrDefault(m =>
                m.Name == latest);
        if (appliedMigration is null) throw new MigrationException("Unknown migration applied in database");

        var applyFrom = Math.Max(appliedMigration.Index, fromVersion);

        var outstandingMigrations =
            (await _migrationOptions.Source.GetMigrationsAsync(cancellationToken)).Where(m => m.Index > applyFrom)
            .OrderBy(m => m.Index);

        foreach (var migration in outstandingMigrations)
            await _migrationOptions.Provider.ApplyMigrationAsync(migration, cancellationToken);
    }
}