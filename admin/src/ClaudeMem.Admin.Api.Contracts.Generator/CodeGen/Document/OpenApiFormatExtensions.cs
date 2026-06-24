using System;

namespace ClaudeMem.Admin.CodeGen.Document;

/// <summary>Extension methods for deriving <see cref="OpenApiFormat"/> from file paths.</summary>
public static class OpenApiFormatExtensions
{
    /// <summary>Detects the OpenAPI format from the file extension (<c>.json</c>, <c>.yaml</c>, or <c>.yml</c>). Throws for unrecognised extensions.</summary>
    public static OpenApiFormat GetFormat(string path)
    {
        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return OpenApiFormat.Json;
        }

        if (path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
        {
            return OpenApiFormat.Yaml;
        }

        throw new NotSupportedException("File extension must be json, yaml or yml.");
    }
}
