using DotMigrate.Builder;

namespace DotMigrate.Databases.MsSql;

public static class SqlSetupMigrationsExtensions
{
    public static MigrationOptionsBuilder<TMigrator> UseMsSql<TMigrator>(
        this MigrationOptionsBuilder<TMigrator> builder,
        MsSqlMigrationConfiguration configuration)
    {
        builder.WithConfiguration(configuration);
        var provider = new MsSqlDatabaseProvider(configuration.ConnectionString, configuration);
        builder.WithProvider(provider);
        return builder;
    }
}