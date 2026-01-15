using DotMigrate.Abstractions;

namespace DotMigrate;

public class MigrationOptions<TMigrator>
{
    public required IMigrationSource Source { get; init; }
    public required IMigrationDatabaseProvider Provider { get; init; }
    public required AMigrationConfiguration Configuration { get; init; }
}