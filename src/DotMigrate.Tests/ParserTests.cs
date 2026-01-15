using System;
using DotMigrate;
using DotMigrate.Migrations;
using Xunit;

namespace DotMigrate.Tests;

public class ParserTests
{
    [Fact]
    public void Parse_FileMigration_ParsesNameIndexAndUp()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name CreateTable",
            "-- +DotMigrate Index 1",
            "-- +DotMigrate BlockStart",
            "-- +DotMigrate Up",
            "CREATE TABLE Foo (Id INT);",
            "-- +DotMigrate BlockEnd"
        };

        var migration = (FileMigration)FileMigrationParser.Parse(lines);

        Assert.Equal("CreateTable", migration.Name);
        Assert.Equal(1, migration.Index);
        Assert.Equal("CREATE TABLE Foo (Id INT);", migration.Command);
    }

    [Fact]
    public void Parse_UpDownFileMigration_ParsesDown()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name AddColumn",
            "-- +DotMigrate Index 2",
            "-- +DotMigrate BlockStart",
            "-- +DotMigrate Up",
            "ALTER TABLE Foo ADD Col INT;",
            "-- +DotMigrate Down",
            "ALTER TABLE Foo DROP COLUMN Col;",
            "-- +DotMigrate BlockEnd"
        };

        var migration = (UpDownFileMigration)FileMigrationParser.Parse(lines);

        Assert.Equal("AddColumn", migration.Name);
        Assert.Equal(2, migration.Index);
        Assert.Equal("ALTER TABLE Foo ADD Col INT;", migration.Command);
        Assert.Equal("ALTER TABLE Foo DROP COLUMN Col;", migration.DownCommand);
    }

    [Fact]
    public void Parse_MissingName_Throws()
    {
        var lines = new[]
        {
            "-- +DotMigrate Index 3",
            "-- +DotMigrate Up",
            "SELECT 1;",
            "-- +DotMigrate BlockEnd"
        };

        Assert.Throws<InvalidOperationException>(() => FileMigrationParser.Parse(lines));
    }

    [Fact]
    public void Parse_MissingIndex_Throws()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name NoIndex",
            "-- +DotMigrate Up",
            "SELECT 1;",
            "-- +DotMigrate BlockEnd"
        };

        Assert.Throws<InvalidOperationException>(() => FileMigrationParser.Parse(lines));
    }
}
