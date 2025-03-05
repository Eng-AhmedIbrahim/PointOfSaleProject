using Pos.Repository.Data;
using POS.Core.Entities.Identity;
using System.IdentityModel.Tokens.Jwt;

namespace POS.Services.AuthModuleService;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthService(IConfiguration configuration,
        AppDbContext context,
         UserManager<AppUser> userManager,
         RoleManager<IdentityRole> roleManager
        )
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task<bool> AddUserToRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return false;

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
            return false;

        var result= await _userManager.AddToRoleAsync(user, roleName);

        return result.Succeeded;
    }

    public async Task<bool> CreateRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
            return false;

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

        return result.Succeeded;
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

    public async Task<AppUser> CreateUserAsync(string userName,string displayName, string password, string? userRole)
    {
        var user = new AppUser
        {
            UserName = userName,
            NormalizedUserName = displayName,
            Email = userName,
            RegistrationDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            Log.Error("User creation failed: {0}", string.Join(", ", result.Errors.Select(e => e.Description)));

        if (!string.IsNullOrEmpty(userRole))
        {
            if (!await _roleManager.RoleExistsAsync(userRole))
                await _roleManager.CreateAsync(new IdentityRole(userRole));

            await _userManager.AddToRoleAsync(user, userRole);
        }

        return user;
    }

    public async Task<bool> DeleteRoleAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
            return false;

        var result = await _roleManager.DeleteAsync(role);
        return result.Succeeded;
    }
    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<List<IdentityRole>> GetAllRolesAsync()
    =>  _roleManager.Roles.ToList();

    public async Task<List<AppUser>> GetAllUsersAsync()
    => _userManager.Users.ToList();

    public async Task<IdentityRole> GetRoleAsync(string roleName)
    => await _roleManager.FindByNameAsync(roleName)??new();

    public async Task<AppUser> GetUserAsync(string userId)
    => await _userManager.FindByIdAsync(userId)??new();

    public async Task<bool> RemoveUserFromRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> UpdateUserAsync(string userId,string newUserName, string newPassword, string newDisplayName, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        // Update basic details
        user.UserName = newUserName;
        user.Email = newUserName;
        user.NormalizedUserName = newDisplayName;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return false;

        if (!string.IsNullOrEmpty(newPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!passwordResult.Succeeded)
                return false;
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        if (!await _roleManager.RoleExistsAsync(newRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(newRole));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, newRole);
        return roleResult.Succeeded;
    }
}