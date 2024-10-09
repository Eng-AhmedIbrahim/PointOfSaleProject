using System.Reflection;

namespace POS.Repository.Data;

public class AppDbContext:DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    => modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    public DbSet<Company> Companies { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Attributes> Attributes { get; set; }
    public DbSet<AttributeItem> AttributeItems { get; set; }
    public DbSet<MenuSalesItems> MenuSalesItems { get; set; }
}
