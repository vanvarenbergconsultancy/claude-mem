using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace ClaudeMem.Admin.CodeGen;

/// <summary>Base class for NSwag-backed code generators; handles loading, generating, and saving output files.</summary>
public abstract class GeneratorBase
{
    private readonly BaseGeneratorOptions _generatorOptions;

    protected GeneratorBase(BaseGeneratorOptions generatorOptions)
    {
        _generatorOptions = generatorOptions;
    }

    /// <summary>Runs the full generate-and-save pipeline.</summary>
    public virtual async Task GenerateCode(CancellationToken cancellationToken = default)
    {
        CreateOutputDirectoryIfNotYetExists();
        var openApiDocument = await LoadOpenApiDocument(cancellationToken).ConfigureAwait(false);

        var generator = CreateGenerator(openApiDocument);
        var code = generator.GenerateFile();

        await Save(code, cancellationToken).ConfigureAwait(false);
    }

    protected void CreateOutputDirectoryIfNotYetExists()
    {
        var directoryPath = Path.GetDirectoryName(_generatorOptions.OutputFilePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath!);
        }
    }

    protected virtual Task<OpenApiDocument> LoadOpenApiDocument(CancellationToken cancellationToken = default)
    {
        var path = _generatorOptions.InputSpecificationFileName;
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path);
        }

        return _generatorOptions.OpenApiDocumentLoader.LoadOpenApiDocument(path, cancellationToken);
    }

    protected abstract CSharpGeneratorBase CreateGenerator(OpenApiDocument openApiDocument);

    protected virtual Task Save(string code, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(_generatorOptions.OutputFilePath, code, cancellationToken);
}
