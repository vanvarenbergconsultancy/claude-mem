
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ODataFilter.Core;
/// <summary> Maps frontend field names to their database column names by preprocessing the OData filter and orderby strings before they reach the OData parser. </summary>
/// <remarks>
/// Preprocessing happens at string level (word-boundary replacement) before the OData parser sees the query.
/// This means field names with special characters that are not valid OData identifiers can be safely aliased here.
/// Use <see cref="ODataFieldMapBuilder"/> via <see cref="Create"/> to construct an instance.
/// </remarks>
public sealed class ODataFieldMap
{
    private readonly IReadOnlyList<(Regex Pattern, string Replacement)> _mappings;

    internal ODataFieldMap(IReadOnlyList<(Regex, string)> mappings)
    {
        _mappings = mappings;
    }

    /// <summary>Creates a new <see cref="ODataFieldMapBuilder"/> to configure field name mappings.</summary>
    public static ODataFieldMapBuilder Create() => new();

    /// <summary>
    /// Applies all configured field name mappings to the filter and orderby strings in <paramref name="options"/>.
    /// </summary>
    public ODataQueryOptions Apply(ODataQueryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_mappings.Count == 0)
        {
            return options;
        }

        return options with
        {
            Filter = ApplyMappings(options.Filter),
            OrderBy = ApplyMappings(options.OrderBy),
        };
    }

    private string? ApplyMappings(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        foreach (var (pattern, replacement) in _mappings)
        {
            // Quoted-string matches (from the alternation in ODataFieldMapBuilder) are
            // returned unchanged; only the identifier alternative gets replaced.
            input = pattern.Replace(input, m => m.Value[0] == '\'' ? m.Value : replacement);
        }

        return input;
    }
}

