namespace NutriTrack.Core.Features.Identity;

public record LoginCommand(string Email, string Password);

public record LoginResult(string AccessToken, string RefreshToken);

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}