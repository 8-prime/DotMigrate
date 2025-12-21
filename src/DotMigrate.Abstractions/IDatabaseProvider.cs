using System.Threading;
using System.Threading.Tasks;

namespace DotMigrate.Abstractions;

public interface IDatabaseProvider
{
    void GetLock();
    Task GetLockAsync(CancellationToken cancellationToken = default);
    void ReleaseLock();
    Task ReleaseLockAsync(CancellationToken cancellationToken = default);
    string GetVersion();
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
    void ApplyMigration(IMigration migration);
    Task ApplyMigrationAsync(IMigration migration, CancellationToken cancellationToken = default);
}