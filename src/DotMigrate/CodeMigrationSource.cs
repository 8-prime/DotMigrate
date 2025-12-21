using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;

namespace DotMigrate;

public class CodeMigrationSource : IMigrationSource
{
    private readonly List<IMigration> _migrations = [];

    public IEnumerable<IMigration> GetMigrations()
    {
        return _migrations;
    }

    public Task<IEnumerable<IMigration>> GetMigrationsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(GetMigrations());
    }

    public IMigration? Latest()
    {
        return _migrations.OrderByDescending(m => m.Index).FirstOrDefault();
    }

    public Task<IMigration?> LatestAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Latest());
    }

    public void AddMigration(IMigration migration)
    {
        _migrations.Add(migration);
    }
}