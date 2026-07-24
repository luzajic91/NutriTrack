using NutriTrack.Shared.Email;
using NutriTrack.Shared.Models.Auth;

namespace NutriTrack.Shared.Features.Identity;

public class AuthService
{
    private const int RefreshTokenLifetimeDays = 7;
    private const int EmailConfirmationLifetimeHours = 24;

    private readonly NutriTrackDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly RegisterValidator _registerValidator;
    private readonly LoginValidator _loginValidator;
    private readonly RefreshTokenValidator _refreshTokenValidator;
    private readonly RevokeTokenValidator _revokeTokenValidator;
    private readonly ConfirmEmailValidator _confirmEmailValidator;

    public AuthService(
        NutriTrackDbContext db,
        JwtTokenService jwt,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        RegisterValidator registerValidator,
        LoginValidator loginValidator,
        RefreshTokenValidator refreshTokenValidator,
        RevokeTokenValidator revokeTokenValidator,
        ConfirmEmailValidator confirmEmailValidator)
    {
        _db = db;
        _jwt = jwt;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _revokeTokenValidator = revokeTokenValidator;
        _confirmEmailValidator = confirmEmailValidator;
    }

    public async Task<int> Register(RegisterRequest cmd, CancellationToken ct)
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
            RoleId = userRole.RoleId,
            EmailConfirmed = false
        };

        _db.Add(user);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} registered", user.UserId);
        return user.UserId;
    }

    public async Task ConfirmEmail(ConfirmEmailRequest cmd, CancellationToken ct)
    {
        _confirmEmailValidator.ValidateAndThrow(cmd);

        var token = await _db.EmailConfirmationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == cmd.Token, ct)
            ?? throw new NotFoundException("Invalid confirmation link.");

        if (!token.IsActive)
            throw new ForbiddenException("Confirmation link has expired or already been used.");

        token.User.EmailConfirmed = true;
        token.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Email confirmed for user {UserId}", token.UserId);
    }

    private async Task SendConfirmationEmailAsync(User user, CancellationToken ct)
    {
        // Hex is URL-safe, so the raw token drops straight into the query string.
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        _db.Add(new EmailConfirmationToken
        {
            UserId = user.UserId,
            Token = rawToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(EmailConfirmationLifetimeHours)
        });
        await _db.SaveChangesAsync(ct);

        var clientBaseUrl = (_configuration["App:ClientBaseUrl"] ?? string.Empty).TrimEnd('/');
        var link = $"{clientBaseUrl}/confirm-email?token={rawToken}";

        var body =
            $"""
            <p>Welcome to NutriTrack!</p>
            <p>Please confirm your email address by clicking the link below:</p>
            <p><a href="{link}">Confirm my email</a></p>
            <p>This link expires in {EmailConfirmationLifetimeHours} hours. If you didn't create an account, you can ignore this email.</p>
            """;

        await _emailSender.SendAsync(user.Email, "Confirm your NutriTrack account", body, ct);
    }

    public async Task<AuthTokensDto> Login(LoginRequest cmd, CancellationToken ct)
    {
        _loginValidator.ValidateAndThrow(cmd);

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == cmd.Email, ct);

        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for {Email} (no such user)", cmd.Email);
            throw new NotFoundException("Invalid email or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user {UserId} (bad password)", user.UserId);
            throw new NotFoundException("Invalid email or password.");
        }

        if (!user.EmailConfirmed)
            throw new ForbiddenException("Please confirm your email before logging in.");

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

        _logger.LogInformation("User {UserId} logged in", user.UserId);
        return new AuthTokensDto { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public async Task<AuthTokensDto> RefreshToken(RefreshTokenRequest cmd, CancellationToken ct)
    {
        _refreshTokenValidator.ValidateAndThrow(cmd);

        var existing = await _db.RefreshTokens
            .Include(r => r.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(r => r.Token == cmd.RefreshToken, ct)
            ?? throw new NotFoundException("Refresh token not found.");

        if (!existing.IsActive)
        {
            _logger.LogWarning(
                "Refresh attempted with inactive token {Token} for user {UserId}",
                LogMasking.Mask(existing.Token), existing.UserId);
            throw new ForbiddenException("Refresh token is no longer active.");
        }

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

        _logger.LogInformation("Access token refreshed for user {UserId}", existing.UserId);
        return new AuthTokensDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
    }

    public async Task RevokeToken(RevokeTokenRequest cmd, CancellationToken ct)
    {
        _revokeTokenValidator.ValidateAndThrow(cmd);

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == cmd.RefreshToken, ct)
            ?? throw new NotFoundException("Refresh token not found.");

        if (!token.IsActive)
            throw new ForbiddenException("Refresh token is already inactive.");

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);
    }
}
