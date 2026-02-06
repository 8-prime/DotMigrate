using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;
using Microsoft.Extensions.Hosting;

namespace DotMigrate.Services;

public class MigrationsRunerService<TMigrator> : BackgroundService
{
    private readonly IMigrator<TMigrator> _migrator;

    public MigrationsRunerService(IMigrator<TMigrator> migrator)
    {
        _migrator = migrator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _migrator.RunAsync(stoppingToken);
    }
}
