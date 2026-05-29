namespace NutriTrack.Shared.Features.Identity;

public class AuthService
{
    private const int RefreshTokenLifetimeDays = 7;

    private readonly NutriTrackDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly ILogger<AuthService> _logger;
    private readonly RegisterValidator _registerValidator;
    private readonly LoginValidator _loginValidator;
    private readonly RefreshTokenValidator _refreshTokenValidator;
    private readonly RevokeTokenValidator _revokeTokenValidator;

    public AuthService(
        NutriTrackDbContext db,
        JwtTokenService jwt,
        ILogger<AuthService> logger,
        RegisterValidator registerValidator,
        LoginValidator loginValidator,
        RefreshTokenValidator refreshTokenValidator,
        RevokeTokenValidator revokeTokenValidator)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _revokeTokenValidator = revokeTokenValidator;
    }

    public async Task<int> Register(RegisterCommand cmd, CancellationToken ct)
    {
        _registerValidator.ValidateAndThrow(cmd);

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == cmd.Email, ct);
        if (emailTaken)
            throw new FluentValidation.ValidationException("Email is already in use.");

        var userRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == "User", ct)
            ?? throw new NotFoundException("Default role not found.");

        var user = new User
        {
            Email = cmd.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
            RoleId = userRole.RoleId
        };

        _db.Add(user);
        await _db.SaveChangesAsync(ct);

        return user.UserId;
    }

    public async Task<LoginResult> Login(LoginCommand cmd, CancellationToken ct)
    {
        _loginValidator.ValidateAndThrow(cmd);

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == cmd.Email, ct)
            ?? throw new NotFoundException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
            throw new NotFoundException("Invalid email or password.");

        var accessToken = _jwt.GenerateAccessToken(user.UserId, user.Role.Name);
        var refreshToken = _jwt.GenerateRefreshToken();

        _db.Add(new RefreshToken
        {
            UserId = user.UserId,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays)
        });

        await _db.SaveChangesAsync(ct);

        return new LoginResult(accessToken, refreshToken);
    }

    public async Task<LoginResult> RefreshToken(RefreshTokenCommand cmd, CancellationToken ct)
    {
        _refreshTokenValidator.ValidateAndThrow(cmd);

        var existing = await _db.RefreshTokens
            .Include(r => r.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(r => r.Token == cmd.RefreshToken, ct)
            ?? throw new NotFoundException("Refresh token not found.");

        if (!existing.IsActive)
            throw new ForbiddenException("Refresh token is no longer active.");

        var newAccessToken = _jwt.GenerateAccessToken(
            existing.User.UserId, existing.User.Role.Name);
        var newRefreshToken = _jwt.GenerateRefreshToken();

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByToken = newRefreshToken;

        _db.Add(new RefreshToken
        {
            UserId = existing.UserId,
            Token = newRefreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays)
        });

        await _db.SaveChangesAsync(ct);

        return new LoginResult(newAccessToken, newRefreshToken);
    }

    public async Task RevokeToken(RevokeTokenCommand cmd, CancellationToken ct)
    {
        _revokeTokenValidator.ValidateAndThrow(cmd);

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == cmd.RefreshToken, ct)
            ?? throw new NotFoundException("Refresh token not found.");

        if (!token.IsActive)
            throw new ForbiddenException("Refresh token is already inactive.");

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
