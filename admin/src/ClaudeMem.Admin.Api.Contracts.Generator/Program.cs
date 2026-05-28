using System;
using System.IO;
using ClaudeMem.Admin.Api.Contracts.Generator;
using ClaudeMem.Admin.CodeGen.Client;
using ClaudeMem.Admin.CodeGen.NameGenerators;

// From bin/Debug/net10.0/, navigate up 5 levels to the admin/ repo root
var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

await new ClientGenerator(new ClientGeneratorOptions(
    @namespace: "ClaudeMem.Admin.Api.Contracts",
    inputSpecificationFileName: Path.Combine(repoRoot, "docs/openapi.yaml"),
    outputFilePath: Path.Combine(repoRoot, "src/ClaudeMem.Admin.Api.Contracts/Client/AdminApiClient.generated.cs"),
    useSystemTextJson: true,
    operationNameGenerator: OperationNameGenerator.MultipleControllersByTagWithPathSegmentsMethods,
    typesInclusionFactory: new AdminApiClientTypesInclusionFactory()
)).GenerateCode();
