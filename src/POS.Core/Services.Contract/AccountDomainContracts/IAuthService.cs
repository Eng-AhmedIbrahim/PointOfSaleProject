using POS.Core.Entities.Identity;

namespace POS.Core.Services.Contract.AccountDomainContracts;

public interface IAuthService
{
    public Task<string> CreateTokenAsync(AppUser user, UserManager<AppUser> userManager);

    public Task<AppUser> CreateUserAsync(string userName, string displayName, string password, string? userRole);

    public Task<bool> CreateRoleAsync(string roleName);

    public Task<bool> UpdateUserAsync(string userId, string newUserName, string newPassword, string newDisplayName, string newRole);
    public Task<List<AppUser>> GetAllUsersAsync();
    public Task<AppUser> GetUserAsync(string userId);
    public Task<bool> DeleteUserAsync(string userId);
    public Task<bool> DeleteRoleAsync(string roleName);

    public Task<bool> AddUserToRoleAsync(string userId, string roleName);
    public Task<bool> RemoveUserFromRoleAsync(string userId, string roleName);

    public Task<List<IdentityRole>> GetAllRolesAsync();
    public Task<IdentityRole> GetRoleAsync(string roleName);
}