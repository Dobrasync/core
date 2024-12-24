using Dobrasync.Core.Client.Database.Enums;
using Dobrasync.Core.Client.Main.Services.Auth.Dto;
using Dobrasync.Core.Client.Main.Services.Logger;
using Dobrasync.Core.Client.Main.Services.SystemSetting;
using Dobrasync.Core.Client.Shared.Exceptions;
using Dobrasync.Core.Client.Shared.Helpers;

namespace Dobrasync.Core.Client.Main.Services.Auth;

public class AuthService(ISystemSettingService settings, IApiClient apiClient, ILoggerService logger) : IAuthService
{
    private static readonly HttpHelper httpClient = new();

    public async Task AuthenticateAsync()
    {
        var authToken = await settings.TryGetSettingAsync(ESystemSetting.AUTH_TOKEN);
        if (authToken != null && !string.IsNullOrEmpty(authToken.Value))
        {
            logger.LogFatal("Already authenticated. Logout first.");
            return;
        }

        await BeginDeviceAuth();
    }

    public async Task LogoutAsync()
    {
        var authToken = await settings.TryGetSettingAsync(ESystemSetting.AUTH_TOKEN);
        if (authToken == null || string.IsNullOrEmpty(authToken.Value))
        {
            logger.LogFatal("Not logged in.");
            return;
        }

        var authorityDto = await apiClient.GetIdpAuthorityAsync();
        var authority = authorityDto.Content;
        var clientIdDto = await apiClient.GetIdpDeviceClientIdAsync();
        var clientId = clientIdDto.Content;
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", authToken.Value),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("token_type_hint", "access_token")
        });

        await httpClient.PostAsync<dynamic>($"{authority}/oauth/v2/revoke", requestContent);
        await settings.SetSettingAsync(ESystemSetting.AUTH_TOKEN, null);
        logger.LogInfo("Success.");
    }

    public async Task<bool> IsLoggedInAsync()
    {
        try
        {
            var sessionInfo = await apiClient.GetSessionInfoAsync();
            return true;
        }
        catch (Exception e)
        {
            await LogoutAsync();
            return false;
        }
    }

    public async Task RequireLoggedInAsync()
    {
        if (!await IsLoggedInAsync()) throw new NotLoggedInException();
    }

    private async Task BeginDeviceAuth()
    {
        var authorityDto = await apiClient.GetIdpAuthorityAsync();
        var authority = authorityDto.Content;
        var clientIdDto = await apiClient.GetIdpDeviceClientIdAsync();
        var clientId = clientIdDto.Content;


        var deviceCodeDetailsRaw = await RequestDeviceCode(authority, clientId);
        DeviceAuthCodeDto deviceCodeDetails =
            JsonSerializer.Deserialize<DeviceAuthCodeDto>(deviceCodeDetailsRaw.ToString());
        logger.LogInfo(
            $"Please go to {deviceCodeDetails.verification_uri} and enter the code {deviceCodeDetails.user_code}");

        var accessToken = await PollForAccessToken(authority, clientId, deviceCodeDetails.device_code,
            deviceCodeDetails.interval, deviceCodeDetails.expires_in);
        await settings.SetSettingAsync(ESystemSetting.AUTH_TOKEN, accessToken);

        logger.LogInfo("Sign-in successful.");
    }

    private static async Task<dynamic> RequestDeviceCode(string authority, string clientId)
    {
        var postData = new List<KeyValuePair<string, string>>
        {
            new("client_id", clientId),
            new("scope", "openid profile email")
        };
        var content = new FormUrlEncodedContent(postData);

        return await httpClient.PostAsync<dynamic>($"{authority}/oauth/v2/device_authorization", content);
    }

    private static async Task<string> PollForAccessToken(string authority, string clientId, string deviceCode,
        int interval, int timeout)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("device_code", deviceCode),
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
        });

        var count = 0;
        while (true)
        {
            count++;

            if (count * interval > timeout) throw new TimeoutException($"Timeout {timeout}ms exceeded");

            try
            {
                var result = await httpClient.PostAsync<dynamic>($"{authority}/oauth/v2/token", content);
                if (result.access_token != null) return result.access_token;
            }
            catch (HttpRequestException)
            {
                // Handle HTTP errors here (such as rate limiting or other non-200 responses)
            }

            Task.Delay(TimeSpan.FromSeconds(interval)).Wait(); // Respect polling interval
        }
    }
}