using DBQueryAIEngine.Api.Models;
using DBQueryAIEngine.Api.Options;
using Microsoft.Extensions.Options;

namespace DBQueryAIEngine.Api.Services;

public sealed class SqlGenerationService(IOptions<WarehouseOptions> options) : ISqlGenerationService
{
    /// <summary>
    /// Generates parameterized SQL from known warehouse schema and approved aggregation patterns.
    /// </summary>
    public Task<GeneratedSql> GenerateAsync(AnalyticsIntent intent, IReadOnlyList<WarehouseTableSchema> schema, CancellationToken cancellationToken)
    {
        var table = schema.FirstOrDefault(s => string.Equals(s.TableName, intent.TableName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Table {intent.TableName} is not allowed.");

        var dateColumn = table.Columns.Any(c => c.ColumnName == intent.TimeColumn)
            ? intent.TimeColumn
            : table.Columns.First(c => c.DataType.Contains("date", StringComparison.OrdinalIgnoreCase)).ColumnName;

        var metricColumn = ResolveMetricColumn(table, intent.Metric);
        var periodExpression = BuildPeriodExpression(dateColumn, intent.Grain);
        var metricExpression = metricColumn is null
            ? "COUNT_BIG(*)"
            : $"SUM(TRY_CONVERT(decimal(18,2), [{metricColumn.ColumnName}]))";

        var sql = $"""
            SELECT TOP (@MaxRows)
                   {periodExpression} AS PeriodStart,
                   {metricExpression} AS MetricValue
            FROM [{table.SchemaName}].[{table.TableName}]
            WHERE [{dateColumn}] >= @StartDate
              AND [{dateColumn}] < @EndDate
            GROUP BY {periodExpression}
            ORDER BY PeriodStart;
            """;

        var parameters = new Dictionary<string, object?>
        {
            ["@StartDate"] = intent.StartDate ?? DateTime.UtcNow.Date.AddDays(-30),
            ["@EndDate"] = intent.EndDate ?? DateTime.UtcNow.Date.AddDays(1),
            ["@MaxRows"] = options.Value.MaxRows
        };

        var chart = new ChartDefinition("line", "PeriodStart", "MetricValue", $"{intent.Grain} {intent.Metric} trend");
        return Task.FromResult(new GeneratedSql(sql, parameters, chart, []));
    }

    /// <summary>
    /// Chooses a numeric metric column using business-friendly naming conventions and table metadata.
    /// </summary>
    private static WarehouseColumnSchema? ResolveMetricColumn(WarehouseTableSchema table, string metric)
    {
        var names = metric switch
        {
            "usage" => new[] { "UsageAmount", "UsageCount", "Quantity", "Qty" },
            "vouchers" => new[] { "VoucherAmount", "Amount", "TotalAmount" },
            "sales" => new[] { "SalesAmount", "NetAmount", "Revenue", "Amount", "TotalAmount" },
            _ => new[] { "Amount", "TotalAmount", "Count" }
        };

        return table.Columns.FirstOrDefault(c => names.Contains(c.ColumnName, StringComparer.OrdinalIgnoreCase))
            ?? table.Columns.FirstOrDefault(c => IsNumeric(c.DataType) && names.Any(n => c.ColumnName.Contains(n, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Returns the SQL expression that normalizes raw dates into the requested reporting period.
    /// </summary>
    private static string BuildPeriodExpression(string dateColumn, string grain)
    {
        return grain switch
        {
            "weekly" => $"DATEADD(week, DATEDIFF(week, 0, [{dateColumn}]), 0)",
            "monthly" => $"DATEFROMPARTS(YEAR([{dateColumn}]), MONTH([{dateColumn}]), 1)",
            "quarterly" => $"DATEFROMPARTS(YEAR([{dateColumn}]), ((DATEPART(quarter, [{dateColumn}]) - 1) * 3) + 1, 1)",
            _ => $"CAST([{dateColumn}] AS date)"
        };
    }

    /// <summary>
    /// Identifies SQL numeric types that can safely participate in aggregate metrics.
    /// </summary>
    private static bool IsNumeric(string dataType)
    {
        return dataType is "bigint" or "int" or "smallint" or "tinyint" or "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real";
    }
}
