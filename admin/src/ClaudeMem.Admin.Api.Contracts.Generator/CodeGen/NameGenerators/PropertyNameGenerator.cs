using System.Globalization;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

/// <summary>
/// Converts snake_case JSON property names (e.g. my_property_name) to PascalCase C# names (MyPropertyName).
/// The built-in CSharpPropertyNameGenerator only capitalizes the first letter.
/// </summary>
public class PropertyNameGenerator : IPropertyNameGenerator
{
    private readonly CSharpPropertyNameGenerator _generator = new();

    public string Generate(JsonSchemaProperty property)
    {
        var name = _generator.Generate(property);
        return RemoveUnderscoresAndCapitalize(name);
    }

    private static string RemoveUnderscoresAndCapitalize(string name)
    {
        var parts = name.Split('_');
        for (var i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpper(parts[i][0], CultureInfo.InvariantCulture) + parts[i].Substring(1);
            }
        }
        return string.Join(string.Empty, parts);
    }
}
