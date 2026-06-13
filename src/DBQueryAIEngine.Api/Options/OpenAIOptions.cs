namespace DBQueryAIEngine.Api.Options;

public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";
    public string Provider { get; set; } = "AzureOpenAI";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-10-21";
    public double Temperature { get; set; } = 0.1;
    public int MaxTokens { get; set; } = 1600;
}
