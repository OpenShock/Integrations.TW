using System.Drawing;
using System.Text;
using MelonLoader;
using Newtonsoft.Json;

namespace ShockLink.Integrations.TW.API;

// ReSharper disable once InconsistentNaming
internal static class ShockLinkAPI
{
    private static HttpClient? _client;
    private static readonly MelonLogger.Instance Logger = new(nameof(ShockLinkIntegrationTW), Color.LawnGreen);

    public static void Reload(string endpoint, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.Msg("No ApiToken Configured");
            return;
        }
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Logger.Msg("No Endpoint Configured");
            return;
        }

        _client = new();
        _client.BaseAddress = new Uri(endpoint);
        _client.DefaultRequestHeaders.Add("ShockLinkToken", token);
        Logger.Msg("Setup Client");
    }

    public static async Task Control(params Control[] data)
    {
        if (_client == null)
        {
            Logger.Msg("No ApiToken or Endpoint configured");
            return;
        }
        try
        {
            var messageString = JsonConvert.SerializeObject(data, Formatting.None);
            Logger.Msg(messageString);

            var message = new HttpRequestMessage(HttpMethod.Post, "/1/shockers/control");
            message.Content = new StringContent(messageString, Encoding.UTF8, "application/json");

            var result = await _client.SendAsync(message);
            if (!result.IsSuccessStatusCode)
            {
                Logger.Msg(result.StatusCode);
                Logger.Msg(await result.Content.ReadAsStringAsync());
            }

        }
        catch (Exception ex)
        {
            Logger.Error("Send failed", ex);
            throw;
        }
    }

    public static async Task<ShockerWithDevice?> GetShocker(Guid shockerId)
    {
        if (_client == null)
        {
            Logger.Msg("No ApiToken or Endpoint configured");
            return null;
        }
        var response = await _client.GetAsync($"/1/shockers/{shockerId}");
        if (!response.IsSuccessStatusCode)
        {
            Logger.Warning("Error while getting shocker info", shockerId, response.StatusCode, await response.Content.ReadAsStringAsync());
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
            Logger.Msg("No ApiToken or Endpoint configured");
            return null;
        }
        
        var response = await _client.GetAsync($"/1/shockers/{shockerId}/logs");
        Logger.Msg(response.StatusCode);
        if (!response.IsSuccessStatusCode)
        {
            Logger.Warning("Error while getting shocker logs", shockerId, response.StatusCode, await response.Content.ReadAsStringAsync());
            return null;
        }
        var content = await response.Content.ReadAsStringAsync();
        Logger.Msg(content);
        var json = JsonConvert.DeserializeObject<BaseResponse<IEnumerable<LogEntry>>>(content);
        return json?.Data;
    }
}