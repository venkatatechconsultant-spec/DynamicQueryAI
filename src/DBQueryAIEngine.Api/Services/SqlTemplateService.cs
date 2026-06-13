using DBQueryAIEngine.Api.Models;

namespace DBQueryAIEngine.Api.Services;

public sealed class SqlTemplateService(IWarehouseSchemaService schemaService) : ISqlTemplateService
{
    private static readonly string[] Grains = ["daily", "weekly", "monthly", "quarterly"];

    /// <summary>
    /// Builds approved SQL template patterns from scanned table metadata instead of allowing unrestricted LLM SQL.
    /// </summary>
    public async Task<IReadOnlyList<SqlTemplate>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        var schema = await schemaService.GetAllowedSchemaAsync(cancellationToken);
        var templates = new List<SqlTemplate>();

        foreach (var table in schema)
        {
            var dateColumn = FindColumn(table, ["SalesDate", "TransactionDate", "TrxDate", "InspectionDate", "CreatedDate", "DateKey"]);
            var amountColumn = FindColumn(table, ["SalesAmount", "NetAmount", "Amount", "Revenue", "TotalAmount", "UsageAmount"]);
            var countColumn = FindColumn(table, ["Id", $"{table.TableName}Id", "TransactionId", "VoucherId"]);

            if (dateColumn is null)
            {
                continue;
            }

            foreach (var grain in Grains)
            {
                var metricExpression = amountColumn is not null
                    ? $"SUM(TRY_CONVERT(decimal(18,2), [{amountColumn.ColumnName}]))"
                    : $"COUNT_BIG([{countColumn?.ColumnName ?? dateColumn.ColumnName}])";

                templates.Add(new SqlTemplate(
                    $"{table.TableName}-{grain}",
                    $"{table.TableName} {grain} trend",
                    $"Aggregates {table.TableName} by {grain}.",
                    table.TableName,
                    grain,
                    BuildTemplate(table.SchemaName, table.TableName, dateColumn.ColumnName, metricExpression, grain)));
            }
        }

        return templates;
    }

    /// <summary>
    /// Creates a period aggregation query template for the selected table and grain.
    /// </summary>
    private static string BuildTemplate(string schemaName, string tableName, string dateColumn, string metricExpression, string grain)
    {
        var period = grain switch
        {
            "weekly" => $"DATEADD(week, DATEDIFF(week, 0, [{dateColumn}]), 0)",
            "monthly" => $"DATEFROMPARTS(YEAR([{dateColumn}]), MONTH([{dateColumn}]), 1)",
            "quarterly" => $"DATEFROMPARTS(YEAR([{dateColumn}]), ((DATEPART(quarter, [{dateColumn}]) - 1) * 3) + 1, 1)",
            _ => $"CAST([{dateColumn}] AS date)"
        };

        return $"""
            SELECT TOP (@MaxRows)
                   {period} AS PeriodStart,
                   {metricExpression} AS MetricValue
            FROM [{schemaName}].[{tableName}]
            WHERE [{dateColumn}] >= @StartDate
              AND [{dateColumn}] < @EndDate
            GROUP BY {period}
            ORDER BY PeriodStart;
            """;
    }

    /// <summary>
    /// Finds a likely business column by exact preferred names first, then by loose suffix matching.
    /// </summary>
    private static WarehouseColumnSchema? FindColumn(WarehouseTableSchema table, IReadOnlyList<string> preferredNames)
    {
        return table.Columns.FirstOrDefault(c => preferredNames.Contains(c.ColumnName, StringComparer.OrdinalIgnoreCase))
            ?? table.Columns.FirstOrDefault(c => preferredNames.Any(p => c.ColumnName.Contains(p, StringComparison.OrdinalIgnoreCase)));
    }
}
