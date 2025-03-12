using POS.Contract.Dtos.DineInDtos;

namespace POS.Core.Services.Contract.AccountDomainContracts;

public interface IAuthService
{
    public Task<string> CreateTokenAsync(AppUser user, IList<string> roles, List<Claim> roleClaims);

    public Task<AppUser?> CreateUserAsync(RegisterDto registerDto);

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

    public Task<HashSet<string>> GetUserPermissionsAsync(ClaimsPrincipal user);

    //InUse
    public Task<List<AppUser>> GetUsersHasSpecificRole(string roleName);
}