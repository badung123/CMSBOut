namespace Bankout.API.Helpers;

public class PartnerBankSettings
{
    public string BaseUrl { get; set; } = "http://207.148.121.241:6999";
    public string ApiKey { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}
