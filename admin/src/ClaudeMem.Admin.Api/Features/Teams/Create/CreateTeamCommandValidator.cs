using FluentValidation;

namespace ClaudeMem.Admin.Api.Features.Teams.Create;

internal sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MinimumLength(4)
            .MaximumLength(256);
    }
}
