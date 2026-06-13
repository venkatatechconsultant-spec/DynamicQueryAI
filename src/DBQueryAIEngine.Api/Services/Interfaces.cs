using DBQueryAIEngine.Api.Models;

namespace DBQueryAIEngine.Api.Services;

public interface IChatOrchestrator
{
    Task<ChatResponse> AnswerAsync(ChatRequest request, CancellationToken cancellationToken);
}

public interface IWarehouseSchemaService
{
    Task<IReadOnlyList<WarehouseTableSchema>> GetAllowedSchemaAsync(CancellationToken cancellationToken);
}

public interface ISqlTemplateService
{
    Task<IReadOnlyList<SqlTemplate>> GetTemplatesAsync(CancellationToken cancellationToken);
}

public interface IIntentParserService
{
    Task<AnalyticsIntent> ParseAsync(string message, IReadOnlyList<WarehouseTableSchema> schema, CancellationToken cancellationToken);
}

public interface ISqlGenerationService
{
    Task<GeneratedSql> GenerateAsync(AnalyticsIntent intent, IReadOnlyList<WarehouseTableSchema> schema, CancellationToken cancellationToken);
}

public interface IWarehouseQueryService
{
    Task<QueryResult> ExecuteAsync(GeneratedSql generatedSql, CancellationToken cancellationToken);
}

public interface IInsightService
{
    Task<string> SummarizeAsync(AnalyticsIntent intent, QueryResult result, CancellationToken cancellationToken);
}

public interface ILLMClient
{
    Task<string?> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
    Task<string?> CompleteTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
