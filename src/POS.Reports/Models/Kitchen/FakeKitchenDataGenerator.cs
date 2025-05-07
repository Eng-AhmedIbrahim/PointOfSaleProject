namespace POS.Reports.Models.Kitchen;

public class FakeKitchenDataGenerator
{
    private static readonly Random Random = new();

    public static (KitchenReceipt receipt, List<TableItem> tableItems) GenerateFakeReceipt(int itemCount = 5)
    {
        var arabicNames = new[] { "محمد أمين", "أحمد خالد", "ياسر محمود", "محمود عبد الله", "علي حسن" };
        var kitchenTypes = new[] { "مطبخ رئيسي", "مطبخ الحلويات", "مطبخ المشروبات", "مطبخ الوجبات السريعة" };
        var orderTypes = new[] { "طاولة", "تيك أواي", "توصيل", "طلبية خاصة" };
        var kitchenNotes = new[]
        {
                "يجب تقديم الطلب خلال 15 دقيقة",
                "إضافة صلصة حارة بجانب الطلب",
                "تقديم الطبق ساخن جداً",
                "بدون مكسرات للطبق",
                "تقطيع البيتزا 8 قطع"
        };

        var receipt = new KitchenReceipt
        {
            Id = Random.Next(1000, 9999),
            KitchenType = kitchenTypes[Random.Next(kitchenTypes.Length)],
            OrderType = orderTypes[Random.Next(orderTypes.Length)],
            CashierName = arabicNames[Random.Next(arabicNames.Length)],
            KitchenNote = Random.Next(3) == 1 ? kitchenNotes[Random.Next(kitchenNotes.Length)] : string.Empty,
            DateCreated = DateTimeOffset.Now
        };

        var items = GenerateFakeItems(itemCount);
        receipt.AddItems(items);

        return (receipt, items.SelectMany(i => i.Items).ToList());
    }

    private static List<ReceiptItem> GenerateFakeItems(int count)
    {
        var arabicProducts = new[]
        {
                "بيتزا كبيرة", "برجر لحم", "ساندويتش دجاج", "سلطة يوناني", "مشروب غازي",
                "شاي مثلج", "قهوة عربية", "حلويات شرقية", "معكرونة", "بيتزا صغيرة",
                "ساندويتش فيليه", "بطاطس مقلية", "كباب مشوي", "شيش طاووق", "مشاوي"
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
                    LineComment = Random.Next(4) == 1 ? GetRandomKitchenComment() : null,
                    Attributes = Random.Next(3) == 1 ? GenerateFakeAttributes(Random.Next(1, 3)) : new List<AttributeDto>()
                });
            }

            receiptItems.Add(new ReceiptItem(tableItems));
        }

        return receiptItems;
    }

    private static string GetRandomKitchenComment()
    {
        var comments = new[]
        {
                "بدون بصل",
                "صوص إضافي",
                "حار جداً",
                "بدون ملح",
                "تقطيع نصين",
                "تقديم سريع",
                "بدون جلوتين"
            };
        return comments[Random.Next(comments.Length)];
    }

    private static List<AttributeDto> GenerateFakeAttributes(int count)
    {
        var arabicAttributes = new[]
        {
                "إضافة جبن",
                "صوص حار",
                "بدون طماطم",
                "حجم كبير",
                "تتبيلة خاصة",
                "مقرمشات إضافية",
                "صوص ثوم",
                "حجم عائلي",
                "تزيين إضافي"
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