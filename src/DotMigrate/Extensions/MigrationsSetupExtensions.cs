using System;
using DotMigrate.Abstractions;
using DotMigrate.Builder;
using DotMigrate.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotMigrate.Extensions;

public static class MigrationsSetupExtensions
{
    public static IHostApplicationBuilder MigrationsSetup<TMigrator>(this IHostApplicationBuilder builder,
        IMigrationSource source,
        Action<MigrationOptionsBuilder<TMigrator>> configuration)
    {
        var optionsBuilder = new MigrationOptionsBuilder<TMigrator>();
        configuration(optionsBuilder);
        optionsBuilder.WithSource(source);
        builder.Services.AddSingleton(optionsBuilder.Create());
        builder.Services.AddSingleton<IMigrator<TMigrator>, Migrator<TMigrator>>();

        return builder;
    }

    public static IHostApplicationBuilder MigrateOnStart<TMigrator>(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHostedService<MigrationsRunerService<TMigrator>>();
        return builder;
    }
}