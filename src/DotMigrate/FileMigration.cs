using DotMigrate.Abstractions;

namespace DotMigrate;

public class FileMigration : IMigration
{
    public required string Name { get; init; }
    public int Index { get; init; }
    public required string Command { get; init; }
}