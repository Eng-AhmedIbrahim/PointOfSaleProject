namespace BlazorBase.ERPFrontServices.BranchServices;

public interface IBranchService
{
    public Task<IReadOnlyList<BranchToReturnDto>> GetBranches();
}