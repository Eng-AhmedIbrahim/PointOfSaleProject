namespace POS.Contract.Models;

public class TableItem
{
    public int Id { get; set; }
    public int DatabaseId { get; set; }
    public int Quantity { get; set; }
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public decimal? Total { get; set; }
    public decimal? AttributePrice { get; set; }
    public string? LineComment { get; set; }
    public string? NameAr { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool? PrintInBackupReceiptFromItem { get; set; }
    public bool? PrintInBackupReceiptFromCategory { get; set; }
    public int? ItemKitchenTypeId { get; set; }
    public int? CategoryKitchenTypeId { get; set; }
    public List<AttributeDto>? Attributes { get; set; } = new List<AttributeDto>();

    public bool HasDiscount { get; set; }
    public decimal? DiscountPercentage { get; set; } = null;
    public decimal? DiscountAmount { get; set; } = null;
    public decimal? TotalDiscountPrice { get; set; }
    public decimal? TotalAfterDiscount { get; set; }

    public bool HasTax { get; set; } = false;
    public decimal TaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }

    public bool IsReadOnly { get; set; }
    public bool IsVoided { get; set; }
    public decimal? TotalVoidAmount { get; set; }
    public int? VoidAmount { get; set; }
    public string? VoidBy { get; set; }
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }

    public TableItem Clone()
    {
        return new TableItem
        {
            Id = this.Id,
            DatabaseId = this.DatabaseId,
            Quantity = this.Quantity,
            Name = this.Name,
            NameAr = this.NameAr,
            CategoryId = this.CategoryId,
            CategoryName = this.CategoryName,
            Price = this.Price,
            Total = this.Total,
            LineComment = this.LineComment,
            PrintInBackupReceiptFromItem = this.PrintInBackupReceiptFromItem,
            PrintInBackupReceiptFromCategory = this.PrintInBackupReceiptFromCategory,
            ItemKitchenTypeId = this.ItemKitchenTypeId,
            CategoryKitchenTypeId = this.CategoryKitchenTypeId,
            Attributes = this.Attributes?.Select(attr => attr.Clone()).ToList() ?? new List<AttributeDto>(),
            HasDiscount = this.HasDiscount,
            DiscountPercentage = this.DiscountPercentage,
            DiscountAmount = this.DiscountAmount,
            HasTax = this.HasTax,
            TaxAmount = this.TaxAmount,
            TotalAmount = this.TotalAmount,
            IsReadOnly = this.IsReadOnly,
            TotalDiscountPrice = this.TotalDiscountPrice,
            TotalAfterDiscount = this.TotalAfterDiscount,
            IsVoided = this.IsVoided,
            TotalVoidAmount = this.TotalVoidAmount,
            VoidBy = this.VoidBy,
            VoidByName = this.VoidByName,
            VoidTime = this.VoidTime,
            VoidReason = this.VoidReason
        };
    }
}

public class AttributeDto
{
    public int? Id { get; set; }
    public string? Name { get; set; } = string.Empty;

    public AttributeDto Clone()
    {
        return new AttributeDto
        {
            Id = this.Id,
            Name = this.Name
        };
    }
}

public class AttributeDtoComparer : IEqualityComparer<AttributeDto>
{
    public bool Equals(AttributeDto? x, AttributeDto? y)
    {
        if (x == null || y == null)
            return x == y; // Both null = equal, one null = not equal

        return x.Id == y.Id && x.Name == y.Name;
    }

    public int GetHashCode(AttributeDto obj)
    {
        return HashCode.Combine(obj.Id, obj.Name);
    }
}
