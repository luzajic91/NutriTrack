namespace NutriTrack.Core.Auth;

public class CurrentUserService
{
    public int UserId { get; }
    public string Role { get; }

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User
            ?? throw new ForbiddenException("No authenticated user found.");

        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new ForbiddenException("User ID claim missing.");

        UserId = int.Parse(idClaim);
        Role = user.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new ForbiddenException("Role claim missing.");
    }
}