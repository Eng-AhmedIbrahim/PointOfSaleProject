namespace POS.Services.AppDateServices;

public class AppDateService : IAppDateService
{
    private readonly IUnitOfWork _unitOfWork;


    public AppDateService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<AppDate> GetAppDateAsync()
    {
         var appDates= await _unitOfWork.Repository<AppDate>().GetAllAsync();

        return appDates!.FirstOrDefault()??new();
    }

    public async Task<AppDate> UpdateAppDate()
    {
        try
        {
            var appDate = (await GetAppDateAsync());
            appDate.PosDate = appDate.PosDate.AddDays(1);
            appDate.StoreDate = appDate.StoreDate.AddDays(1);

            _unitOfWork.Repository<AppDate>().Update(appDate);
            await _unitOfWork.CompleteAsync();
            return appDate;
        }
        catch (Exception ex) 
        {
            Log.Error(ex.Message);
            return new();
        }
    }


    public async Task<AppDate> UpdateOrderNumber()
    {
        var appDate = await GetAppDateAsync();
        appDate.StoreDate = appDate.StoreDate;
        appDate.PosDate = appDate.PosDate;
        appDate.CurrentOrderNumber = appDate.CurrentOrderNumber + 1;
        appDate.BranchId = appDate.BranchId;

        _unitOfWork.Repository<AppDate>().Update(appDate);
        await _unitOfWork.CompleteAsync();
        return appDate;
    }
}