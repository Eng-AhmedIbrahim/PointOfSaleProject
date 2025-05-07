namespace POS.Reports.Models.Delivery;

public class FakeDeliveryDataGenerator
{

    private static readonly Random Random = new();

    public static (DeliveryReceipt receipt, List<TableItem> tableItems) GenerateFakeReceipt(int itemCount = 5)
    {
        var arabicNames = new[] { "محمد أمين", "أحمد خالد", "ياسر محمود", "محمود عبد الله", "علي حسن" };
        var zones = new[] { "حي النصر", "المنطقة الشرقية", "وسط المدينة", "الحي الغربي", "الضاحية الجنوبية" };
        var deliveryNames = new[] { "توصيل سريع", "توصيل عادي", "توصيل فاخر", "توصيل VIP", "توصيل اقتصادي" };
        var paymentMethods = new[] { "كاش", "بطاقة ائتمان", "محفظة إلكترونية", "تحويل بنكي" };

        var receipt = new DeliveryReceipt
        {
            Id = Random.Next(1000, 9999),
            StoreName = "متجر الاختبار",
            CashierName = arabicNames[Random.Next(arabicNames.Length)],
            ReceiptType = "توصيل",
            PaymentMethod = paymentMethods[Random.Next(paymentMethods.Length)],
            ReceiptNote = "يرجى التأكد من العنوان قبل التوصيل",
            FooterMessage = "شكراً لاختياركم متجرنا\nللاستفسار: 0123456789",
            DateCreated = DateTimeOffset.Now,

            // Customer Information
            CustomerFirstPhone = $"01{Random.Next(10000000, 99999999)}",
            CustomerSecondPhone = Random.Next(2) == 1 ? $"01{Random.Next(10000000, 99999999)}" : null,
            CustomerName = arabicNames[Random.Next(arabicNames.Length)],
            Building = $"مبنى {Random.Next(1, 50)}",
            HomeNumber = $"منزل {Random.Next(1, 100)}",
            FloorNumber = $"طابق {Random.Next(1, 10)}",
            FlatNumber = $"شقة {Random.Next(1, 50)}",
            ZoneName = zones[Random.Next(zones.Length)],
            AddressNote = "بجوار مسجد السلام",
            DeliveryName = deliveryNames[Random.Next(deliveryNames.Length)],
        };

        // Generate items and calculate totals
        var items = GenerateFakeItems(itemCount);
        receipt.AddItems(items);

        // Calculate amounts
        var subtotal = items.SelectMany(i => i.Items).Sum(i => i.Total);
        receipt.DeliveryFees = (decimal)(Random.NextDouble() * 30 + 5);
        receipt.TotalAmount = subtotal;
        receipt.TotalOrder = subtotal + receipt.DeliveryFees;

        return (receipt, items.SelectMany(i => i.Items).ToList());
    }

    private static List<ReceiptItem> GenerateFakeItems(int count)
    {
        var arabicProducts = new[]
        {
    "بيتزا كبيرة", "برجر لحم", "ساندويتش دجاج", "سلطة يوناني", "مشروب غازي",
    "شاي مثلج", "قهوة عربية", "حلويات شرقية", "معكرونة", "بيتزا صغيرة"
};

        var receiptItems = new List<ReceiptItem>();

        for (int i = 0; i < count; i++)
        {
            var tableItems = new List<TableItem>();
            int itemCount = Random.Next(1, 3); // 1-2 items per receipt item

            for (int j = 0; j < itemCount; j++)
            {
                var quantity = Random.Next(1, 5);
                var price = (decimal)(Random.NextDouble() * 50 + 5);
                var total = quantity * price;

                tableItems.Add(new TableItem
                {
                    Id = i * 100 + j,
                    Name = arabicProducts[Random.Next(arabicProducts.Length)],
                    Quantity = quantity,
                    Price = price,
                    Total = total,
                    LineComment = Random.Next(4) == 1 ? "بدون بصل" : null,
                    Attributes = Random.Next(3) == 1 ? GenerateFakeAttributes(Random.Next(1, 3)) : new List<AttributeDto>()
                });
            }

            receiptItems.Add(new ReceiptItem(tableItems));
        }

        return receiptItems;
    }

    private static List<AttributeDto> GenerateFakeAttributes(int count)
    {
        var arabicAttributes = new[]
        {
    "إضافة جبن", "صوص حار", "بدون طماطم", "حجم كبير", "تتبيلة خاصة",
    "مقرمشات إضافية", "صوص ثوم", "حجم عائلي", "تزيين إضافي"
};

        return Enumerable.Range(1, count)
            .Select(i => new AttributeDto
            {
                Id = i,
                Name = arabicAttributes[Random.Next(arabicAttributes.Length)]
            })
            .ToList();
    }
}