using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;
using Microsoft.Extensions.Hosting;

namespace DotMigrate.Services;

public class MigrationsRunerService : BackgroundService
{
    private readonly IMigrator[] _migrators;

    public MigrationsRunerService(IMigrator[] migrators)
    {
        _migrators = migrators;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var exceptions = new List<Exception>();

        foreach (var migrator in _migrators)
        {
            try
            {
                await migrator.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count != 0)
        {
            throw new AggregateException("One or more migrations failed", exceptions);
        }
    }
}
