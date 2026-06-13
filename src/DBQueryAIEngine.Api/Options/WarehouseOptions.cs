namespace DBQueryAIEngine.Api.Options;

public sealed class WarehouseOptions
{
    public const string SectionName = "Warehouse";
    public string DefaultSchema { get; set; } = "dbo";
    public string[] AllowedTables { get; set; } = [];
    public int MaxRows { get; set; } = 500;
    public int CommandTimeoutSeconds { get; set; } = 120;
    public int SchemaCacheMinutes { get; set; } = 60;
}
