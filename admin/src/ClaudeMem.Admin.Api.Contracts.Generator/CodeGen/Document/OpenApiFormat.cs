namespace ClaudeMem.Admin.CodeGen.Document;

/// <summary>Specifies the serialisation format of an OpenAPI document file.</summary>
public enum OpenApiFormat
{
    /// <summary>Format has not been specified; the loader will attempt auto-detection.</summary>
    Undefined,

    /// <summary>JSON-serialised OpenAPI document.</summary>
    Json,

    /// <summary>YAML-serialised OpenAPI document.</summary>
    Yaml
}
