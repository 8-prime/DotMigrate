using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotMigrate.Migrations;
using Xunit;

namespace DotMigrate.Tests;

public class FilesystemTests
{
    [Fact]
    public async Task FilesystemMigrationSource_ReadsMigrations()
    {
        var dir = Path.Combine(Path.GetTempPath(), "DotMigrateTests", Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "001_create.sql");
            await File.WriteAllTextAsync(
                file,
                "-- +DotMigrate Name Create;\n-- +DotMigrate Index 1\n-- +DotMigrate Up\nCREATE TABLE X;\n-- +DotMigrate BlockEnd\n"
            );

            var source = new FilesystemMigrationSource(dir);
            var migrations = source.GetMigrations().ToList();
            Assert.Single(migrations);
            Assert.Equal(1, migrations[0].Index);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FileSystemMigrationSource_ThrowsWhenDirectoryDoesNotExist()
    {
        var dir = Path.Combine(Path.GetTempPath(), "DotMigrateTests", Path.GetRandomFileName());
        Assert.Throws<DirectoryNotFoundException>(() => new FilesystemMigrationSource(dir));
    }

    [Fact]
    public async Task FilesystemMigrationSource_FindsLatestMigration()
    {
        var dir = Path.Combine(Path.GetTempPath(), "DotMigrateTests", Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "001_create.sql");
            await File.WriteAllTextAsync(
                file,
                "-- +DotMigrate Name Create;\n-- +DotMigrate Index 1\n-- +DotMigrate Up\nCREATE TABLE X;\n-- +DotMigrate BlockEnd\n"
            );

            var file2 = Path.Combine(dir, "002_create.sql");
            await File.WriteAllTextAsync(
                file,
                "-- +DotMigrate Name Update;\n-- +DotMigrate Index 2\n-- +DotMigrate Up\nCREATE TABLE Y;\n-- +DotMigrate BlockEnd\n"
            );

            var source = new FilesystemMigrationSource(dir);
            var migrations = source.GetMigrations().ToList();
            var latestMigration = await source.LatestAsync(CancellationToken.None);
            Assert.Equal(2, migrations.Count);
            Assert.NotNull(latestMigration);
            Assert.Equal("Update", latestMigration.Name);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
