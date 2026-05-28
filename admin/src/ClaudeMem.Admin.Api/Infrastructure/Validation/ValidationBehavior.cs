using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Infrastructure.Validation;

internal sealed class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IEnumerable<IValidator<TMessage>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TMessage>> validators)
    {
        _validators = validators;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var validationContext = new ValidationContext<TMessage>(message);
        var validationFailures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var validationResult = await validator.ValidateAsync(validationContext, cancellationToken);
            validationFailures.AddRange(validationResult.Errors);
        }

        if (validationFailures.Count > 0)
        {
            throw new ValidationException(validationFailures);
        }

        return await next(message, cancellationToken);
    }
}
