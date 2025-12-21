namespace DotMigrate.Abstractions;

public interface IUpDownMigration : IMigration
{
    string UpCommand { get; }
    string DownCommand { get; }
}