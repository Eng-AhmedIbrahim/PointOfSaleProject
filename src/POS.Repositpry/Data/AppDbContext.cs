using POS.Core.Entities.Categorie;
using POS.Core.Entities.Customer;

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
    public DbSet<Orders> Orders { get; set; }
    public DbSet<OrderItemsDetails> OrdersDetails { get; set; }
    public DbSet<TakeawayCustomer> TakeawayCustomers { get; set; }

    //public DbSet<DiscountCode> DiscountCodes { get; set; }
    //public DbSet<DiscountCodesOccasion> DiscountCodesOccasions { get; set; }
    //public DbSet<DiscountReason> DiscountReasons { get; set; }
    //public DbSet<ShiftHandover> Shifts { get; set; }
}