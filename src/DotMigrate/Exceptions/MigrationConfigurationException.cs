using System;

namespace DotMigrate.Exceptions;

public class MigrationConfigurationException : Exception
{
    public MigrationConfigurationException() { }

    public MigrationConfigurationException(string message)
        : base(message) { }

    public MigrationConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
