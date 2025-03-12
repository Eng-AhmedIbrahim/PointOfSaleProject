namespace BlazorBase.ERPFrontServices.AppDateServices;

public interface IAppDateService
{
    public Task<AppDateToReturnDto> GetAppDate();
}