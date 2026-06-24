using NutriTrack.Shared.Models.Auth;

namespace NutriTrack.Shared.Features.Identity;

public class RevokeTokenValidator : AbstractValidator<RevokeTokenRequest>
{
    public RevokeTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
