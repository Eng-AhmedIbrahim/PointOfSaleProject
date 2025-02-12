namespace POS.Repository.Data.Configurations.OrderConfigurations;

public class OrderConfiguration : IEntityTypeConfiguration<Orders>
{
    public void Configure(EntityTypeBuilder<Orders> builder)
    {
        //builder.HasKey(o => o.Id);

        //builder.Property(o => o.BranchID)
        //    .IsRequired();

        //builder.Property(o => o.BranchName)
        //    .HasMaxLength(100); 

        //builder.Property(o => o.Closed)
        //    .IsRequired();

        //builder.Property(o => o.ShiftID)
        //    .IsRequired();

        ////builder.HasOne(o => o.Details)
        ////    .WithOne()
        ////    .HasForeignKey<OrderDetail>(d => d.); // Assuming OrderDetail has a foreign key to Order

        //builder.HasOne(o => o.Customer)
        //    .WithOne()
        //    .HasForeignKey<DeliveryInfo>(d => d.CustomerId); // Adjust according to your model

        //builder.HasOne(o => o.Delivery)
        //    .WithOne()
        //    .HasForeignKey<DeliveryCompanyInfo>(d => d.DeliveryCompanyID); // Adjust according to your model

        //builder.HasOne(o => o.Payment)
        //    .WithOne()
        //    .HasForeignKey<PaymentInfo>(p => p.PaymentId); // Assuming PaymentInfo has a foreign key to Order

        //builder.HasOne(o => o.Discount)
        //    .WithOne()
        //    .HasForeignKey<DiscountInfo>(d => d.DiscountId); // Assuming DiscountInfo has a foreign key to Order

        //builder.HasOne(o => o.Table)
        //    .WithOne()
        //    .HasForeignKey<TableInfo>(t => t.TableID); // Adjust according to your model

        //builder.HasOne(o => o.Staff)
        //    .WithOne()
        //    .HasForeignKey<StaffInfo>(s => s.WaiterID); // Adjust according to your model

        //builder.HasOne(o => o.ShiftHandover)
        //    .WithMany() // Assuming ShiftHandover can relate to multiple Orders
        //    .HasForeignKey(s => s.ShiftID); // Adjust as needed
    }
}
