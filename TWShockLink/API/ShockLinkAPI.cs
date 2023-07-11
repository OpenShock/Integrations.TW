using System.Drawing;
using System.Text;
using MelonLoader;
using Newtonsoft.Json;

namespace ShockLink.Integrations.TW.API;

// ReSharper disable once InconsistentNaming
internal static class ShockLinkAPI
{
    private static HttpClient? _client;

    public static void Reload(string endpoint, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ShockLinkIntegrationTW.Logger.Error("No ApiToken Configured");
            return;
        }
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            ShockLinkIntegrationTW.Logger.Error("No Endpoint Configured");
            return;
        }

        try
        {
            _client = new();
            _client.BaseAddress = new Uri(endpoint);
            _client.DefaultRequestHeaders.Add("ShockLinkToken", token);
            ShockLinkIntegrationTW.Logger.Msg("Setup ShockLink API Client");
        }
        catch (UriFormatException e)
        {
            ShockLinkIntegrationTW.Logger.Error("The Base API URL entered is not a valid URL! Please check your settings!");
        }
    }

    public static async Task Control(params Control[] data)
    {
        if (_client == null)
        {
            ShockLinkIntegrationTW.Logger.Error("No ApiToken or Endpoint configured");
            return;
        }
        try
        {
            var messageString = JsonConvert.SerializeObject(data, Formatting.None);
            var message = new HttpRequestMessage(HttpMethod.Post, "/1/shockers/control");
            message.Content = new StringContent(messageString, Encoding.UTF8, "application/json");

            var result = await _client.SendAsync(message);
            if (!result.IsSuccessStatusCode)
            {
                ShockLinkIntegrationTW.Logger.Error(result.StatusCode);
                ShockLinkIntegrationTW.Logger.Error(await result.Content.ReadAsStringAsync());
            }

        }
        catch (Exception ex)
        {
            ShockLinkIntegrationTW.Logger.Error("Send failed", ex);
            throw;
        }
    }

    public static async Task<ShockerWithDevice?> GetShocker(Guid shockerId)
    {
        if (_client == null)
        {
            ShockLinkIntegrationTW.Logger.Error("No ApiToken or Endpoint configured");
            return null;
        }
        var response = await _client.GetAsync($"/1/shockers/{shockerId}");
        if (!response.IsSuccessStatusCode)
        {
            ShockLinkIntegrationTW.Logger.Warning("Error while getting shocker info", shockerId, response.StatusCode, await response.Content.ReadAsStringAsync());
            return null;
        }
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonConvert.DeserializeObject<BaseResponse<ShockerWithDevice>>(content);
        return json?.Data;
    }
    
    public static async Task<IEnumerable<LogEntry>?> GetShockerLogs(Guid shockerId)
    {
        if (_client == null)
        {
            ShockLinkIntegrationTW.Logger.Error("No ApiToken or Endpoint configured");
            return null;
        }
        
        var response = await _client.GetAsync($"/1/shockers/{shockerId}/logs");
        ShockLinkIntegrationTW.Logger.Msg(response.StatusCode);
        if (!response.IsSuccessStatusCode)
        {
            ShockLinkIntegrationTW.Logger.Warning("Error while getting shocker logs", shockerId, response.StatusCode, await response.Content.ReadAsStringAsync());
            return null;
        }
        var content = await response.Content.ReadAsStringAsync();
        ShockLinkIntegrationTW.Logger.Msg(content);
        var json = JsonConvert.DeserializeObject<BaseResponse<IEnumerable<LogEntry>>>(content);
        return json?.Data;
    }
}