using DBQueryAIEngine.Api.Models;
using DBQueryAIEngine.Api.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DBQueryAIEngine.Api.Services;

public sealed class WarehouseQueryService(
    IConfiguration configuration,
    IOptions<WarehouseOptions> options) : IWarehouseQueryService
{
    /// <summary>
    /// Executes generated parameterized SQL against Fabric DW and returns a compact row set for charting and summarization.
    /// </summary>
    public async Task<QueryResult> ExecuteAsync(GeneratedSql generatedSql, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("FabricWarehouse")
            ?? throw new InvalidOperationException("ConnectionStrings:FabricWarehouse is not configured.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(generatedSql.Sql, connection)
        {
            CommandTimeout = options.Value.CommandTimeoutSeconds
        };

        foreach (var parameter in generatedSql.Parameters)
        {
            command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
        }

        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return new QueryResult(rows);
    }
}
