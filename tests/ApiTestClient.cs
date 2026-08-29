using System.Net.Http.Json;
using System.Text.Json;

namespace Planara.Privacy.Tests;

public static class ApiTestClient
{
    public static async Task<JsonDocument> PostAsync(
        this HttpClient client,
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { query, variables };

        var response = await client.PostAsJsonAsync("/graphql", payload, cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);

        return json ?? throw new InvalidOperationException("Empty GraphQL response");
    }

    public static JsonElement? GetErrors(this JsonDocument document)
        => document.RootElement.TryGetProperty("errors", out var errors) ? errors : null;

    public static JsonElement GetData(this JsonDocument document)
        => document.RootElement.GetProperty("data");

    public static void AsUser(this HttpClient client, Guid userId)
    {
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
    }
}