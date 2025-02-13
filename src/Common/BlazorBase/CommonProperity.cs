namespace BlazorBase;

public class CommonProsperites
{
    public double Spacing { get; set; } = 4.0;
    public double Padding => Spacing * 2;
    public double FontSize => Spacing + 16;
}
