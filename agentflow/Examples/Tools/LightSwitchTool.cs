using System.Text;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples.Tools;

public class LightSwitchTool : ITool
{
    private readonly IHttpClientFactory httpClientFactory;

    public LightSwitchTool(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public string Name { get; } = "turn_lights_on_off";

    public string Definition { get; } =
""""
def turn_lights_on_off(on_or_off: str) -> str:
    """
    Turns the lights on or off the lights.
    """
    Examples:
        turn_lights_on_off('on') # turns the lights on
        turn_lights_on_off('off') # turns the lights off
    pass

"""".TrimEnd();

    public async Task<string> GetOutputAsync(ConversationThread conversation, string input)
    {
        // URL of the service
        Uri url;

        if (input.Contains("on", StringComparison.Ordinal))
        {
            url = new Uri("http://192.168.50.46:8123/api/services/homeassistant/turn_on");
        }
        else
        {
            url = new Uri("http://192.168.50.46:8123/api/services/homeassistant/turn_off");
        }

        // Your API token
        const string token = "<fixme>";

        // JSON payload
        const string jsonData = "{\"entity_id\": \"light.corner\"}";

        using var client = this.httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        using HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            // Make the POST request
            HttpResponseMessage response = await client.PostAsync(url, content);

            // Read the response
            string result = await response.Content.ReadAsStringAsync();

            return "success";
        }
        catch (Exception ex)
        {
            var logger = this.GetLogger();
            logger.LogError("Failure: {Message}", ex.Message);
            return "falure";
        }
    }
}
