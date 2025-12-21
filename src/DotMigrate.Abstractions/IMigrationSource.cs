using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotMigrate.Abstractions;

public interface IMigrationSource
{
    public IEnumerable<IMigration> GetMigrations();
    public Task<IEnumerable<IMigration>> GetMigrationsAsync(CancellationToken cancellationToken);
    public IMigration? Latest();
    public Task<IMigration?> LatestAsync(CancellationToken cancellationToken);
}