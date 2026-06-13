using DBQueryAIEngine.Api.Models;
using DBQueryAIEngine.Api.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DBQueryAIEngine.Api.Services;

public sealed class WarehouseSchemaService(
    IConfiguration configuration,
    IOptions<WarehouseOptions> options,
    IMemoryCache cache) : IWarehouseSchemaService
{
    private const string CacheKey = "fabric-dw-allowed-schema";

    /// <summary>
    /// Reads INFORMATION_SCHEMA metadata for the configured tables and caches it to avoid repeated warehouse metadata scans.
    /// </summary>
    public async Task<IReadOnlyList<WarehouseTableSchema>> GetAllowedSchemaAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<WarehouseTableSchema>? cached) && cached is not null)
        {
            return cached;
        }

        var warehouse = options.Value;
        var connectionString = configuration.GetConnectionString("FabricWarehouse")
            ?? throw new InvalidOperationException("ConnectionStrings:FabricWarehouse is not configured.");

        var tables = warehouse.AllowedTables.Length > 0
            ? warehouse.AllowedTables
            : ["TrxDetails", "FactSales", "FactInspections", "FactMediaDetails", "VoucherTransactionDetails"];

        const string sql = """
            SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE,
                   CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schema
              AND TABLE_NAME IN (SELECT value FROM STRING_SPLIT(@tables, ','))
            ORDER BY TABLE_NAME, ORDINAL_POSITION;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schema", warehouse.DefaultSchema);
        command.Parameters.AddWithValue("@tables", string.Join(',', tables));

        var rows = new List<(string Schema, string Table, WarehouseColumnSchema Column)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((
                reader.GetString(0),
                reader.GetString(1),
                new WarehouseColumnSchema(
                    reader.GetString(2),
                    reader.GetString(3),
                    string.Equals(reader.GetString(4), "YES", StringComparison.OrdinalIgnoreCase),
                    reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6)),
                    reader.IsDBNull(7) ? null : Convert.ToInt32(reader.GetValue(7)))));
        }

        var schema = rows
            .GroupBy(x => new { x.Schema, x.Table })
            .Select(g => new WarehouseTableSchema(g.Key.Schema, g.Key.Table, g.Select(x => x.Column).ToList()))
            .ToList();

        cache.Set(CacheKey, schema, TimeSpan.FromMinutes(Math.Max(5, warehouse.SchemaCacheMinutes)));
        return schema;
    }
}
