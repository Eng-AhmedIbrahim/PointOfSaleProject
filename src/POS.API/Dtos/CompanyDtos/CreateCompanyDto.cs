namespace POS.API.Dtos.CompanyDtos;

public class CreateCompanyDto
{
    public CreateCompanyDto()
    {
        CreationDate = DateTime.Now;
    }
    public string? EnglishName { get; set; }
    public string? ArabicName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime? CreationDate { get; set; }
}