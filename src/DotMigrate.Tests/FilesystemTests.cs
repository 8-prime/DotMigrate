using System.IO;
using System.Linq;
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
            try
            {
                Directory.Delete(dir, true);
            }
            catch { }
        }
    }
}
