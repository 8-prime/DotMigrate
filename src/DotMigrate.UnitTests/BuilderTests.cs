using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;
using DotMigrate.Builder;
using Xunit;

namespace DotMigrate.Tests;

public class BuilderTests
{
    [Fact]
    public void Create_WithoutAllParts_Throws()
    {
        var builder = new MigrationOptionsBuilder<string>();
        Assert.Throws<InvalidOperationException>(() => builder.Create());
    }

    [Fact]
    public void Create_WithAllParts_ReturnsOptions()
    {
        var builder = new MigrationOptionsBuilder<string>();
        builder.WithConfiguration(new DummyConfig { ConnectionString = "x" });
        builder.WithProvider(new TestProvider());
        builder.WithSource(new TestSource());

        var options = builder.Create();
        Assert.NotNull(options);
        Assert.Equal("x", options.Configuration.ConnectionString);
    }

    private class DummyConfig : AMigrationConfiguration
    {
        public override string ToString()
        {
            return ConnectionString;
        }
    }

    private class TestProvider : IMigrationDatabaseProvider
    {
        public void GetLock() { }

        public Task GetLockAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void ReleaseLock() { }

        public Task ReleaseLockAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public int? GetVersion()
        {
            return 0;
        }

        public Task<int?> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<int?>(0);
        }

        public void ApplyMigration(IMigration migration) { }

        public Task ApplyMigrationAsync(
            IMigration migration,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }
    }

    private class TestSource : IMigrationSource
    {
        public IEnumerable<IMigration> GetMigrations()
        {
            return [];
        }

        public Task<IEnumerable<IMigration>> GetMigrationsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<IMigration>>([]);
        }

        public IMigration? Latest()
        {
            return null;
        }

        public Task<IMigration?> LatestAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IMigration?>(null);
        }
    }
}
