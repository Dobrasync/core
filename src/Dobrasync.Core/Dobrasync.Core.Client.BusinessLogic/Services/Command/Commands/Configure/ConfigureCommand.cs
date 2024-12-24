

using Dobrasync.Core.Client.Database.Enums;
using Dobrasync.Core.Client.Main.Const;
using Dobrasync.Core.Client.Main.Services.Command.Commands.Base;
using Dobrasync.Core.Client.Main.Services.Logger;
using Dobrasync.Core.Client.Main.Services.SystemSetting;

namespace Dobrasync.Core.Client.Main.Services.Command.Commands.Configure;

public class ConfigureCommand(ISystemSettingService settings, ILoggerService logger) : ICommand
{
    public string GetName()
    {
        return "config";
    }

    public async Task<int> Execute(string[] args)
    {
        var result = Parser.Default.ParseArguments<ConfigureOptions>(args);
        if (result.Errors.Any()) return 1;

        if (result.Value.List)
        {
            var list = await settings.GetAllSystemSettingsAsync();
            logger.LogInfo(JsonSerializer.Serialize(list));
            return ExitCodes.Success;
        }

        if (!string.IsNullOrEmpty(result.Value.DefaultDirectory))
            await settings.SetSettingAsync(ESystemSetting.DEFAULT_LIBRARY_DIRECTORY, result.Value.DefaultDirectory);
        if (!string.IsNullOrEmpty(result.Value.DefaultRemote))
            await settings.SetSettingAsync(ESystemSetting.REMOTE_ADDRESS, result.Value.DefaultRemote);
        if (!string.IsNullOrEmpty(result.Value.TempBlockDir))
            await settings.SetSettingAsync(ESystemSetting.TEMP_BLOCK_DIRECTORY, result.Value.TempBlockDir);

        return 0;
    }
}