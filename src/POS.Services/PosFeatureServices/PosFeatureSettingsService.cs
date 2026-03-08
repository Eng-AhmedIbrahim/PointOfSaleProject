namespace POS.Services.PosFeatureServices;

public class PosFeatureSettingsService : IPosFeatureSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PosFeatureSettingsService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<PosFeatureSettingToReturnDto>> GetSettingsByComputerNameAsync(string computerName)
    {
        var spec = new POS.Core.Specifications.BaseSpecifications<PosFeatureSetting>(s => s.ComputerName == computerName);
        var settings = await _unitOfWork.Repository<PosFeatureSetting>().GetAllWithSpecificationAsync(spec);

        if (settings == null || !settings.Any())
        {
            // If no settings found for this computer, initialize with defaults
            await InitializeSettingsForComputerAsync(computerName);
            settings = await _unitOfWork.Repository<PosFeatureSetting>().GetAllWithSpecificationAsync(spec);
        }

        return _mapper.Map<List<PosFeatureSettingToReturnDto>>(settings);
    }

    public async Task<bool> InitializeSettingsForComputerAsync(string computerName)
    {
        // 1. Check if settings for this specific device already exist
        var computerSpec = new POS.Core.Specifications.BaseSpecifications<PosFeatureSetting>(s => s.ComputerName == computerName);
        var existingSettings = await _unitOfWork.Repository<PosFeatureSetting>().GetAllWithSpecificationAsync(computerSpec);
        
        if (existingSettings != null && existingSettings.Any())
        {
            return true; // Already initialized for this computer
        }

        // 2. Load default settings from JSON file
        // This ensures every new device starts with the latest defaults from the configuration file
        var settingsFromJson = await PosDbContextDataSeed.GetDataFromJsonFile<PosFeatureSetting>("posSettings.json");
        
        if (settingsFromJson == null || !settingsFromJson.Any())
        {
            return false;
        }

        // 3. Create settings for this specific computer
        foreach (var setting in settingsFromJson)
        {
            var newSetting = new PosFeatureSetting
            {
                FeatureName = setting.FeatureName,
                NameEn = setting.NameEn,
                NameAr = setting.NameAr,
                ModuleName = setting.ModuleName,
                ScreenName = setting.ScreenName,
                Value = setting.Value,
                ComputerName = computerName // Assign the current computer name
            };
            await _unitOfWork.Repository<PosFeatureSetting>().AddAsync(newSetting);
        }

        // Save all settings at once
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> UpdateSettingsAsync(string computerName, List<PosFeatureSettingToReturnDto> settings)
    {
        var existingSpec = new POS.Core.Specifications.BaseSpecifications<PosFeatureSetting>(s => s.ComputerName == computerName);
        var existingSettings = await _unitOfWork.Repository<PosFeatureSetting>().GetAllWithSpecificationAsync(existingSpec);

        foreach (var settingDto in settings)
        {
            var existing = existingSettings.FirstOrDefault(s => s.FeatureName == settingDto.FeatureName);
            if (existing != null)
            {
                existing.Value = settingDto.Value;
                existing.NameEn = settingDto.NameEn;
                existing.NameAr = settingDto.NameAr;
                _unitOfWork.Repository<PosFeatureSetting>().Update(existing);
            }
            else
            {
                var setting = _mapper.Map<PosFeatureSetting>(settingDto);
                setting.Id = 0;
                setting.ComputerName = computerName;
                await _unitOfWork.Repository<PosFeatureSetting>().AddAsync(setting);
            }
        }

        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName, string? computerName = null)
    {
        var spec = new POS.Core.Specifications.BaseSpecifications<PosFeatureSetting>(s => 
            s.FeatureName == featureName && 
            (computerName == null || s.ComputerName == computerName));
        
        var setting = await _unitOfWork.Repository<PosFeatureSetting>().GetByIdWithSpecificationTrackedAsync(spec);
        return setting?.Value ?? false;
    }
}
