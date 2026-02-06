using System.Threading;
using System.Threading.Tasks;

namespace DotMigrate.Abstractions;

public interface IMigrator<TMigrator>
{
    public void Run();
    public Task RunAsync(CancellationToken cancellationToken = default);
    public bool AllMigrationsApplied();
    public Task<bool> AllMigrationsAppliedAsync(CancellationToken cancellationToken = default);
    public void MigrateOutstanding();
    public Task MigrateOutstandingAsync(CancellationToken cancellationToken = default);
    public void MigrateToVersion(int version);
    public Task MigrateToVersionAsync(int version, CancellationToken cancellationToken = default);
}
