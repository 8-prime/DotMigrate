using System.Collections.Generic;
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
                if (!AllMigrationsApplied())
                    throw new MigrationException("Not all migrations applied");

                break;
            case MigrationMode.Migrate:
                if (_migrationOptions.Configuration.ToVersion is null)
                {
                    MigrateOutstanding();
                    return;
                }

                MigrateToVersion(_migrationOptions.Configuration.ToVersion.Value);

                break;
            default:
                throw new MigrationConfigurationException(
                    $"Invalid migration mode. Mode {_migrationOptions.Configuration.Mode}"
                );
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
                if (_migrationOptions.Configuration.ToVersion is null)
                {
                    await MigrateOutstandingAsync(cancellationToken);
                    return;
                }

                await MigrateToVersionAsync(
                    _migrationOptions.Configuration.ToVersion.Value,
                    cancellationToken
                );

                break;
            default:
                throw new MigrationConfigurationException(
                    $"Invalid migration mode. Mode {_migrationOptions.Configuration.Mode}"
                );
        }
    }

    public bool AllMigrationsApplied()
    {
        var latestMigration = _migrationOptions.Provider.GetVersion();
        return _migrationOptions.Source.Latest()?.Index == latestMigration;
    }

    public async Task<bool> AllMigrationsAppliedAsync(CancellationToken cancellationToken = default)
    {
        var latestMigration = await _migrationOptions.Provider.GetVersionAsync(cancellationToken);
        return (await _migrationOptions.Source.LatestAsync(cancellationToken))?.Index
            == latestMigration;
    }

    public void MigrateOutstanding()
    {
        var latest = _migrationOptions.Provider.GetVersion();
        var outstandingMigrations = latest is null
            ? _migrationOptions.Source.GetMigrations()
            : GetOutStandingMigrationBasedOnApplied(latest.Value);

        foreach (var migration in outstandingMigrations.OrderBy(m => m.Index))
        {
            _migrationOptions.Provider.ApplyMigration(migration);
        }
    }

    public async Task MigrateOutstandingAsync(CancellationToken cancellationToken = default)
    {
        var latest = await _migrationOptions.Provider.GetVersionAsync(cancellationToken);
        var outstandingMigrations = latest is null
            ? await _migrationOptions.Source.GetMigrationsAsync(cancellationToken)
            : await GetOutStandingMigrationBasedOnAppliedAsync(latest.Value, cancellationToken);

        foreach (var migration in outstandingMigrations.OrderBy(m => m.Index))
        {
            await _migrationOptions.Provider.ApplyMigrationAsync(migration, cancellationToken);
        }
    }

    public void MigrateToVersion(int version)
    {
        var latest = _migrationOptions.Provider.GetVersion();
        if (latest > version)
        {
            return;
        }

        var outstandingMigrations = _migrationOptions
            .Source.GetMigrations()
            .Where(m => m.Index > latest)
            .OrderBy(m => m.Index);

        foreach (var migration in outstandingMigrations)
            _migrationOptions.Provider.ApplyMigration(migration);
    }

    public async Task MigrateToVersionAsync(
        int version,
        CancellationToken cancellationToken = default
    )
    {
        var latest = await _migrationOptions.Provider.GetVersionAsync(cancellationToken);
        if (latest > version)
        {
            return;
        }

        var outstandingMigrations = (
            await _migrationOptions.Source.GetMigrationsAsync(cancellationToken)
        )
            .Where(m => m.Index > latest)
            .OrderBy(m => m.Index);

        foreach (var migration in outstandingMigrations)
            await _migrationOptions.Provider.ApplyMigrationAsync(migration, cancellationToken);
    }

    private IEnumerable<IMigration> GetOutStandingMigrationBasedOnApplied(
        int appliedMigrationIndex,
        CancellationToken cancellationToken = default
    )
    {
        var migrations = _migrationOptions.Source.GetMigrations();
        var appliedMigration = migrations.FirstOrDefault(m => m.Index == appliedMigrationIndex);
        if (appliedMigration is null)
        {
            throw new MigrationException("Unknown migration applied in database");
        }

        return _migrationOptions
            .Source.GetMigrations()
            .Where(m => m.Index > appliedMigration.Index);
    }

    private async Task<IEnumerable<IMigration>> GetOutStandingMigrationBasedOnAppliedAsync(
        int appliedMigrationIndex,
        CancellationToken cancellationToken = default
    )
    {
        var migrations = await _migrationOptions.Source.GetMigrationsAsync(cancellationToken);
        var appliedMigration = migrations.FirstOrDefault(m => m.Index == appliedMigrationIndex);
        if (appliedMigration is null)
        {
            throw new MigrationException("Unknown migration applied in database");
        }

        return (await _migrationOptions.Source.GetMigrationsAsync(cancellationToken)).Where(m =>
            m.Index > appliedMigration.Index
        );
    }
}
