using POS.Core.Entities.Customer;
using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.OrderServices;

namespace POS.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Orders?> CreateOrderAsync(Orders order)
    {
        if (order is null)
            return null;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                // Ensure the customer exists before assigning TakeawayCustomerId
                if (order.TakeawayCustomer is not null)
                {
                    var existingCustomer = await _unitOfWork.Repository<TakeawayCustomer>()
                        .GetByIdAsync(order.TakeawayCustomer.Id);

                    if (existingCustomer is null)
                    {
                        await _unitOfWork.Repository<TakeawayCustomer>().AddAsync(order.TakeawayCustomer);
                        await _unitOfWork.CompleteAsync(); 
                        order.TakeawayCustomerId = order.TakeawayCustomer.Id;
                    }
                    else
                    {
                        order.TakeawayCustomerId = existingCustomer.Id;
                    }
                }
                else
                {
                    // If there's no customer, don't set TakeawayCustomerId (it should allow NULL in the DB)
                    order.TakeawayCustomerId = null;
                }

                // Now add the order
                await _unitOfWork.Repository<Orders>().AddAsync(order);

                // Add order details if they exist
                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    foreach (var item in order.OrderDetails)
                    {
                        await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(item);
                    }
                }

                // Save changes
                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return null;
                }

                transaction.Commit();
                return order;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while creating the order.");
                return null;
            }
        }

    }

}