using System;

namespace DotMigrate.Exceptions;

public class FileMigrationException : Exception
{
    public FileMigrationException()
        : base() { }

    public FileMigrationException(string message)
        : base(message) { }

    public FileMigrationException(string message, Exception innerException)
        : base(message, innerException) { }
}
