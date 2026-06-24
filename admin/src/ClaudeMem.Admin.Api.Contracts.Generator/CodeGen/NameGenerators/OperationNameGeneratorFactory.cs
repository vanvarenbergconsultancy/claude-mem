using NSwag.CodeGeneration.OperationNameGenerators;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

/// <summary>Resolves an <see cref="IOperationNameGenerator"/> implementation from an <see cref="OperationNameGenerator"/> enum value.</summary>
public static class OperationNameGeneratorFactory
{
    /// <summary>Returns the NSwag operation-name generator that corresponds to the given strategy.</summary>
    public static IOperationNameGenerator Get(OperationNameGenerator operationNameGenerator)
    {
        return operationNameGenerator switch
        {
            OperationNameGenerator.SingleControllerWithPathSegmentsMethods =>
                new OperationNameGeneratorSingleControllerWithPathSegmentsMethods(),
            OperationNameGenerator.MultipleControllersByTagWithPathSegmentsMethods =>
                new OperationNameGeneratorMultipleControllersByTagWithPathSegmentsMethods(),
            OperationNameGenerator.MultipleControllersByPathSegmentsWithOperationNameMethods =>
                new OperationNameGeneratorMultipleControllersByPathSegmentsWithOperationNameMethods(),
            _ => new OperationNameGeneratorMultipleControllersByTagWithPathSegmentsMethods()
        };
    }
}
