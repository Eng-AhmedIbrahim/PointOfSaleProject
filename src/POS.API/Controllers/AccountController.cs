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
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(error => error.Description));
            return BadRequest(new ApiResponse(400, errors));
        }

        return Ok(true);
    }

    [HttpPost("Login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto model)
    {
        if (!ModelState.IsValid)
            return Unauthorized(new ApiResponse(401));

        var user = await _userManager.FindByNameAsync(model.UserName);

        if (user is null)
            return Unauthorized(new ApiResponse(401));


        var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

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

        var user = await _userManager.FindByNameAsync(UserName);

        return Ok(new UserDto()
        {
            UserName = user.UserName,
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


    [HttpPost("CreateAdmin")]
    public async Task<IActionResult> CreateAdminUser(RegisterDto model)
    {
        if (CheckUserExists(model.UserName).Result.Value)
            return BadRequest(new ApiValidationErrorResponse() { Errors = new string[] { "This email already exists!" } });

        var user = new AppUser
        {
            UserName = model.UserName,
            RegistrationDate = DateTime.Now,
        };


        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            var userRole = await _userManager.AddToRoleAsync(user, "admin");
            string errors = string.Join(", ", result.Errors.Select(error => error.Description));
            return BadRequest(new ApiResponse(400, errors));
        }

        return Ok(true);

    }
}