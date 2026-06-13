using System.Text.Json;
using DBQueryAIEngine.Api.Models;

namespace DBQueryAIEngine.Api.Services;

public sealed class InsightService(ILLMClient llmClient) : IInsightService
{
    /// <summary>
    /// Summarizes query results into concise business insights, using a deterministic fallback if LLM configuration is incomplete.
    /// </summary>
    public async Task<string> SummarizeAsync(AnalyticsIntent intent, QueryResult result, CancellationToken cancellationToken)
    {
        if (result.Rows.Count == 0)
        {
            return "No matching data was returned for the selected date range.";
        }

        var systemPrompt = "You are a senior data analyst. Explain trends briefly, mention notable increases or drops, and avoid inventing causes.";
        var userPrompt = $"""
            User question: {intent.UserQuestion}
            Grain: {intent.Grain}
            Metric: {intent.Metric}
            Rows JSON: {JsonSerializer.Serialize(result.Rows.Take(80))}
            """;

        var summary = await llmClient.CompleteTextAsync(systemPrompt, userPrompt, cancellationToken);
        return string.IsNullOrWhiteSpace(summary) ? BuildFallbackSummary(intent, result) : summary.Trim();
    }

    /// <summary>
    /// Computes a simple trend summary when an LLM provider is not configured.
    /// </summary>
    private static string BuildFallbackSummary(AnalyticsIntent intent, QueryResult result)
    {
        var values = result.Rows
            .Select(r => r.TryGetValue("MetricValue", out var value) && decimal.TryParse(Convert.ToString(value), out var parsed) ? parsed : 0)
            .ToList();

        var first = values.FirstOrDefault();
        var last = values.LastOrDefault();
        var change = first == 0 ? 0 : Math.Round(((last - first) / first) * 100, 2);
        return $"The {intent.grainSafe()} {intent.Metric} trend returned {result.Rows.Count} periods. The metric moved from {first:N2} to {last:N2}, a {change:N2}% change over the selected range.";
    }
}

internal static class IntentSummaryExtensions
{
    /// <summary>
    /// Provides a display-safe grain value for fallback insight text.
    /// </summary>
    public static string grainSafe(this AnalyticsIntent intent) => string.IsNullOrWhiteSpace(intent.Grain) ? "periodic" : intent.Grain;
}
