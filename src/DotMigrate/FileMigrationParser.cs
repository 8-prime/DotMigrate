using System;
using System.Text;
using DotMigrate.Abstractions;
using DotMigrate.Exceptions;
using DotMigrate.Migrations;

namespace DotMigrate;

public class FileMigrationParser
{
    private enum ParseState
    {
        Start,
        Invalid,
        Up,
        InUpBlock,
        Down,
        InDownBlock,
    }

    private const string DirectivePrefix = "+DotMigrate";
    private const string NameDirective = "Name ";
    private const string IndexDirective = "Index ";
    private const string UpDirective = "Up";
    private const string DownDirective = "Down";
    private const string BeginBlockDirective = "BeginBlock";
    private const string EndBlockDirective = "EndBlock";

    public static IMigration Parse(string[] lines)
    {
        var mode = ParseState.Start;
        var upBuilder = new StringBuilder();
        var downBuilder = new StringBuilder();
        int? index = null;
        string? name = null;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var lineIsDirective = TryParseDirective(line, out var directive);
            switch (mode)
            {
                case ParseState.Start:
                    if (!lineIsDirective)
                    {
                        throw new FileMigrationException(
                            "Migration must specify directive in start state"
                        );
                    }

                    if (directive.StartsWith(IndexDirective, StringComparison.OrdinalIgnoreCase))
                    {
                        index = int.Parse(directive[IndexDirective.Length..].Trim());
                        continue;
                    }

                    if (directive.StartsWith(NameDirective, StringComparison.OrdinalIgnoreCase))
                    {
                        name = directive[NameDirective.Length..].Trim().ToString();
                        continue;
                    }

                    if (directive.StartsWith(UpDirective, StringComparison.OrdinalIgnoreCase))
                    {
                        mode = ParseState.Up;
                        continue;
                    }

                    if (directive.StartsWith(DownDirective, StringComparison.OrdinalIgnoreCase))
                    {
                        mode = ParseState.Down;
                        continue;
                    }

                    throw new FileMigrationException(
                        $"Invalid directive {directive} in start state"
                    );
                case ParseState.Invalid:
                    throw new FileMigrationException("Invalid directive");
                case ParseState.Up:
                    if (
                        !lineIsDirective
                        || !directive.Equals(
                            BeginBlockDirective,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        throw new FileMigrationException(
                            "Up directive must be followed with BeginBlock directive"
                        );
                    }

                    mode = ParseState.InUpBlock;
                    break;
                case ParseState.InUpBlock:
                    mode = HandleMigrationBlock(lineIsDirective, directive, line, upBuilder, mode);
                    break;
                case ParseState.Down:
                    if (
                        !lineIsDirective
                        || !directive.Equals(
                            BeginBlockDirective,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        throw new FileMigrationException(
                            "Down directive must be followed with BeginBlock directive"
                        );
                    }

                    mode = ParseState.InDownBlock;
                    break;
                case ParseState.InDownBlock:
                    mode = HandleMigrationBlock(
                        lineIsDirective,
                        directive,
                        line,
                        downBuilder,
                        mode
                    );
                    break;
                default:
                    throw new FileMigrationException("Parser reached invalid internal state");
            }
        }

        if (mode != ParseState.Start)
        {
            throw new FileMigrationException($"Migration was in state {mode} at end of file");
        }

        if (index is null)
        {
            throw new FileMigrationException("Migration index must be set at end of file");
        }

        if (name is null)
        {
            throw new FileMigrationException("Migration name must be set at end of file");
        }

        if (downBuilder.Length > 0)
        {
            return new UpDownFileMigration
            {
                DownCommand = downBuilder.ToString(),
                Command = upBuilder.ToString(),
                Name = name,
                Index = index.Value,
            };
        }

        return new FileMigration
        {
            Command = upBuilder.ToString(),
            Name = name,
            Index = index.Value,
        };
    }

    private static ParseState HandleMigrationBlock(
        bool isDirective,
        ReadOnlySpan<char> directive,
        string line,
        StringBuilder blockBuilder,
        ParseState state
    )
    {
        if (isDirective)
        {
            return directive.Equals(EndBlockDirective, StringComparison.OrdinalIgnoreCase)
                ? ParseState.Start
                : throw new FileMigrationException(
                    $"Only 'EndBlock' directive allowed within blocks. Used forbidden directive: {directive}"
                );
        }

        blockBuilder.AppendLine(line);
        return state;
    }

    private static bool TryParseDirective(ReadOnlySpan<char> line, out ReadOnlySpan<char> directive)
    {
        var span = line.TrimStart();

        // Must start with SQL line comment
        if (!span.StartsWith("--", StringComparison.Ordinal))
        {
            directive = Span<char>.Empty;
            return false;
        }

        span = span[2..].TrimStart();

        // Must start with +DotMigrate
        if (!span.StartsWith(DirectivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            directive = Span<char>.Empty;
            return false;
        }

        directive = span[DirectivePrefix.Length..].TrimStart();
        return true;
    }
}
