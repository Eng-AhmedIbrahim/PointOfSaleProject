namespace BlazorBase.ERPFrontServices.DineInServices;

public interface IDineInService
{
    public Task<ICollection<TableGroupToReturnDto>> GetTableGroupsAsync();

    public Task<ICollection<TableToReturnDto>> GetTablesByGroupId(int tableGroupId);
    public Task<ICollection<TableToReturnDto>> GetTables();
    public Task<ICollection<CaptainOrderUserToReturnDto>> GetCaptainOrders();
}
