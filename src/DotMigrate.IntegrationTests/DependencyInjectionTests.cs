using DotMigrate.Abstractions;
using DotMigrate.Databases.MsSql;
using DotMigrate.Extensions;
using DotMigrate.IntegrationTests.Common;
using DotMigrate.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace DotMigrate.IntegrationTests;

public class DependencyInjectionTests : IAsyncLifetime
{
    private IServiceProvider _serviceProvider = null!;
    private readonly MsSqlContainer _msSqlContainer;

    public DependencyInjectionTests()
    {
        _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .Build();
    }


    [Fact]
    public async Task RunAsync_ShouldWriteMigrationToDatabase()
    {
        var migrator = _serviceProvider.GetRequiredService<IMigrator<TestMigrations>>();
        Assert.NotNull(migrator);
        await migrator.RunAsync(TestContext.Current.CancellationToken);
        // Get connection string to verify database state
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

        // Verify Foo table has correct structure
        await using (var checkFooStructureCmd = new SqlCommand("""
                                                                       SELECT COUNT(*) 
                                                                       FROM INFORMATION_SCHEMA.COLUMNS 
                                                                       WHERE TABLE_SCHEMA = 'IntegrationTests' 
                                                                       AND TABLE_NAME = 'Foo' 
                                                                       AND COLUMN_NAME = 'Id' 
                                                                       AND DATA_TYPE = 'int'
                                                               """, connection))
        {
            var fooIdColumnExists =
                (int)await checkFooStructureCmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, fooIdColumnExists);
        }

        // Verify Migrations table exists
        await using (var checkMigrationsTableCmd = new SqlCommand("""
                                                                          SELECT COUNT(*) 
                                                                          FROM INFORMATION_SCHEMA.TABLES 
                                                                          WHERE TABLE_SCHEMA = 'IntegrationTests' 
                                                                          AND TABLE_NAME = 'TestMigrations'
                                                                  """, connection))
        {
            var migrationsTableExists =
                (int)await checkMigrationsTableCmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, migrationsTableExists);
        }

        // Verify migration entry exists with correct values
        await using (var checkMigrationEntryCmd = new SqlCommand("""
                                                                         SELECT COUNT(*) 
                                                                         FROM [IntegrationTests].[TestMigrations] 
                                                                         WHERE [Index] = 1 
                                                                         AND [Name] = 'CreateTable'
                                                                 """, connection))
        {
            var migrationEntryExists =
                (int)await checkMigrationEntryCmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, migrationEntryExists);
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        var serviceCollection = new ServiceCollection();
        serviceCollection.MigrationsSetup<TestMigrations>(new FilesystemMigrationSource("./Common"), opts =>
            opts.UseMsSql(
                new MsSqlMigrationConfiguration
                {
                    ConnectionString = _msSqlContainer.GetConnectionString(),
                    SchemaName = "IntegrationTests",
                    Mode = MigrationMode.Migrate,
                    MigrationTableName = "TestMigrations"
                }));

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }
}