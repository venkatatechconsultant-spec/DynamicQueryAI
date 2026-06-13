using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DBQueryAIEngine.Api.Options;
using Microsoft.Extensions.Options;

namespace DBQueryAIEngine.Api.Services;

public sealed class OpenAIChatClient(HttpClient httpClient, IOptions<OpenAIOptions> options) : ILLMClient
{
    /// <summary>
    /// Requests a JSON-only chat completion from OpenAI or Azure OpenAI when credentials are configured.
    /// </summary>
    public Task<string?> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        return CompleteAsync(systemPrompt, userPrompt, true, cancellationToken);
    }

    /// <summary>
    /// Requests a text chat completion from OpenAI or Azure OpenAI when credentials are configured.
    /// </summary>
    public Task<string?> CompleteTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        return CompleteAsync(systemPrompt, userPrompt, false, cancellationToken);
    }

    /// <summary>
    /// Sends the provider-specific chat completion request while allowing placeholder configuration to fall back gracefully.
    /// </summary>
    private async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, bool jsonOnly, CancellationToken cancellationToken)
    {
        var cfg = options.Value;
        if (string.IsNullOrWhiteSpace(cfg.ApiKey) || cfg.ApiKey.StartsWith('<') || string.IsNullOrWhiteSpace(cfg.Endpoint) || string.IsNullOrWhiteSpace(cfg.Model))
        {
            return null;
        }

        var isAzure = cfg.Provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase);
        var requestUri = isAzure
            ? $"{cfg.Endpoint.TrimEnd('/')}/openai/deployments/{cfg.Model}/chat/completions?api-version={cfg.ApiVersion}"
            : $"{cfg.Endpoint.TrimEnd('/')}/v1/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = isAzure ? null : new AuthenticationHeaderValue("Bearer", cfg.ApiKey);
        if (isAzure)
        {
            request.Headers.Add("api-key", cfg.ApiKey);
        }

        var payload = new Dictionary<string, object?>
        {
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["temperature"] = cfg.Temperature,
            ["max_tokens"] = cfg.MaxTokens
        };

        if (!isAzure)
        {
            payload["model"] = cfg.Model;
        }

        if (jsonOnly)
        {
            payload["response_format"] = new { type = "json_object" };
        }

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }
}
