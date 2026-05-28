using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp;

namespace ClaudeMem.Admin.CodeGen;

/// <summary>
/// Extends the NSwag DefaultTemplateFactory to look in our own assembly for embedded templates
/// before falling back to the built-in NSwag templates.
/// </summary>
public class CustomEmbeddedTemplateFactory : NSwag.CodeGeneration.DefaultTemplateFactory
{
    private readonly Assembly _assemblyContainingEmbeddedResources;

    private static readonly Assembly[] NSwagAssembliesForDefaultTemplates =
    [
        typeof(CSharpGeneratorSettings).GetTypeInfo().Assembly,
        typeof(CSharpGeneratorBaseSettings).GetTypeInfo().Assembly
    ];

    public CustomEmbeddedTemplateFactory(CodeGeneratorSettingsBase settings, Assembly assemblyContainingEmbeddedResources)
        : base(settings, NSwagAssembliesForDefaultTemplates)
    {
        _assemblyContainingEmbeddedResources = assemblyContainingEmbeddedResources;
    }

    protected override string GetEmbeddedLiquidTemplate(string language, string template)
    {
        var customTemplate = GetCustomEmbeddedTemplate(template);
        return !string.IsNullOrEmpty(customTemplate)
            ? customTemplate
            : base.GetEmbeddedLiquidTemplate(language, template);
    }

    private string? GetCustomEmbeddedTemplate(string templateName)
    {
        var resourceName = FindEmbeddedResourceName($"{templateName}.liquid");
        if (resourceName == null)
        {
            return null;
        }

        using var stream = _assemblyContainingEmbeddedResources.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private string? FindEmbeddedResourceName(string fileName)
    {
        return _assemblyContainingEmbeddedResources
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
    }
}
