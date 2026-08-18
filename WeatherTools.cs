using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class WeatherTools
{
    [McpServerTool(Name = "get_forecast")]
    [Description("Get current weather and a daily forecast for a place by name (city, town, etc.) anywhere in the world.")]
    public static async Task<string> GetForecastAsync(
        HttpClient client,
        [Description("Name of the city or place, e.g. 'Hyderabad' or 'Hyderabad, India'.")] string location)
    {
  
        var geocodeUrl = string.Create(CultureInfo.InvariantCulture,
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1");

        using var geocodeDoc = await client.GetJsonDocumentAsync(geocodeUrl);

        if (!geocodeDoc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            return $"Couldn't find a location matching \"{location}\".";
        }

        var match = results[0];
        var latitude = match.GetProperty("latitude").GetDouble();
        var longitude = match.GetProperty("longitude").GetDouble();
        var resolvedName = match.GetProperty("name").GetString();
        var country = match.TryGetProperty("country", out var c) ? c.GetString() : null;

   
        var forecastUrl = string.Create(CultureInfo.InvariantCulture,
            $"/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=auto");

        using var doc = await client.GetJsonDocumentAsync(forecastUrl);
        var root = doc.RootElement;

        var current = root.GetProperty("current_weather");
        var currentTemp = current.GetProperty("temperature").GetDouble();

        var daily = root.GetProperty("daily");
        var dates = daily.GetProperty("time").EnumerateArray().Select(d => d.GetString()).ToList();
        var highs = daily.GetProperty("temperature_2m_max").EnumerateArray().Select(t => t.GetDouble()).ToList();
        var lows = daily.GetProperty("temperature_2m_min").EnumerateArray().Select(t => t.GetDouble()).ToList();
        var rainChance = daily.GetProperty("precipitation_probability_max").EnumerateArray().Select(p => p.GetInt32()).ToList();

        var lines = new List<string>
        {
            $"Forecast for {resolvedName}{(country is null ? "" : $", {country}")}:",
            $"Current temperature: {currentTemp}°C"
        };

        for (int i = 0; i < dates.Count; i++)
        {
            lines.Add($"{dates[i]}: High {highs[i]}°C, Low {lows[i]}°C, Rain chance {rainChance[i]}%");
        }

        return string.Join("\n", lines);
    }
}