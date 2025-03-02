namespace POS.Repository.Data.Configurations.OrderConfigurations
{
    public class OrderItemsDetailsConfiguration : IEntityTypeConfiguration<OrderItemsDetails>
    {
        public void Configure(EntityTypeBuilder<OrderItemsDetails> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrderType)
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(o => o.Quantity).HasDefaultValue(1);

            builder.Property(o => o.TotalDiscountPrice).HasColumnType("decimal(18,2)");
            builder.Property(o => o.TotalDiscountPercentage).HasColumnType("decimal(18,2)");
            builder.Property(o => o.TotalDiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(o => o.TotalAfterDiscount).HasColumnType("decimal(18,2)");
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            builder.Property(o => o.VoidAmount).HasColumnType("int");

            builder.HasOne(o => o.Order)
               .WithMany(o => o.OrderDetails)
               .HasForeignKey(o => o.OrderId) // Foreign key references Id in Orders
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(o => o.MenuSalesItem)
                   .WithMany()
                   .HasForeignKey(o => o.MenuSalesItemId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship with OrderItemAttributes
            builder.HasMany(o => o.OrderItemAttributes)
                   .WithOne(a => a.OrderItem)
                   .HasForeignKey(a => a.OrderItemId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Ensure AttributeName is stored as required
            //builder.Entity<OrderItemAttributes>()
            //       .Property(a => a.AttributeName)
            //       .IsRequired()
            //       .HasMaxLength(100);
        }
    }
}
