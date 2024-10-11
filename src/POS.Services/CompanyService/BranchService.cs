namespace POS.Services.CompanyService;

public class BranchService : IBranchService
{
    private readonly IUnitOfWork _unitOfWork;

    public BranchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Branch?> CrateBranchAsync(Branch branch)
    {
        try
        {
            if (branch is null)
                return null;

            await _unitOfWork.Repository<Branch>().AddAsync(branch);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            return branch;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating the company.");
            return null;
        }
    }

    public async Task<Branch?>? GetBranchByIdAsync(int BranchId)
    {
        try
        {
            var branch = await _unitOfWork.Repository<Branch>().GetByIdAsync(BranchId);

            if (branch is null)
                return null;

            return branch;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Company With This Id {companyId}", BranchId);
            return null;
        }
    }

    public async Task<IReadOnlyList<Branch>?> GetBranchesAsync()
    {
        try
        {
            var branches = await _unitOfWork.Repository<Branch>().GetAllAsync();

            if (branches is null)
                return null;

            return branches;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Companies");
            return null;
        }
    }

    public async Task<Branch?> UpdateBranchAsync(Branch oldBranch, Branch newBranch)
    {
        try
        {
            oldBranch.Id = oldBranch.Id;
    
            if (!string.IsNullOrEmpty(newBranch.Name))
            {
                oldBranch.Name = newBranch.Name;
                oldBranch.NormalizedName = newBranch.Name.ToUpper();
            }
            if (!string.IsNullOrEmpty(newBranch.Description))
                oldBranch.Description = newBranch.Description;
            if (!string.IsNullOrEmpty(newBranch.Address))
                oldBranch.Address = newBranch.Address;
            if (!string.IsNullOrEmpty(newBranch.ImagePath))
                oldBranch.ImagePath = newBranch.ImagePath;

            if (newBranch.LogoWidth != oldBranch.LogoWidth && newBranch.LogoWidth != 0)
                oldBranch.LogoWidth = newBranch.LogoWidth;

            if (newBranch.LogoHeight != oldBranch.LogoHeight && newBranch.LogoHeight != 0)
                oldBranch.LogoHeight = newBranch.LogoHeight;

            if (!string.IsNullOrEmpty(newBranch.Phone1))
                oldBranch.Phone1 = newBranch.Phone1;

            if (!string.IsNullOrEmpty(newBranch.Phone2))
                oldBranch.Phone2 = newBranch.Phone2;

            if (newBranch.Active != oldBranch.Active)
                oldBranch.Active = newBranch.Active;

            if (newBranch.Suspend != oldBranch.Suspend)
                oldBranch.Suspend = newBranch.Suspend;

            _unitOfWork.Repository<Branch>().Update(oldBranch);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            return oldBranch ?? null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Occur During Update Company That Have Id {companyId}", oldBranch.Id);
            return null;
        }
    }
    public async Task<bool> DeleteBranch(Branch branch)
    {
        try
        {
            if (branch is null)
                return false;

            _unitOfWork.Repository<Branch>().Delete(branch);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return false;
            return true;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cant Delete Company With Id {companyId}", branch.Id);
            return false;
        }
    }
}