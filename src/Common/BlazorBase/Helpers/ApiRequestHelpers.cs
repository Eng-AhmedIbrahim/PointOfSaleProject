using BlazorBase.API;
using Microsoft.Extensions.Logging;

namespace BlazorBase.Helpers;

public static class ApiRequestHelpers
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<HttpResponseMessage?> SendApiRequest(
        Func<Task<HttpResponseMessage>> apiRequest, ILogger logger)
    {
        try
        {
            return await apiRequest();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("API request failed: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    public static async Task<string> GetResponseMessage(
        HttpResponseMessage response, string successMessage)
    {
        var content = await response.Content.ReadAsStringAsync();

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
            case HttpStatusCode.NoContent:
            case HttpStatusCode.Created:
                return successMessage;
            case HttpStatusCode.BadRequest:
            case HttpStatusCode.NotFound:
                var errorResponse = DeserializeResponseContent<ApiResponse>(content);
                return $" {errorResponse!.GetType()} Bad request or Not found";
            default:
                return $"Unexpected error: {response.StatusCode} 🤔";
        }
    }

    public static T? DeserializeResponseContent<T>(string content)
        => JsonSerializer.Deserialize<T>(content, Options);
}