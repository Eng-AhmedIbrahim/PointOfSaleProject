using POS.Core.Entities.Kitchen;

namespace Pos.Repository.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    => modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    public DbSet<AttributeItem> AttributeItems { get; set; }
    public DbSet<Attributes> Attributes { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<KitchenType> KitchenTypes { get; set; }
    public DbSet<MenuSalesItems> MenuSalesItems { get; set; }
    public DbSet<OrderItemAttributes> OrderItemAttributes { get; set; }
    public DbSet<Orders> Orders { get; set; }
    public DbSet<OrderItemsDetails> OrdersDetails { get; set; }
    public DbSet<OrderSetting> OrderSettings { get; set; }
    public DbSet<PrintingSettings> PrintingSettings { get; set; }
    public DbSet<TakeawayCustomer> TakeawayCustomers { get; set; }

}