namespace SaasApi.Application.Common.Settings;

public class ResendSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "SaaS API";
}
