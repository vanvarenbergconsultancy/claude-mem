using System;
using System.Globalization;
using System.Text;
using Microsoft.CSharp;
using NSwag;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

public static class NameSanitizer
{
    public static string SanitizeApiTitle(OpenApiDocument document)
    {
        var title = document.Info.Title;
        var sanitized = MakeSafeClassName(title);
        sanitized = CapitalizeFirst(sanitized);
        sanitized = StripApiSuffix(sanitized);
        return sanitized;
    }

    public static string CapitalizeFirst(string part)
        => char.ToUpper(part[0], CultureInfo.InvariantCulture) + part.Substring(1);

    public static string StripApiSuffix(string part)
        => part.EndsWith("Api", StringComparison.OrdinalIgnoreCase) ? part.Substring(0, part.Length - 3) : part;

    public static string MakeSafeClassName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        var sanitized = BuildValidIdentifier(NormalizeInput(input), true);
        return IsKeyword(sanitized) ? $"{sanitized}Class" : sanitized;
    }

    public static string MakeSafeParameterName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        var sanitized = BuildValidIdentifier(NormalizeInput(input), false);
        return IsKeyword(sanitized) ? $"@{sanitized}" : sanitized;
    }

    private static string NormalizeInput(string input) => input.Normalize(NormalizationForm.FormD);

    private static string BuildValidIdentifier(string input, bool capitalizeFirstLetter)
    {
        var builder = new StringBuilder();
        var capitalizeNext = capitalizeFirstLetter;

        foreach (var c in input)
        {
            if (char.IsLetter(c))
            {
                builder.Append(capitalizeNext ? char.ToUpper(c, CultureInfo.InvariantCulture) : c);
                capitalizeNext = false;
            }
            else if (char.IsDigit(c) && builder.Length > 0)
            {
                builder.Append(c);
                capitalizeNext = false;
            }
            else if (c == '-' || c == '_' || char.IsWhiteSpace(c))
            {
                capitalizeNext = true;
            }
        }

        if (builder.Length == 0)
        {
            throw new ArgumentException("Input did not contain a valid string after sanitizing.", nameof(input));
        }

        return builder.ToString();
    }

    private static bool IsKeyword(string identifier)
    {
        using var provider = new CSharpCodeProvider();
        return !provider.IsValidIdentifier(identifier);
    }
}
