namespace ODataFilter.Core.Tests.Unit;

using System;

using AwesomeAssertions;
using AwesomeAssertions.Execution;

using ODataFilter.Core;

using SqlKata.Compilers;

using Xunit;

/// <summary>Regression tests — one test per bug fixed in the absorbed DynamicODataToSQL source.</summary>
/// <remarks>
/// Issue numbers correspond to the upstream GitHub tracker at
/// https://github.com/DynamicODataToSQL/DynamicODataToSQL/issues/
/// Additional fixes not present in the upstream issue tracker are documented inline.
/// </remarks>
public sealed class DynamicODataToSqlRegressionTests
{
    private static readonly ODataSqlTranslator Sut = new(new PostgresCompiler());

    // ── Fix #59: SqlKata 4.x compatibility ──────────────────────────────────

    [Fact]
    public void SqlKata4x_Compat_TranslatorConstructsWithPostgresCompiler()
    {
        var translator = new ODataSqlTranslator(new PostgresCompiler());

        translator.Translate("t", ODataQueryOptions.Empty).Sql.Should().NotBeEmpty();
    }

    [Fact]
    public void SqlKata4x_Compat_TranslationRoundTripProducesValidSql()
    {
        var options = new ODataQueryOptions { Filter = "name eq 'Acme' and active eq true" };

        var result = Sut.Translate("teams", options);

        using var scope = new AssertionScope();
        result.Sql.Should().StartWith("SELECT");
        result.Parameters.Should().NotBeEmpty();
    }

    // ── Fix #58: hex-encoded field names beyond _x0020_ ──────────────────────

    [Fact]
    public void HexEncodedFieldName_SingleHexSequence_IsDecoded()
    {
        // _x0032_ = Unicode U+0032 = '2', so "col_x0032_" should become "col2"
        var options = new ODataQueryOptions { Filter = "col_x0032_ eq 1" };

        var result = Sut.Translate("table", options);

        result.Sql.Should().Contain("\"col2\"");
    }

    [Fact]
    public void HexEncodedFieldName_SpaceSequence_IsDecoded()
    {
        // _x0020_ = Unicode U+0020 = ' ' (space) — the original single case that was supported
        // This ensures the general regex still handles the legacy space case
        var options = new ODataQueryOptions { Filter = "spaced_x0020_col eq 'x'" };

        var result = Sut.Translate("table", options);

        result.Sql.Should().Contain("\"spaced col\"");
    }

    // ── Fix #52: right-side property access (column-to-column comparisons) ───

    [Fact]
    public void ColumnToColumnComparison_NeOperator_EmitsBothColumnsWithoutParameter()
    {
        var options = new ODataQueryOptions { Filter = "startId ne endId" };

        var result = Sut.Translate("ranges", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("\"startId\"");
        result.Sql.Should().Contain("\"endId\"");
        result.Sql.Should().Contain("<>");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ColumnToColumnComparison_EqOperator_EmitsBothColumns()
    {
        var options = new ODataQueryOptions { Filter = "foreignKey eq primaryKey" };

        var result = Sut.Translate("items", options);

        using var scope = new AssertionScope();
        result.Sql.Should().Contain("\"foreignKey\"");
        result.Sql.Should().Contain("\"primaryKey\"");
        result.Parameters.Should().BeEmpty();
    }

    // ── Fix #46: leading/trailing spaces in string constants are preserved ───

    [Fact]
    public void StringConstant_TrailingSpace_IsPreservedInParameter()
    {
        var options = new ODataQueryOptions { Filter = "name eq 'Test '" };

        var result = Sut.Translate("items", options);

        result.Parameters.Values.Should().ContainSingle(v => v is string && (string)v == "Test ");
    }

    [Fact]
    public void StringConstant_LeadingSpace_IsPreservedInParameter()
    {
        var options = new ODataQueryOptions { Filter = "name eq ' Test'" };

        var result = Sut.Translate("items", options);

        result.Parameters.Values.Should().ContainSingle(v => v is string && (string)v == " Test");
    }

    // ── Fix #14: column name quote-stripping (defensive) ────────────────────

    [Fact]
    public void PropertyName_WithoutQuotes_EmitsColumnCorrectly()
    {
        // Baseline: normal unquoted property names should always pass through cleanly
        var options = new ODataQueryOptions { Filter = "name eq 'Acme'" };

        var result = Sut.Translate("items", options);

        result.Sql.Should().Contain("\"name\"");
    }

    // ── Fix #42: ODataFilterParseException wraps parser construction failures ─

    [Fact]
    public void InvalidColumnName_WithTilde_ThrowsODataFilterParseException()
    {
        // Column names containing '~' are not valid OData identifiers.
        // The OData parser throws ODataException; we wrap it as ODataFilterParseException.
        var options = new ODataQueryOptions { Filter = "inva~lid eq 1" };

        var action = () => Sut.Translate("table", options);

        action.Should().Throw<ODataFilterParseException>();
    }

    // ── Additional fix: ODataFilterParseException wraps ParseFilter() failures

    [Fact]
    public void ParseFilterFailure_WrappedInODataFilterParseException()
    {
        // A filter with a malformed expression (unclosed parenthesis) should
        // throw ODataFilterParseException, not a raw ODataException.
        // This covers the try/catch added around parser.ParseFilter() and siblings,
        // distinct from the parser construction try/catch for fix #42.
        var options = new ODataQueryOptions { Filter = "name eq 'unclosed" };

        var action = () => Sut.Translate("table", options);

        action.Should().Throw<ODataFilterParseException>();
    }

    // ── Fix #33: improved $top error message ────────────────────────────────
    // Note: ODataQueryOptions.Top is int? so the public API cannot produce a
    // non-integer $top string. This fix is exercised only by callers who use the
    // internal ODataToSqlConverter directly with a dictionary — not testable here.

    // ── Fix #41: thread-safety (stateless per call) ──────────────────────────

    [Fact]
    public void Translate_CalledConcurrently_DoesNotThrow()
    {
        // Verifies that a single ODataSqlTranslator instance can be called from
        // multiple threads simultaneously (stateless — new parser per call).
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var filters = new[]
        {
            "name eq 'A'", "count gt 0", "active eq true", "name eq 'B'",
            "count lt 100", "active eq false", "name eq 'C'", "count ge 5"
        };

        System.Threading.Tasks.Parallel.ForEach(filters, filter =>
        {
            try
            {
                Sut.Translate("table", new ODataQueryOptions { Filter = filter });
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        exceptions.Should().BeEmpty();
    }
}
