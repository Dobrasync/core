using Dobrasync.Core.Client.BusinessLogic.Services.Logger;
using Dobrasync.Core.Client.Common.Exceptions;
using Dobrasync.Core.Client.Database.Entities;
using Dobrasync.Core.Client.Database.Enums;
using Dobrasync.Core.Client.Database.Repo;
using Microsoft.EntityFrameworkCore;

namespace Dobrasync.Core.Client.BusinessLogic.Services.SystemSetting;

public class SystemSettingService(IRepoWrapper repoWrap, ILoggerService logger) : ISystemSettingService
{
    public async Task<List<SystemSettingEntity>> GetAllSystemSettingsAsync()
    {
        return await repoWrap.SystemSettingRepo.QueryAll().ToListAsync();
    }

    public async Task<SystemSettingEntity> SetSettingAsync(ESystemSetting key, string? newValue)
    {
        var setting = await repoWrap.DbContext.SystemSettings.FirstOrDefaultAsync(x => x.Id == key.ToString());
        if (setting == null) throw new EntityNotFoundException();

        setting.Value = newValue;

        await repoWrap.SystemSettingRepo.UpdateAsync(setting);
        logger.LogInfo($"System setting {key} updated to '{newValue}'.");
        return setting;
    }

    public async Task<SystemSettingEntity?> TryGetSettingAsync(ESystemSetting key)
    {
        var setting = await repoWrap.SystemSettingRepo.QueryAll().FirstOrDefaultAsync(x => x.Id == key.ToString());
        return setting;
    }

    public async Task<SystemSettingEntity> GetSettingThrowsAsync(ESystemSetting key)
    {
        var set = await TryGetSettingAsync(key);
        if (set == null) throw new KeyNotFoundException($"The setting for {key} was not found.");

        return set;
    }

    public async Task<string> GetSettingValueThrowsAsync(ESystemSetting key)
    {
        var s = await GetSettingValueAsync(key);
        if (s == null) throw new ArgumentException("Setting has no value.");

        return s;
    }

    public async Task<string?> GetSettingValueAsync(ESystemSetting key)
    {
        var s = await GetSettingThrowsAsync(key);
        if (s.Value == null) return null;

        return s.Value;
    }

    public string? GetSettingValue(ESystemSetting key)
    {
        var s = repoWrap.SystemSettingRepo.QueryAll().FirstOrDefault(x => x.Id == key.ToString());
        if (s == null) return null;

        return s.Value;
    }
}