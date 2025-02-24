namespace POS.Reports.Models;

public static class FakeDataGenerator
{
    private static readonly Random Random = new Random();

    public static Receipt GenerateFakeReceipt(int itemCount = 10)
    {
        var receipt = new Receipt
        {
            Id = 1,
            StoreName = "Test Store",
            CashierName = "Administrator",
            ReceiptType = "تيك اواي",
            PaymentMethod = "كاش",
            ReceiptNote = " ملاحظة ملاحظة ملاحظة",
            FooterMessage = "علموا اولادكم ان فلسطين عربية" + "\n" + "For Delivery Call: +1234567890"
        };
        var items = GenerateFakeItems(itemCount, receipt.Id);
        receipt.AddItems(items);


        return receipt;
    }

    private static List<ReceiptItem> GenerateFakeItems(int count, int receiptId)
    {
        var items = new List<ReceiptItem>();

        for (var i = 1; i <= count; i++)
        {
            var item = new ReceiptItem
            {
                Name = $"عنصر {i}",
                Quantity = Random.Next(1, 10), // Random quantity between 1 and 9
                Price = (decimal)(Random.NextDouble() * 1000) // Random price between 0 and 100
            };

            items.Add(item);
        }

        return items;
    }
}