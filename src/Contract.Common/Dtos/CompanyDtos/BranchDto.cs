namespace POS.Contract.Dtos.CompanyDtos;

public class BranchDto
{
    public int Id { get; set; }
    public BranchDto()
    {
        CreationDate = DateTime.Now;
        CreationDate.ToString("yyyy-MM-dd hh:mm:ss.fff tt zzz");
    }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public IFormFile? Logo { get; set; }
    public int LogoWidth { get; set; } = 200;
    public int LogoHeight { get; set; } = 100;
    public string? Address { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public bool Active { get; set; } = true;
    public bool Suspend { get; set; } = false;
    public DateTime CreationDate { get; set; }
}