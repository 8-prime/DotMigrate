using DotMigrate.Abstractions;
using DotMigrate.Databases.MsSql;
using DotMigrate.Extensions;
using DotMigrate.IntegrationTests.Common;
using Microsoft.Data.SqlClient;
using DotMigrate.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace DotMigrate.IntegrationTests;

public class MigrationSchemaAndMultipleMigrationsTests : IAsyncLifetime
{
    private IServiceProvider _serviceProvider = null!;
    private readonly MsSqlContainer _msSqlContainer;

    public MigrationSchemaAndMultipleMigrationsTests()
    {
        _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .Build();
    }


    [Fact]
    public async Task RunAsync_ShouldCreateSchemaAndApplyMultipleMigrations()
    {
        var migrator = _serviceProvider.GetRequiredService<IMigrator<TestMigrations>>();
        Assert.NotNull(migrator);
        await migrator.RunAsync(TestContext.Current.CancellationToken);

        var connectionString = _msSqlContainer.GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        // Verify Foo table exists
        await using (var checkFooTableCmd = new SqlCommand("""

                                                                   SELECT COUNT(*) 
                                                                   FROM INFORMATION_SCHEMA.TABLES 
                                                                   WHERE TABLE_SCHEMA = 'IntegrationTests' 
                                                                   AND TABLE_NAME = 'Foo'
                                                           """, connection))
        {
            var fooTableExists = (int)await checkFooTableCmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, fooTableExists);
        }

        // Verify Bar table exists (second migration)
        await using (var checkBarTableCmd = new SqlCommand("""
                                                                   SELECT COUNT(*) 
                                                                   FROM INFORMATION_SCHEMA.TABLES 
                                                                   WHERE TABLE_SCHEMA = 'IntegrationTests' 
                                                                   AND TABLE_NAME = 'Bar'
                                                           """, connection))
        {
            var barTableExists = (int)await checkBarTableCmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, barTableExists);
        }

        // Verify migration entries exist for Index 1 and 2
        await using (var checkMigrationEntryCmd = new SqlCommand("""
                                                                         SELECT COUNT(*) 
                                                                         FROM [IntegrationTests].[TestMigrations] 
                                                                         WHERE [Index] IN (1,2)
                                                                 """, connection))
        {
            var migrationEntries = (int)await checkMigrationEntryCmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, migrationEntries);
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        var serviceCollection = new ServiceCollection();
        serviceCollection.MigrationsSetup<TestMigrations>(new FilesystemMigrationSource("./MultipleMigrations"), opts =>
                    opts.UseMsSql(
                        new MsSqlMigrationConfiguration
                        {
                            ConnectionString = _msSqlContainer.GetConnectionString(),
                            SchemaName = "IntegrationTests",
                            Mode = MigrationMode.Migrate,
                            MigrationTableName = "TestMigrations",
                            DatabaseUser = "sa"
                        }));

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }
}
