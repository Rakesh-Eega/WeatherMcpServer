using System.Text.Json;

internal static class HttpClientExtensions
{
    public static async Task<JsonDocument> GetJsonDocumentAsync(this HttpClient client, string requestUri)
    {
        using var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}