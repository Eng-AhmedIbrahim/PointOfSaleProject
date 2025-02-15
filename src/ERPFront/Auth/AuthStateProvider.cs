namespace ERPFront.Auth
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private bool _authenticated;
        private readonly ClaimsPrincipal _unauthenticated = new(new ClaimsIdentity());

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            _authenticated = false;
            await Task.Delay(10);
            var user = _unauthenticated;
            return  new AuthenticationState(user);
        }
    }
}