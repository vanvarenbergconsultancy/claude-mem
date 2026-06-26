using System;
using System.IO;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace ClaudeMem.Admin.Api.Tests.Unit.Schema;

public sealed class SchemaDriftTests
{
    private static string SchemaSourcePath => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../../../src/storage/postgres/schema.ts"));

    [Fact]
    public void CanonicalSchemaFile_ExistsAtExpectedPath()
    {
        // This repo is a fork — admin/ is a subdirectory, not a standalone checkout.
        // If this fails, schema.ts was moved in the origin and SchemaSourcePath must be updated.
        File.Exists(SchemaSourcePath).Should().BeTrue(
            $"schema.ts not found at '{SchemaSourcePath}'. " +
            "If it was moved in the origin repo, update SchemaSourcePath in SchemaDriftTests.");
    }

    [Fact]
    public void EmbeddedSchema_MatchesCanonicalTypeScriptSource()
    {
        var tsContent = File.ReadAllText(SchemaSourcePath);
        var match = Regex.Match(tsContent, @"PHASE_1_SCHEMA_SQL\s*=\s*`(?<sql>[^`]+)`", RegexOptions.Singleline | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(10));

        match.Success.Should().BeTrue("PHASE_1_SCHEMA_SQL constant must exist in schema.ts");

        var canonical = NormalizeSQL(match.Groups["sql"].Value);
        var embedded = NormalizeSQL(LoadEmbeddedSchema());

        embedded.Should().Be(canonical,
            "Infrastructure/schema.sql is out of sync with src/storage/postgres/schema.ts. " +
            "Update schema.sql to match PHASE_1_SCHEMA_SQL.");
    }

    private static string NormalizeSQL(string sql) =>
        Regex.Replace(sql.Trim(), @"\s+", " ", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(10));

    private static string LoadEmbeddedSchema()
    {
        var assembly = typeof(Program).Assembly;
        using var stream = assembly.GetManifestResourceStream("ClaudeMem.Admin.Api.Infrastructure.Database.schema.sql")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
