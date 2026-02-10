using POS.Core.Entities.OrderEntity;

namespace POS.Core.Services.Contract.PosFeatureServices;

public interface IPosFeatureSettingsService
{
    Task<List<PosFeatureSetting>> GetSettingsByComputerNameAsync(string computerName);
    Task<bool> UpdateSettingsAsync(string computerName, List<PosFeatureSetting> settings);
    Task<bool> InitializeSettingsForComputerAsync(string computerName);
}
