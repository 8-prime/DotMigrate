using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Abstractions;

namespace DotMigrate.Migrations;

public class FilesystemMigrationSource : IMigrationSource
{
    private readonly string _path;
    private List<IMigration>? _migrations;

    public FilesystemMigrationSource(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);

        _path = path;
    }

    public IEnumerable<IMigration> GetMigrations()
    {
        if (_migrations is not null)
            return _migrations;

        _migrations = [];
        foreach (var file in Directory.EnumerateFiles(_path, "*.sql*", SearchOption.AllDirectories))
            _migrations.Add(FileMigrationParser.Parse(File.ReadAllLines(file)));

        return _migrations;
    }

    public async Task<IEnumerable<IMigration>> GetMigrationsAsync(
        CancellationToken cancellationToken
    )
    {
        if (_migrations is not null)
            return _migrations;

        _migrations = [];
        foreach (var file in Directory.EnumerateFiles(_path, "*.sql*", SearchOption.AllDirectories))
            _migrations.Add(
                FileMigrationParser.Parse(await File.ReadAllLinesAsync(file, cancellationToken))
            );

        return _migrations;
    }

    public IMigration? Latest()
    {
        if (_migrations is null)
            GetMigrations();

        return _migrations?.OrderByDescending(m => m.Index).FirstOrDefault();
    }

    public async Task<IMigration?> LatestAsync(CancellationToken cancellationToken)
    {
        if (_migrations is null)
            await GetMigrationsAsync(cancellationToken);

        return _migrations?.OrderByDescending(m => m.Index).FirstOrDefault();
    }
}
