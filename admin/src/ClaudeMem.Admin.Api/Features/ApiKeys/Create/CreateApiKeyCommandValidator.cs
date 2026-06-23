using FluentValidation;

namespace ClaudeMem.Admin.Api.Features.ApiKeys.Create;

internal sealed class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(c => c.ActorId)
            .NotEmpty()
            .MaximumLength(256);
    }
}
