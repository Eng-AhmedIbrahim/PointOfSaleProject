using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.PosFeatureServices;
using Pos.Repository.Data.DataSeed;

namespace POS.Services.PosFeatureServices;

public class PosFeatureSettingsService : IPosFeatureSettingsService
{
    private readonly IUnitOfWork _unitOfWork;

    public PosFeatureSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PosFeatureSetting>> GetSettingsByComputerNameAsync(string computerName)
    {
        var spec = new POS.Core.Specifications.BaseSpecifications<PosFeatureSetting>(s => s.ComputerName == computerName);
        var settings = await _unitOfWork.Repository<PosFeatureSetting>().GetAllWithSpecificationAsync(spec);

        if (settings == null || !settings.Any())
        {
            // If no settings found for this computer, initialize with defaults
            await InitializeSettingsForComputerAsync(computerName);
            settings = await _unitOfWork.Repository<PosFeatureSetting>().GetAllWithSpecificationAsync(spec);
        }

        return settings.ToList();
    }

    public async Task<bool> InitializeSettingsForComputerAsync(string computerName)
    {
        // Fetch default settings (where ComputerName is null or specifically marked as default)
        // Or fetch from seed file if not in DB.
        // Assuming there are "template" settings with ComputerName = "Default" or null
        var spec = new POS.Core.Specifications.BaseSpecifications<PosFeatureSetting>(s => string.IsNullOrEmpty(s.ComputerName) || s.ComputerName == "Default");
        var defaultSettings = await _unitOfWork.Repository<PosFeatureSetting>().GetAllWithSpecificationAsync(spec);
        
        if (defaultSettings == null || !defaultSettings.Any())
        {
             // If no defaults in DB, try to load from JSON
             try 
             {
                 var settings = await PosDbContextDataSeed.GetDataFromJsonFile<PosFeatureSetting>("posSettings.json");
                 if (settings != null && settings.Any())
                 {
                     foreach (var setting in settings)
                     {
                         setting.Id = 0; // Reset ID for new entry
                         setting.ComputerName = computerName;
                         // Ensure defaults
                         if (string.IsNullOrEmpty(setting.NameEn)) setting.NameEn = setting.FeatureName; 
                         await _unitOfWork.Repository<PosFeatureSetting>().AddAsync(setting);
                     }
                     await _unitOfWork.CompleteAsync();
                     return true;
                 }
             }
             catch
             {
                 // Log error or fallback
             }
             return false;
        }

        foreach (var setting in defaultSettings)
        {
            var newSetting = new PosFeatureSetting
            {
                FeatureName = setting.FeatureName,
                NameEn = setting.NameEn,
                NameAr = setting.NameAr,
                ModuleName = setting.ModuleName,
                Value = setting.Value,
                ComputerName = computerName
            };
            await _unitOfWork.Repository<PosFeatureSetting>().AddAsync(newSetting);
        }

        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> UpdateSettingsAsync(string computerName, List<PosFeatureSetting> settings)
    {
        var existingSpec = new POS.Core.Specifications.BaseSpecifications<PosFeatureSetting>(s => s.ComputerName == computerName);
        var existingSettings = await _unitOfWork.Repository<PosFeatureSetting>().GetAllWithSpecificationAsync(existingSpec);

        foreach (var setting in settings)
        {
            var existing = existingSettings.FirstOrDefault(s => s.FeatureName == setting.FeatureName);
            if (existing != null)
            {
                existing.Value = setting.Value;
                // Update names if they changed in definition (optional)
                existing.NameEn = setting.NameEn;
                existing.NameAr = setting.NameAr;
                _unitOfWork.Repository<PosFeatureSetting>().Update(existing);
            }
            else
            {
                setting.Id = 0;
                setting.ComputerName = computerName;
                await _unitOfWork.Repository<PosFeatureSetting>().AddAsync(setting);
            }
        }

        return await _unitOfWork.CompleteAsync() > 0;
    }
}
