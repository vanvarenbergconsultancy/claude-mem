
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ODataFilter.Core;
/// <summary>Fluent builder for <see cref="ODataFieldMap"/>.</summary>
public sealed class ODataFieldMapBuilder
{
    private readonly List<(Regex Pattern, string Replacement)> _mappings = [];

    /// <summary>
    /// Maps <paramref name="frontendName"/> to <paramref name="columnName"/> in filter/orderby expressions.
    /// The match is case-sensitive and word-boundary delimited so partial tokens are not replaced.
    /// </summary>
    public ODataFieldMapBuilder Map(string frontendName, string columnName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(frontendName);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

        // The alternation 'quoted-string'|identifier ensures the replacement never fires
        // inside single-quoted OData string constants (e.g. status eq 'name' must not
        // rewrite the value 'name' when "name" is a mapped field).
        var pattern = new Regex(
            $@"'(?:[^']|'')*'|\b{Regex.Escape(frontendName)}\b",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            TimeSpan.FromSeconds(1));

        _mappings.Add((pattern, columnName));

        return this;
    }

    /// <summary>Builds the <see cref="ODataFieldMap"/>.</summary>
    public ODataFieldMap Build() => new(_mappings.AsReadOnly());
}
