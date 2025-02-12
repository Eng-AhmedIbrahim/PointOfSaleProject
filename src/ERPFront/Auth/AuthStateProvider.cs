using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ERPFront.Auth
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private bool _authenticated;
        private readonly ClaimsPrincipal _unauthenticated = new(new ClaimsIdentity());

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            _authenticated = false;
            var user = _unauthenticated;
            return new AuthenticationState(user);
        }
    }
}