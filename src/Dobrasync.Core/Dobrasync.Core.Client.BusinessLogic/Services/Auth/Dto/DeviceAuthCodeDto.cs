

namespace Dobrasync.Core.Client.Main.Services.Auth.Dto;

public class DeviceAuthCodeDto
{
    public string device_code { get; set; } = default!;
    public string user_code { get; set; } = default!;
    public string verification_uri { get; set; } = default!;
    public string verification_uri_complete { get; set; } = default!;
    public int expires_in { get; set; } = default!;
    public int interval { get; set; } = default!;
}