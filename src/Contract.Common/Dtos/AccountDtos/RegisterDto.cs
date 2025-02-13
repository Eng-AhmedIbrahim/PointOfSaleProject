namespace POS.Contract.Dtos.AccountDtos;

public class RegisterDto
{
    [Required]
    public string UserName { get; set; }

    [Required]
    public string Password { get; set; }
}
