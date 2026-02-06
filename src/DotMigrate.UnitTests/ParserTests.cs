using System;
using DotMigrate;
using DotMigrate.Exceptions;
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
            "-- +DotMigrate Up",
            "-- +DotMigrate BeginBlock",
            "CREATE TABLE Foo (Id INT);",
            "-- +DotMigrate EndBlock",
        };

        var migration = (FileMigration)FileMigrationParser.Parse(lines);

        Assert.Equal("CreateTable", migration.Name);
        Assert.Equal(1, migration.Index);
        Assert.Equal($"CREATE TABLE Foo (Id INT);{Environment.NewLine}", migration.Command);
    }

    [Fact]
    public void Parse_UpDownFileMigration_ParsesDown()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name AddColumn",
            "-- +DotMigrate Index 2",
            "-- +DotMigrate Up",
            "-- +DotMigrate BeginBlock",
            "ALTER TABLE Foo ADD Col INT;",
            "-- +DotMigrate EndBlock",
            "-- +DotMigrate Down",
            "-- +DotMigrate BeginBlock",
            "ALTER TABLE Foo DROP COLUMN Col;",
            "-- +DotMigrate EndBlock",
        };

        var migration = (UpDownFileMigration)FileMigrationParser.Parse(lines);

        Assert.Equal("AddColumn", migration.Name);
        Assert.Equal(2, migration.Index);
        Assert.Equal($"ALTER TABLE Foo ADD Col INT;{Environment.NewLine}", migration.Command);
        Assert.Equal(
            $"ALTER TABLE Foo DROP COLUMN Col;{Environment.NewLine}",
            migration.DownCommand
        );
    }

    [Fact]
    public void Parse_MissingName_Throws()
    {
        var lines = new[]
        {
            "-- +DotMigrate Index 3",
            "-- +DotMigrate Up",
            "-- +DotMigrate BeginBlock",
            "SELECT 1;",
            "-- +DotMigrate EndBlock",
        };

        Assert.Throws<FileMigrationException>(() => FileMigrationParser.Parse(lines));
    }

    [Fact]
    public void Parse_MissingIndex_Throws()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name NoIndex",
            "-- +DotMigrate Up",
            "-- +DotMigrate BeginBlock",
            "SELECT 1;",
            "-- +DotMigrate EndBlock",
        };

        Assert.Throws<FileMigrationException>(() => FileMigrationParser.Parse(lines));
    }

    [Fact]
    public void Parse_MissingEndBlock_Throws()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name NoIndex",
            "-- +DotMigrate Up",
            "-- +DotMigrate BeginBlock",
            "SELECT 1;",
        };

        Assert.Throws<FileMigrationException>(() => FileMigrationParser.Parse(lines));
    }

    [Fact]
    public void Parse_MissingBeginBlock_Throws()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name NoIndex",
            "-- +DotMigrate Up",
            "SELECT 1;",
            "-- +DotMigrate EndBlock",
        };

        Assert.Throws<FileMigrationException>(() => FileMigrationParser.Parse(lines));
    }

    [Fact]
    public void Parse_MissingUpBeforeBeginBlock_Throws()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name NoIndex",
            "-- +DotMigrate BeginBlock",
            "SELECT 1;",
            "-- +DotMigrate EndBlock",
        };

        Assert.Throws<FileMigrationException>(() => FileMigrationParser.Parse(lines));
    }

    [Fact]
    public void Parse_MissingEndBlockBeforeDown_Throws()
    {
        var lines = new[]
        {
            "-- +DotMigrate Name AddColumn",
            "-- +DotMigrate Index 2",
            "-- +DotMigrate Up",
            "-- +DotMigrate BeginBlock",
            "ALTER TABLE Foo ADD Col INT;",
            "-- +DotMigrate Down",
            "ALTER TABLE Foo DROP COLUMN Col;",
            "-- +DotMigrate EndBlock",
        };

        Assert.Throws<FileMigrationException>(() => FileMigrationParser.Parse(lines));
    }
}
