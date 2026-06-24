using NutriTrack.Shared.Models.Auth;

namespace NutriTrack.Shared.Features.Identity;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
