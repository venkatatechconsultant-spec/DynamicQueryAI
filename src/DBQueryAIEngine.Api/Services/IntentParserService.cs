using System.Text.Json;
using DBQueryAIEngine.Api.Models;

namespace DBQueryAIEngine.Api.Services;

public sealed class IntentParserService(ILLMClient llmClient) : IIntentParserService
{
    /// <summary>
    /// Parses the user's question into a constrained analytics intent, using the LLM when configured and a deterministic fallback otherwise.
    /// </summary>
    public async Task<AnalyticsIntent> ParseAsync(string message, IReadOnlyList<WarehouseTableSchema> schema, CancellationToken cancellationToken)
    {
        var systemPrompt = """
            You convert analytics questions into JSON only.
            Choose one grain: daily, weekly, monthly, quarterly.
            Choose one table only from the provided schema table names.
            Use null when unsure. Do not include markdown.
            JSON shape: {"metric":"sales|usage|inspections|media|vouchers|transactions","grain":"daily|weekly|monthly|quarterly","tableName":"...","dimension":null,"startDate":null,"endDate":null}
            """;
        var userPrompt = $"Question: {message}\nTables: {string.Join(", ", schema.Select(s => s.TableName))}";

        var json = await llmClient.CompleteJsonAsync(systemPrompt, userPrompt, cancellationToken);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return NormalizeIntent(
                    message,
                    root.TryGetProperty("metric", out var metric) ? metric.GetString() : null,
                    root.TryGetProperty("grain", out var grain) ? grain.GetString() : null,
                    root.TryGetProperty("tableName", out var table) ? table.GetString() : null,
                    root.TryGetProperty("dimension", out var dimension) ? dimension.GetString() : null,
                    root.TryGetProperty("startDate", out var start) && start.ValueKind == JsonValueKind.String ? DateTime.TryParse(start.GetString(), out var s) ? s : null : null,
                    root.TryGetProperty("endDate", out var end) && end.ValueKind == JsonValueKind.String ? DateTime.TryParse(end.GetString(), out var e) ? e : null : null,
                    schema);
            }
            catch (JsonException)
            {
                // Fall back to deterministic parsing when an LLM provider returns non-JSON text.
            }
        }

        return ParseWithHeuristics(message, schema);
    }

    /// <summary>
    /// Applies predictable defaults for incomplete intent data so downstream SQL generation remains bounded.
    /// </summary>
    private static AnalyticsIntent NormalizeIntent(string message, string? metric, string? grain, string? tableName, string? dimension, DateTime? startDate, DateTime? endDate, IReadOnlyList<WarehouseTableSchema> schema)
    {
        var normalizedMetric = string.IsNullOrWhiteSpace(metric) ? "sales" : metric.Trim().ToLowerInvariant();
        var normalizedGrain = NormalizeGrain(grain, message);
        var normalizedTable = ResolveTable(tableName, normalizedMetric, message, schema);
        var end = endDate?.Date.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1);
        var start = startDate?.Date ?? normalizedGrain switch
        {
            "daily" => end.AddDays(-30),
            "weekly" => end.AddDays(-7 * 12),
            "monthly" => end.AddMonths(-12),
            "quarterly" => end.AddMonths(-24),
            _ => end.AddDays(-30)
        };

        var timeColumn = ResolveDateColumn(schema.First(s => s.TableName == normalizedTable));
        return new AnalyticsIntent(normalizedMetric, normalizedGrain, timeColumn, start, end, dimension, normalizedTable, message);
    }

    /// <summary>
    /// Provides a zero-dependency parser so the API remains useful before OpenAI credentials are configured.
    /// </summary>
    private static AnalyticsIntent ParseWithHeuristics(string message, IReadOnlyList<WarehouseTableSchema> schema)
    {
        var lower = message.ToLowerInvariant();
        var metric = lower.Contains("usage") ? "usage"
            : lower.Contains("inspection") ? "inspections"
            : lower.Contains("media") ? "media"
            : lower.Contains("voucher") ? "vouchers"
            : lower.Contains("transaction") ? "transactions"
            : "sales";

        return NormalizeIntent(message, metric, NormalizeGrain(null, message), null, null, null, null, schema);
    }

    /// <summary>
    /// Resolves the requested time grain from explicit intent data or natural-language keywords.
    /// </summary>
    private static string NormalizeGrain(string? grain, string message)
    {
        var text = $"{grain} {message}".ToLowerInvariant();
        if (text.Contains("quarter")) return "quarterly";
        if (text.Contains("month")) return "monthly";
        if (text.Contains("week")) return "weekly";
        return "daily";
    }

    /// <summary>
    /// Maps business terms to the nearest configured fact table while honoring the allowed schema list.
    /// </summary>
    private static string ResolveTable(string? tableName, string metric, string message, IReadOnlyList<WarehouseTableSchema> schema)
    {
        var exact = schema.FirstOrDefault(s => string.Equals(s.TableName, tableName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact.TableName;

        var lower = $"{metric} {message}".ToLowerInvariant();
        var preferred = lower.Contains("inspection") ? new[] { "FactInspections", "Fact Inspections" }
            : lower.Contains("media") ? new[] { "FactMediaDetails" }
            : lower.Contains("voucher") ? new[] { "VoucherTransactionDetails" }
            : lower.Contains("transaction") || lower.Contains("trx") ? new[] { "TrxDetails" }
            : new[] { "FactSales" };

        return schema.FirstOrDefault(s => preferred.Contains(s.TableName, StringComparer.OrdinalIgnoreCase))?.TableName
            ?? schema.First().TableName;
    }

    /// <summary>
    /// Selects the most likely date column from a table schema for period-based analytics.
    /// </summary>
    private static string ResolveDateColumn(WarehouseTableSchema table)
    {
        var candidates = new[] { "SalesDate", "TransactionDate", "TrxDate", "InspectionDate", "CreatedDate", "DateKey" };
        return table.Columns.FirstOrDefault(c => candidates.Contains(c.ColumnName, StringComparer.OrdinalIgnoreCase))?.ColumnName
            ?? table.Columns.FirstOrDefault(c => c.DataType.Contains("date", StringComparison.OrdinalIgnoreCase))?.ColumnName
            ?? throw new InvalidOperationException($"No date column found for {table.TableName}.");
    }
}
