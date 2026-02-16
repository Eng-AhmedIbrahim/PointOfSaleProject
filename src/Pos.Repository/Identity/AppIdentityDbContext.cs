using POS.Core.Entities.UserSettings;

namespace Pos.Repository.Identity;

public class AppIdentityDbContext : IdentityDbContext<AppUser, ApplicationRole, string>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }

    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        builder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        builder.Entity<AppUser>().Property(u => u.ArabicName).HasColumnType("nvarchar").HasMaxLength(100);
        builder.Entity<AppUser>().Property(u => u.DisplayName).HasColumnType("nvarchar").HasMaxLength(100);
        builder.Entity<AppUser>().Property(u => u.ImageUrl).HasColumnType("nvarchar").HasMaxLength(100);
    }
}