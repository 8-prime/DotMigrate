using DotMigrate.Abstractions;

namespace DotMigrate.Databases.MsSql;

public class MsSqlMigrationConfiguration : AMigrationConfiguration
{
    public string AppLockName { get; set; } = "SqlServerMigrationLock";
    public string SchemaName { get; set; } = "dbo";
    public required string DatabaseUser { get; set; }
}
