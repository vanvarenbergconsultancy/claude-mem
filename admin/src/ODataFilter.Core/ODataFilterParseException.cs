using System;

namespace ODataFilter.Core;

/// <summary>Thrown when an OData filter or query expression cannot be parsed.</summary>
public sealed class ODataFilterParseException : Exception
{
    public ODataFilterParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
