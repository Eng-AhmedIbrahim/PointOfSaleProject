namespace POS.Reports.Models.DineIn;

public static class FakeDineInDataGenerator
{
    private static readonly Random Random = new();

    public static (DineInReceipt receipt, List<TableItem> tableItems) Generate(int itemCount = 5)
    {
        var arabicNames = new[] { "محمد أمين", "أحمد خالد", "ياسر محمود", "محمود عبد الله", "علي حسن" };
        var receiptNotes = new[] {
            "تم التأكيد مع العميل",
            "يرجى تحضير الطلب بسرعة",
            "الطلب يحتوي على عناصر حساسة",
            "طلب خاص، راجع الملاحظات"
        };

        var receipt = new DineInReceipt
        {
            Id = Random.Next(1000, 9999),
            StoreName = "مطعم الزهراء",
            CashierName = arabicNames[Random.Next(arabicNames.Length)],
            CaptainName = arabicNames[Random.Next(arabicNames.Length)],
            ReceiptType = "طاولة",
            ReceiptNote = Random.Next(3) == 1 ? receiptNotes[Random.Next(receiptNotes.Length)] : string.Empty,
            PaymentMethod = "نقداً",
            FooterMessage = "شكراً لزيارتكم! نتمنى رؤيتكم مرة أخرى.",
            TotalAmount = 0
        };

        var items = GenerateReceiptItems(itemCount);
        receipt.AddItems(items);

        return (receipt, items.SelectMany(i => i.Items).ToList());
    }

    private static List<ReceiptItem> GenerateReceiptItems(int count)
    {
        var productNames = new[]
        {
            "بيتزا مارجريتا", "ساندويتش شاورما", "سلطة فتوش", "كباب لحم", "عصير برتقال",
            "شاي بالنعناع", "قهوة تركي", "صحن حمص", "صحن تبولة", "مشروب غازي"
        };

        var items = new List<ReceiptItem>();

        for (int i = 0; i < count; i++)
        {
            var tableItems = new List<TableItem>();
            int innerCount = Random.Next(1, 3); // 1-2 inner items

            for (int j = 0; j < innerCount; j++)
            {
                var quantity = Random.Next(1, 4);
                var price = Math.Round((decimal)(Random.NextDouble() * 30 + 5), 2);
                var total = quantity * price;

                tableItems.Add(new TableItem
                {
                    Id = i * 100 + j,
                    Name = productNames[Random.Next(productNames.Length)],
                    Quantity = quantity,
                    Price = price,
                    Total = total,
                    LineComment = Random.Next(3) == 1 ? GetRandomComment() : null,
                    Attributes = Random.Next(2) == 1 ? GenerateAttributes(Random.Next(1, 3)) : new List<AttributeDto>()
                });
            }

            items.Add(new ReceiptItem(tableItems));
        }

        return items;
    }

    private static string GetRandomComment()
    {
        var comments = new[]
        {
            "بدون ملح", "زيادة صوص", "تسوية متوسطة", "بدون جبن", "حار جداً"
        };
        return comments[Random.Next(comments.Length)];
    }

    private static List<AttributeDto> GenerateAttributes(int count)
    {
        var attributes = new[]
        {
            "إضافة جبن", "بدون طماطم", "صوص إضافي", "حجم كبير", "تتبيلة خاصة"
        };

        return Enumerable.Range(0, count)
            .Select(_ => new AttributeDto
            {
                Id = Random.Next(1, 1000),
                Name = attributes[Random.Next(attributes.Length)]
            }).ToList();
    }
}
