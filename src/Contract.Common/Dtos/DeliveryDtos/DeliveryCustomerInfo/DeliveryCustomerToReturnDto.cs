namespace POS.Contract.Dtos.DeliveryDtos.DeliveryCustomerInfo;

public class DeliveryCustomerToReturnDto
{
    public int Id { get; set; }
    public string? FirstPhoneNumber { get; set; }
    public string? SecondPhoneNumber { get; set; }
    public string? ClientTitle { get; set; }
    public string? CustomerName { get; set; }

    public ICollection<CustomerAddressDto>? CustomerAddresses { get; set; }
}