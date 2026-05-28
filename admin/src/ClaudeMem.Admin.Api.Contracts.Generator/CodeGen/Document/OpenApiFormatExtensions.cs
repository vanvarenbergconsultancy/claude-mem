using System;

namespace ClaudeMem.Admin.CodeGen.Document;

public static class OpenApiFormatExtensions
{
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
