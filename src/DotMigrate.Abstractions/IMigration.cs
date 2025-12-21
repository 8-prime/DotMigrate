namespace DotMigrate.Abstractions
{
    public interface IMigration
    {
        string Name { get; }
        string Command { get; }
        int Index { get; }
    }
}