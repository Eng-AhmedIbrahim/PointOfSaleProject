namespace POS.API.Controllers;

public class AccountController : BaseApiController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IAuthService _authService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
        IAuthService authService,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IMapper mapper
        )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _authService = authService;
        _roleManager = roleManager;
        _configuration = configuration;
        _mapper = mapper;
    }

    [HttpPost("CreateUser")]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (CheckUserExists(model.UserName).Result.Value)
            return BadRequest(new ApiValidationErrorResponse() { Errors = new string[] { "This User already exists!" } });

        var user = new AppUser
        {
            UserName = model.UserName,
            RegistrationDate = DateTime.Now,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            NormalizedUserName = model.DisplayName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(error => error.Description));
            Log.Error($"Cant Create User With Name {model.UserName}",errors);
            return BadRequest(new ApiResponse(400, errors));
        }

        if(!await _roleManager.RoleExistsAsync(model.roleName))
            return BadRequest(new ApiResponse(400, "Role not found"));

        var result1= await _userManager.AddToRoleAsync(user, model.roleName);

        if (!result1.Succeeded)
        {
            string errors = string.Join(", ", result1.Errors.Select(error => error.Description));
            Log.Error($"Cant Add User To Role {model.UserName}",errors);
            return BadRequest(new ApiResponse(400, errors));
        }

        return Ok(true);
    }

    [HttpPost("Login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto model)
    {
        if (!ModelState.IsValid)
            return Unauthorized(new ApiResponse(401));

        var user = await _userManager.FindByNameAsync(model!.UserName!);

        if (user is null)
            return Unauthorized(new ApiResponse(401));


        var result = await _signInManager.PasswordSignInAsync(user, model!.Password!, false, false);

        if (!result.Succeeded)
            return Unauthorized(new ApiResponse(401));

        var userDto = _mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    //[Authorize]
    [HttpGet("GetCurrentUser")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var UserName = User.FindFirstValue(ClaimTypes.Name);

        var user = await _userManager.FindByNameAsync(UserName!);

        return Ok(new UserDto()
        {
            UserName = user!.UserName!,
            //Token = await _authService.CreateTokenAsync(user, _userManager)
        });
    }

    [HttpGet("GetUsers")]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var mappedUsers = _mapper.Map<List<UserDto>>(users);
        return Ok(mappedUsers);
    }

    [HttpGet("UserExists")]
    public async Task<ActionResult<bool>> CheckUserExists(string userName)
        => await _userManager.FindByNameAsync(userName) is not null;

    [HttpPost("CreateRole")]
    public async Task<ActionResult> CreateRole(string Name)
    {
        try
        {
            if (string.IsNullOrEmpty(Name)) return BadRequest(new ApiResponse(400, "Role cannot be Empty !!"));

            bool isRoleAlreadyExists = await _roleManager.RoleExistsAsync(Name);
            if (isRoleAlreadyExists) return BadRequest(new ApiResponse(400, $"Role: {Name} Already Exists !!"));

            await _roleManager.CreateAsync(new IdentityRole(Name));
            return Ok(Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return BadRequest(new ApiResponse(400));
        }
    }


    //[Authorize]
    [HttpPost("Logout")]
    public async Task<ActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            try
            {
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message); // Log the error
                return BadRequest(new { message = "Error during logout" });
            }
        }

        return BadRequest(new { message = "Unable to logout" });
    }


    [HttpDelete("delete-role/{roleName}")]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        var result = await _authService.DeleteRoleAsync(roleName);
        return result ? Ok("Role deleted successfully") : NotFound("Role not found");
    }

    [HttpDelete("delete-user/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var result = await _authService.DeleteUserAsync(userId);
        return result ? Ok("User deleted successfully") : NotFound("User not found");
    }

    [HttpGet("get-roles")]
    public async Task<ActionResult<List<IdentityRole>>> GetAllRoles()
    {
        var roles = await _authService.GetAllRolesAsync();
        return Ok(roles);
    }

    [HttpGet("get-users")]
    public async Task<ActionResult<List<AppUser>>> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("get-role/{roleName}")]
    public async Task<ActionResult<IdentityRole>> GetRole(string roleName)
    {
        var role = await _authService.GetRoleAsync(roleName);
        return role != null ? Ok(role) : NotFound("Role not found");
    }

    [HttpGet("get-user/{userId}")]
    public async Task<ActionResult<AppUser>> GetUser(string userId)
    {
        var user = await _authService.GetUserAsync(userId);
        return user != null ? Ok(user) : NotFound("User not found");
    }

    [HttpPost("remove-user-from-role")]
    public async Task<IActionResult> RemoveUserFromRole([FromBody] UserRoleRequest request)
    {
        var result = await _authService.RemoveUserFromRoleAsync(request!.UserId!, request!.RoleName!);
        return result ? Ok("User removed from role successfully") : BadRequest("Failed to remove user from role");
    }

    [HttpPost("add-user-to-role")]
    public async Task<IActionResult> AddUserToRole([FromBody] UserRoleRequest request)
    {
        var result = await _authService.AddUserToRoleAsync(request!.UserId!, request!.RoleName!);
        return result ? Ok("User added to role successfully") : BadRequest("Failed to add user to role");
    }
}

public class UserRoleRequest
{
    public string? UserId { get; set; }
    public string? RoleName { get; set; }
}