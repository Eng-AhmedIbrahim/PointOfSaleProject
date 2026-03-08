using POS.Contract;
using POS.Contract.Dtos.DeliveryDtos.DeliveryZoneDtos;

namespace BlazorBase.ERPFrontServices.DeliveryServices;

public interface IDeliveryServices
{
    public Task<IReadOnlyList<DeliveryTitleToReturnDto>> GetAllDeliveryCustomerTitlesAsync();
    public Task<IReadOnlyList<DeliveryZonesToReturnDto>> GetAllDeliveryZonesAsync();
    public Task<IReadOnlyList<DeliveryZonesToReturnDto>> GetDeliveryZoneByBranchAsync(int branchId);
    public Task<DeliveryCustomerToReturnDto> GetClientByPhoneNumberAsync(string phoneNumber);
    public Task<DeliveryCustomerDto> CreateClientAsync(DeliveryCustomerDto deliveryCustomer);
    public Task<CustomerNewAddressDto> AddNewCustomerAddressAsync(CustomerNewAddressDto newDeliveryCustomerAddress);
    public Task<ServiceResponse<DeliveryZonesToReturnDto>> CreateZone(DeliveryZoneDto newZone);
    public Task<ServiceResponse<DeliveryZonesToReturnDto>> UpdateZone(int id, DeliveryZonesToReturnDto updatedZone);
    public Task<ServiceResponse<bool>> DeleteZone(int zoneId);
}