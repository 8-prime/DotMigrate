using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate;
using DotMigrate.Abstractions;
using DotMigrate.Exceptions;
using DotMigrate.Migrations;
using Xunit;

namespace DotMigrate.Tests;

public class MigratorTests
{
    private class TestProvider : IMigrationDatabaseProvider
    {
        private readonly List<IMigration> _applied = new();
        public int? Version { get; set; }

        public int? GetVersion() => Version;

        public Task<int?> GetVersionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Version);

        public void GetLock() { }

        public Task GetLockAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void ReleaseLock() { }

        public Task ReleaseLockAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void ApplyMigration(IMigration migration) => _applied.Add(migration);

        public Task ApplyMigrationAsync(
            IMigration migration,
            CancellationToken cancellationToken = default
        )
        {
            _applied.Add(migration);
            return Task.CompletedTask;
        }

        public IReadOnlyList<IMigration> Applied => _applied;
    }

    private class TestSource : IMigrationSource
    {
        private readonly IEnumerable<IMigration> _migrations;

        public TestSource(IEnumerable<IMigration> migrations) => _migrations = migrations;

        public IEnumerable<IMigration> GetMigrations() => _migrations;

        public Task<IEnumerable<IMigration>> GetMigrationsAsync(
            CancellationToken cancellationToken
        ) => Task.FromResult(_migrations);

        public IMigration? Latest() => _migrations.OrderByDescending(m => m.Index).FirstOrDefault();

        public Task<IMigration?> LatestAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Latest());
    }

    private static IMigration MakeMigration(int index, string name = "m") =>
        new FileMigration
        {
            Index = index,
            Name = name + index,
            Command = "select 1",
        };

    [Fact]
    public void AllMigrationsApplied_ReturnsTrueWhenSame()
    {
        var migrations = new[] { MakeMigration(1), MakeMigration(2) };
        var provider = new TestProvider { Version = 2 };
        var options = new MigrationOptions<string>
        {
            Provider = provider,
            Source = new TestSource(migrations),
            Configuration = new TestConfig { ConnectionString = "x" },
        };
        var migrator = new Migrator<string>(options);

        Assert.True(migrator.AllMigrationsApplied());
    }

    [Fact]
    public void Run_Validate_ThrowsWhenNotAllApplied()
    {
        var migrations = new[] { MakeMigration(1), MakeMigration(2) };
        var provider = new TestProvider { Version = 1 };
        var options = new MigrationOptions<string>
        {
            Provider = provider,
            Source = new TestSource(migrations),
            Configuration = new TestConfig
            {
                ConnectionString = "x",
                Mode = MigrationMode.Validate,
            },
        };
        var migrator = new Migrator<string>(options);

        Assert.Throws<MigrationException>(() => migrator.Run());
    }

    [Fact]
    public void MigrateOutstanding_AppliesMigrationsGreaterThanCurrent()
    {
        var migrations = new[] { MakeMigration(1), MakeMigration(2), MakeMigration(3) };
        var provider = new TestProvider { Version = 1 };
        var options = new MigrationOptions<string>
        {
            Provider = provider,
            Source = new TestSource(migrations),
            Configuration = new TestConfig { ConnectionString = "x" },
        };
        var migrator = new Migrator<string>(options);

        migrator.MigrateOutstanding();

        Assert.Equal(2, provider.Applied.Count); // 2 and 3
        Assert.Equal(2, provider.Applied[0].Index);
        Assert.Equal(3, provider.Applied[1].Index);
    }

    [Fact]
    public void MigrateToVersion_DoesNothingIfLatestGreaterThanTarget()
    {
        var migrations = new[] { MakeMigration(1), MakeMigration(2), MakeMigration(3) };
        var provider = new TestProvider { Version = 3 };
        var options = new MigrationOptions<string>
        {
            Provider = provider,
            Source = new TestSource(migrations),
            Configuration = new TestConfig { ConnectionString = "x" },
        };
        var migrator = new Migrator<string>(options);

        migrator.MigrateToVersion(2);

        Assert.Empty(provider.Applied);
    }

    [Fact]
    public async Task MigrateOutstandingAsync_AppliesMigrations()
    {
        var migrations = new[] { MakeMigration(1), MakeMigration(2), MakeMigration(3) };
        var provider = new TestProvider { Version = 1 };
        var options = new MigrationOptions<string>
        {
            Provider = provider,
            Source = new TestSource(migrations),
            Configuration = new TestConfig { ConnectionString = "x" },
        };
        var migrator = new Migrator<string>(options);

        await migrator.MigrateOutstandingAsync();

        Assert.Equal(2, provider.Applied.Count);
    }

    private class TestConfig : AMigrationConfiguration
    {
        public TestConfig()
        {
            ConnectionString = "x";
        }
    }
}
