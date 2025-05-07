namespace POS.API.Controllers.DeliveryControllers;

public class DeliveryCustomerController : BaseApiController
{
    private readonly IDeliveryCustomerService _deliveryCustomerService;
    private readonly IMapper _mapper;

    public DeliveryCustomerController(IDeliveryCustomerService deliveryCustomerService, IMapper mapper)
    {
        _deliveryCustomerService = deliveryCustomerService;
        _mapper = mapper;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        var customer = await _deliveryCustomerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound("Customer not found");

        var customerDto = _mapper.Map<DeliveryCustomerToReturnDto>(customer); 
        return Ok(customerDto);
    }


    [HttpGet("with-addresses/{id}")]
    public async Task<IActionResult> GetCustomerWithAddresses(int id)
    {
        var customer = await _deliveryCustomerService.GetCustomerWithAddressesAsync(id);
        if (customer == null)
            return NotFound("Customer not found");

        var customerDto = _mapper.Map<DeliveryCustomerToReturnDto>(customer);
        return Ok(customerDto);
    }


    [HttpGet("Get-Customer-By-Phone/{phoneNumber}")]
    public async Task<IActionResult> GetCustomerByPhoneNumber(string phoneNumber)
    {
        var customer = await _deliveryCustomerService.GetCustomerByPhoneNumberAsync(phoneNumber);
        if (customer == null)
            return NotFound("Customer not found");

        var customerDto = _mapper.Map<DeliveryCustomerToReturnDto>(customer);
        return Ok(customerDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCustomers()
    {
        var customers = await _deliveryCustomerService.GetAllCustomersAsync();
        var customersDto = _mapper.Map<IEnumerable<DeliveryCustomerToReturnDto>>(customers);
        return Ok(customersDto);
    }

    [HttpPost("create-customer")]
    public async Task<IActionResult> CreateCustomer(DeliveryCustomerDto customerDto)
    {
        var customer = _mapper.Map<DeliveryCustomerDto, DeliveryCustomerInfo>(customerDto);

        var createdCustomer = await _deliveryCustomerService.CreateCustomerAsync(customer);

        if (createdCustomer is null)
            return BadRequest("Failed to create customer.");

        return Ok(createdCustomer);
    }
        
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, DeliveryCustomerInfo customer)
    {
        if (id != customer.Id)
            return BadRequest("Customer ID mismatch");

        var updatedCustomer = await _deliveryCustomerService.UpdateCustomerAsync(customer);
        return Ok(updatedCustomer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var result = await _deliveryCustomerService.DeleteCustomerAsync(id);
        if (!result)
            return NotFound("Customer not found");

        return NoContent();
    }


    [HttpPost("add-new-customer-address")]
    public async Task<IActionResult> AddNewCustomerAddress(CustomerNewAddressDto customerAddress)
    {
        var customerAddressToCreate = _mapper.Map<CustomerNewAddressDto, CustomerAddress>(customerAddress);
        var createdCustomer = await _deliveryCustomerService.AddNewCustomerAddressAsync(customerAddress.FirstPhoneNumber!, customerAddressToCreate);

        if (createdCustomer is null)
            return BadRequest("Failed to add customer address.");

        return Ok(customerAddress);
    }
}