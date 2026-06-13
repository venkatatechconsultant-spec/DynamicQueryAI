using DBQueryAIEngine.Api.Models;

namespace DBQueryAIEngine.Api.Services;

public sealed class ChatOrchestrator(
    IWarehouseSchemaService schemaService,
    IIntentParserService intentParser,
    ISqlGenerationService sqlGenerationService,
    IWarehouseQueryService queryService,
    IInsightService insightService) : IChatOrchestrator
{
    /// <summary>
    /// Coordinates the full chat analytics pipeline from natural language to SQL, query result, summary, and chart definition.
    /// </summary>
    public async Task<ChatResponse> AnswerAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var schema = await schemaService.GetAllowedSchemaAsync(cancellationToken);
        var intent = await intentParser.ParseAsync(request.Message, schema, cancellationToken);
        var generatedSql = await sqlGenerationService.GenerateAsync(intent, schema, cancellationToken);
        var result = await queryService.ExecuteAsync(generatedSql, cancellationToken);
        var explanation = await insightService.SummarizeAsync(intent, result, cancellationToken);

        return new ChatResponse(
            request.ConversationId ?? Guid.NewGuid().ToString("N"),
            $"{intent.Grain} {intent.Metric} by {intent.Dimension ?? "time"}",
            generatedSql.Sql,
            explanation,
            result.Rows,
            generatedSql.Chart,
            generatedSql.Warnings);
    }
}
