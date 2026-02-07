using System;
using System.Linq;
using DotMigrate.Abstractions;
using DotMigrate.Builder;
using DotMigrate.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotMigrate.Extensions;

public static class MigrationsSetupExtensions
{
    public static IServiceCollection MigrationsSetup<TMigrator>(
        this IServiceCollection services,
        IMigrationSource source,
        Action<MigrationOptionsBuilder<TMigrator>> configuration
    )
    {
        if (services.All(sd => sd.ServiceType != typeof(MigrationsRunerService)))
        {
            services.AddHostedService<MigrationsRunerService>();
        }

        var optionsBuilder = new MigrationOptionsBuilder<TMigrator>();
        configuration(optionsBuilder);
        optionsBuilder.WithSource(source);
        services.AddSingleton(optionsBuilder.Create());
        services.AddSingleton<IMigrator<TMigrator>, Migrator<TMigrator>>();

        return services;
    }
}
