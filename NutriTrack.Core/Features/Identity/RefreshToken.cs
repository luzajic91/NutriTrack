namespace NutriTrack.Core.Features.Identity;

public record RefreshTokenCommand(string RefreshToken);

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}