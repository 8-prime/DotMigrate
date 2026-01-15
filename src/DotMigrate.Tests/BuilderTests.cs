using System;
using DotMigrate.Builder;
using DotMigrate.Abstractions;
using Xunit;

namespace DotMigrate.Tests;

public class BuilderTests
{
    private class DummyConfig : AMigrationConfiguration { public override string ToString() => base.ConnectionString; }

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

    private class TestProvider : IMigrationDatabaseProvider
    {
        public void GetLock() { }
        public System.Threading.Tasks.Task GetLockAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
        public void ReleaseLock() { }
        public System.Threading.Tasks.Task ReleaseLockAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
        public int GetVersion() => 0;
        public System.Threading.Tasks.Task<int> GetVersionAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.FromResult(0);
        public void ApplyMigration(DotMigrate.Abstractions.IMigration migration) { }
        public System.Threading.Tasks.Task ApplyMigrationAsync(DotMigrate.Abstractions.IMigration migration, System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
    }

    private class TestSource : IMigrationSource
    {
        public System.Collections.Generic.IEnumerable<DotMigrate.Abstractions.IMigration> GetMigrations() => System.Array.Empty<DotMigrate.Abstractions.IMigration>();
        public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<DotMigrate.Abstractions.IMigration>> GetMigrationsAsync(System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.FromResult(System.Array.Empty<DotMigrate.Abstractions.IMigration>() as System.Collections.Generic.IEnumerable<DotMigrate.Abstractions.IMigration>);
        public DotMigrate.Abstractions.IMigration? Latest() => null;
        public System.Threading.Tasks.Task<DotMigrate.Abstractions.IMigration?> LatestAsync(System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.FromResult<DotMigrate.Abstractions.IMigration?>(null);
    }
}
