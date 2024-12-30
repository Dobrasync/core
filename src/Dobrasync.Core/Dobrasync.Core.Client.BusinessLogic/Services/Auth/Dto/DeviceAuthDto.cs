namespace Dobrasync.Core.Client.BusinessLogic.Services.Auth.Dto;

public class DeviceAuthDto
{
    public string client_id { get; set; } = default!;
    public string scope { get; set; } = default!;
}