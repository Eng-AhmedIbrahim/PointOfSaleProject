namespace POS.Repository.Data.Configurations.ItemConfigurations;

public class ItemConfigurations : IEntityTypeConfiguration<MenuSalesItems>
{
    private readonly int MaxLength = 255;
    private readonly int MinLength = 70;
    public void Configure(EntityTypeBuilder<MenuSalesItems> builder)
    {
        builder.Property(s => s.ArabicName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.Property(s => s.EnglishName)
         .HasColumnType("nvarchar")
         .HasMaxLength(MinLength)
         .IsRequired();


        builder.Property(c => c.NormalizedEnglishName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.Property(e => e.Price)
         .HasColumnType("decimal(18,2)")
         .IsRequired(false);

        builder.Property(e => e.SecondPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(e => e.ThirdPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(e => e.FourthPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(e => e.FifthPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(e => e.Tax)
           .HasColumnType("decimal(18,2)")
           .IsRequired(false);

        builder.Property(e => e.Description)
            .HasMaxLength(MaxLength) 
            .IsRequired(false);

        builder.Property(e => e.ImagePath)
            .HasMaxLength(MaxLength) 
            .IsRequired(false);

        builder.Property(e => e.BackColor)
            .HasMaxLength(7)
            .IsRequired(false);

        builder.Property(e => e.TextColor)
            .HasMaxLength(7)
            .IsRequired(false);

        builder.Property(e => e.TextSize)
            .IsRequired(false);

        builder.Property(e => e.Invisible)
            .IsRequired();

        builder.Property(e => e.CreationDate)
            .IsRequired();

        builder.Property(e => e.UpdatedDate)
            .IsRequired(false);


        builder.HasOne(e => e.Branch)
           .WithMany()
           .HasForeignKey(e => e.BranchId) // Foreign key to Branch, now nullable
           .OnDelete(DeleteBehavior.SetNull); // This will work now

        builder.HasOne(e => e.Category)
            .WithMany(e => e.MenuSalesItems)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Consider keeping this as restrict

        builder.HasOne(e => e.Attribute)
            .WithOne()
            .HasForeignKey<MenuSalesItems>(e => e.AttributeId)
            .OnDelete(DeleteBehavior.SetNull);

             builder.Property(e => e.MainCategoryId)
            .HasConversion(
                v => v.ToString(),
                v => (MainCategories)Enum.Parse(typeof(MainCategories), v!));
    }
}
