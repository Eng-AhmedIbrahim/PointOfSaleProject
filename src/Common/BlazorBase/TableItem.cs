namespace BlazorBase;

public class TableItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public decimal? Total { get; set; }
    public List<string> Attributes { get; set; } = new List<string>();
}