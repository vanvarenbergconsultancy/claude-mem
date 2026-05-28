using NSwag.CodeGeneration.OperationNameGenerators;

namespace ClaudeMem.Admin.CodeGen.NameGenerators;

public static class OperationNameGeneratorFactory
{
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
