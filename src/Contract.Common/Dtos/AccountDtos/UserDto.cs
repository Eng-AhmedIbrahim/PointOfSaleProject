namespace POS.Contract.Dtos.AccountDtos;

public class UserDto
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public bool UpdatedUserPosSetting { get; set; } 
    public string? DefaultLanguage { get; set; } 
    //public string? Token { get; set; }
}