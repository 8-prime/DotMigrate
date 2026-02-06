using System;
using System.Text;
using DotMigrate.Abstractions;
using DotMigrate.Migrations;

namespace DotMigrate;

public static class FileMigrationParser
{
    private const string DirectivePrefix = "+DotMigrate";

    public static IMigration Parse(string[] lines)
    {
        string? name = null;
        int? index = null;

        var upBuilder = new StringBuilder();
        var downBuilder = new StringBuilder();

        var inUp = false;
        var inDown = false;
        var inBlock = false;

        foreach (var rawLine in lines)
        {
            if (!TryParseDirective(rawLine, out var directive))
            {
                if (inBlock)
                {
                    var target = inUp ? upBuilder : downBuilder;
                    target.AppendLine(rawLine);
                }

                continue;
            }

            if (directive.StartsWith("Name ", StringComparison.OrdinalIgnoreCase))
            {
                name = directive["Name ".Length..].Trim();
                continue;
            }

            if (directive.StartsWith("Index ", StringComparison.OrdinalIgnoreCase))
            {
                index = int.Parse(directive["Index ".Length..].Trim());
                continue;
            }

            if (directive.Equals("Up", StringComparison.OrdinalIgnoreCase))
            {
                inUp = true;
                inDown = false;
                continue;
            }

            if (directive.Equals("Down", StringComparison.OrdinalIgnoreCase))
            {
                inUp = false;
                inDown = true;
                continue;
            }

            if (directive.Equals("BlockStart", StringComparison.OrdinalIgnoreCase))
            {
                // Default to only having up migration. Downs are not enforced
                if (!inUp && !inDown)
                {
                    inUp = true;
                    inDown = false;
                }

                inBlock = true;
                continue;
            }

            if (directive.Equals("BlockEnd", StringComparison.OrdinalIgnoreCase))
                inBlock = false;
        }

        if (name is null)
            throw new InvalidOperationException("Migration Name missing.");

        if (index is null)
            throw new InvalidOperationException("Migration Index missing.");

        if (downBuilder.Length > 0)
            return new UpDownFileMigration
            {
                Name = name,
                Index = index.Value,
                Command = upBuilder.Length > 0 ? upBuilder.ToString().Trim() : string.Empty,
                DownCommand = downBuilder.Length > 0 ? downBuilder.ToString().Trim() : string.Empty,
            };

        return new FileMigration
        {
            Name = name,
            Index = index.Value,
            Command = upBuilder.Length > 0 ? upBuilder.ToString().Trim() : string.Empty,
        };
    }

    private static bool TryParseDirective(string line, out string directive)
    {
        directive = string.Empty;

        var span = line.AsSpan().TrimStart();

        // Must start with SQL line comment
        if (!span.StartsWith("--", StringComparison.Ordinal))
            return false;

        span = span[2..].TrimStart();

        // Must start with +DotMigrate
        if (!span.StartsWith(DirectivePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        span = span[DirectivePrefix.Length..].TrimStart();

        directive = span.ToString();
        return true;
    }
}
