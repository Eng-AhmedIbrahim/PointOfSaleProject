using POS.Core.Entities.Identity;

namespace POS.Core.Services.Contract.AccountDomainContracts;

public interface IAuthService
{
    Task<string> CreateTokenAsync(AppUser user, UserManager<AppUser> userManager);
}
