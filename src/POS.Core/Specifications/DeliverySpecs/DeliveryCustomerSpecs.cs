namespace POS.Core.Specifications.DeliverySpecs;

public class DeliveryCustomerSpecs : BaseSpecifications<DeliveryCustomerInfo>
{
    public DeliveryCustomerSpecs() 
    {
        Includes.Add(c => c.CustomerAddresses!);
    }

    public DeliveryCustomerSpecs(int customerId) : base(c => c.Id == customerId)
    {
        Includes.Add(c => c.CustomerAddresses!);
    }

    public DeliveryCustomerSpecs(string phoneNumber) : base(c => c.FirstPhoneNumber == phoneNumber)
    {
        Includes.Add(c => c.CustomerAddresses!);
    }

}
    