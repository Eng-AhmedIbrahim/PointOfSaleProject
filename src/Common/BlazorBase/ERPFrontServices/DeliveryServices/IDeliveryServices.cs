namespace BlazorBase.ERPFrontServices.DeliveryServices;

public interface IDeliveryServices
{
    public Task<IReadOnlyList<DeliveryTitleToReturnDto>> GetAllDeliveryCustomerTitlesAsync();
    public Task<IReadOnlyList<DeliveryZonesToReturnDto>> GetAllDeliveryZonesAsync();
    public Task<IReadOnlyList<DeliveryZonesToReturnDto>> GetDeliveryZoneByBranchAsync(int branchId);
    public Task<DeliveryCustomerToReturnDto> GetClientByPhoneNumberAsync(string phoneNumber);
    public Task<DeliveryCustomerDto> CreateClientAsync(DeliveryCustomerDto deliveryCustomer);
    public Task<CustomerNewAddressDto> AddNewCustomerAddressAsync(CustomerNewAddressDto newDeliveryCustomerAddress);
}