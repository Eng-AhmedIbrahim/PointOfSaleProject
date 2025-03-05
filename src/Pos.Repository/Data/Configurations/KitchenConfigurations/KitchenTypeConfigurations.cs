namespace Pos.Repository.Data.Configurations.KitchenConfigurations;

public class KitchenTypeConfiguration : IEntityTypeConfiguration<KitchenType>
{
    public void Configure(EntityTypeBuilder<KitchenType> builder)
    {
        builder.ToTable("KitchenTypes");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.BranchId)
            .IsRequired();

        builder.Property(k => k.KitchenName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.HasIndex(k => k.BranchId);
    }
}