namespace DBQueryAIEngine.Api.Models;

public sealed record ChatRequest(string Message, string? ConversationId = null);

public sealed record ChatResponse(
    string ConversationId,
    string Intent,
    string Sql,
    string Explanation,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    ChartDefinition? Chart,
    IReadOnlyList<string> Warnings);

public sealed record ChartDefinition(
    string Type,
    string XAxis,
    string YAxis,
    string Title);

public sealed record WarehouseTableSchema(
    string SchemaName,
    string TableName,
    IReadOnlyList<WarehouseColumnSchema> Columns);

public sealed record WarehouseColumnSchema(
    string ColumnName,
    string DataType,
    bool IsNullable,
    int? MaxLength,
    int? Precision,
    int? Scale);

public sealed record AnalyticsIntent(
    string Metric,
    string Grain,
    string TimeColumn,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Dimension,
    string TableName,
    string UserQuestion);

public sealed record SqlTemplate(
    string Id,
    string Name,
    string Description,
    string TableName,
    string Grain,
    string Sql);

public sealed record GeneratedSql(
    string Sql,
    IReadOnlyDictionary<string, object?> Parameters,
    ChartDefinition Chart,
    IReadOnlyList<string> Warnings);

public sealed record QueryResult(IReadOnlyList<Dictionary<string, object?>> Rows);
