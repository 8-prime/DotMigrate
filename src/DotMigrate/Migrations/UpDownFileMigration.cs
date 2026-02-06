using DotMigrate.Abstractions;

namespace DotMigrate.Migrations;

public class UpDownFileMigration : FileMigration, IUpDownMigration
{
    public string UpCommand => Command;
    public required string DownCommand { get; init; }
}
