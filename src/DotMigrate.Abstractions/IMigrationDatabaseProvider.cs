using System.Threading;
using System.Threading.Tasks;

namespace DotMigrate.Abstractions;

public interface IMigrationDatabaseProvider
{
    void GetLock();
    Task GetLockAsync(CancellationToken cancellationToken = default);
    void ReleaseLock();
    Task ReleaseLockAsync(CancellationToken cancellationToken = default);
    int? GetVersion();
    Task<int?> GetVersionAsync(CancellationToken cancellationToken = default);
    void ApplyMigration(IMigration migration);
    Task ApplyMigrationAsync(IMigration migration, CancellationToken cancellationToken = default);
}
