using DBQueryAIEngine.Api.Options;
using DBQueryAIEngine.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.SectionName));
builder.Services.Configure<WarehouseOptions>(builder.Configuration.GetSection(WarehouseOptions.SectionName));

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<ILLMClient, OpenAIChatClient>();

builder.Services.AddScoped<IWarehouseSchemaService, WarehouseSchemaService>();
builder.Services.AddScoped<ISqlTemplateService, SqlTemplateService>();
builder.Services.AddScoped<IIntentParserService, IntentParserService>();
builder.Services.AddScoped<ISqlGenerationService, SqlGenerationService>();
builder.Services.AddScoped<IWarehouseQueryService, WarehouseQueryService>();
builder.Services.AddScoped<IInsightService, InsightService>();
builder.Services.AddScoped<IChatOrchestrator, ChatOrchestrator>();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ClientCors");
app.MapControllers();

app.Run();
