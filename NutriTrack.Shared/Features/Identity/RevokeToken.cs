namespace NutriTrack.Shared.Features.Identity;

public record RevokeTokenCommand(string RefreshToken);

public class RevokeTokenValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
