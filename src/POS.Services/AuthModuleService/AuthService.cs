using POS.Core.Entities.Identity;

namespace POS.Services.AuthModuleService;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;


    public AuthService(IConfiguration configuration,
        AppDbContext context,
         UserManager<AppUser> userManager
        )
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<string> CreateTokenAsync(AppUser user, UserManager<AppUser> userManager)
    {
        // Private claims (user-defined)
        var authClaims = new List<Claim>()
        {
            new Claim(ClaimTypes.GivenName, user?.UserName??string.Empty),
            new Claim(ClaimTypes.Email, user?.Email??string.Empty)
        };

        var userRoles = await userManager.GetRolesAsync(user);

        foreach (var role in userRoles)
            authClaims.Add(new Claim(ClaimTypes.Role, role));

        var secretKey = Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"] ?? string.Empty);
        var requiredKeyLength = 256 / 8;
        if (secretKey.Length < requiredKeyLength)
        {
            Array.Resize(ref secretKey, requiredKeyLength);
        }

        var token = new JwtSecurityToken(
            audience: _configuration["JWT:ValidAudience"],
            issuer: _configuration["JWT:ValidIssuer"],
            expires: DateTime.UtcNow.AddDays(double.Parse(_configuration["JWT:DurationInDays"] ?? "1")),
            claims: authClaims,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}