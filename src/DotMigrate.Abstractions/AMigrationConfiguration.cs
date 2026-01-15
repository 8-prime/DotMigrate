using System;

namespace DotMigrate.Abstractions;

public abstract class AMigrationConfiguration
{
    public required string ConnectionString { get; set; }
    public string MigrationTableName { get; set; } = "_migrationHistory";
    public TimeSpan LockTimeOut { get; set; } = TimeSpan.FromSeconds(30);
    public MigrationMode Mode { get; set; } = MigrationMode.Migrate;
    public int? ToVersion { get; set; }
}