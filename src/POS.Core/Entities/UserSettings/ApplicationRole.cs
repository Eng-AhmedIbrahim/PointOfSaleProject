namespace POS.Core.Entities.UserSettings;

public class ApplicationRole : IdentityRole
{
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public string? RoleId { get; set; }
    public ApplicationRole? Role { get; set; }

    public int PermissionId { get; set; }
    public Permission? Permission { get; set; }
}
