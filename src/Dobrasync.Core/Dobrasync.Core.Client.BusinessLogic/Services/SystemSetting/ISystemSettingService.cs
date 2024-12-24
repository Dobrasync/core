using Dobrasync.Core.Client.Database.Entities;
using Dobrasync.Core.Client.Database.Enums;

namespace Dobrasync.Core.Client.Main.Services.SystemSetting;

public interface ISystemSettingService
{
    public Task<List<SystemSettingEntity>> GetAllSystemSettingsAsync();
    public Task<SystemSettingEntity> SetSettingAsync(ESystemSetting key, string? newValue);
    public Task<SystemSettingEntity?> TryGetSettingAsync(ESystemSetting key);
    public Task<SystemSettingEntity> GetSettingThrowsAsync(ESystemSetting key);
    public Task<string> GetSettingValueThrowsAsync(ESystemSetting key);
    public Task<string?> GetSettingValueAsync(ESystemSetting key);
    public string? GetSettingValue(ESystemSetting key);
}