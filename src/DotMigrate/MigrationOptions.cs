using DotMigrate.Abstractions;

namespace DotMigrate;

public class MigrationOptions<TMigrator>
{
    public required IMigrationSource Source { get; init; }
    public required IDatabaseProvider Provider { get; init; }
    public required AMigrationConfiguration Configuration { get; init; }
}