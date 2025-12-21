using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DotMigrate.Services;

public class MigrationsRunerService<TMigrator> : BackgroundService
{
    private readonly Migrator<TMigrator> _migrator;

    public MigrationsRunerService(Migrator<TMigrator> migrator)
    {
        _migrator = migrator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _migrator.RunAsync(stoppingToken);
    }
}