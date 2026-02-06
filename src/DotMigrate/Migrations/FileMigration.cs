using DotMigrate.Abstractions;

namespace DotMigrate.Migrations;

public class FileMigration : IMigration
{
    public required string Name { get; init; }
    public int Index { get; init; }
    public required string Command { get; init; }
}
