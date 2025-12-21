using DotMigrate.Abstractions;

namespace DotMigrate;

public class UpDownFileMigration : FileMigration, IUpDownMigration
{
    public string UpCommand => Command;
    public string DownCommand { get; init; }
}