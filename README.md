# DBQueryAIEngine

## Overview

`DBQueryAIEngine` is a solution that combines a .NET Web API with a React-based frontend to turn business analytics questions into guarded SQL queries, execute them against a Fabric Data Warehouse, and return chart-ready results with AI-generated insights.

The solution is designed for secure analytics over a known warehouse schema, with:
- Natural language intent parsing
- Template-based, parameterized SQL generation
- Fabric DW metadata discovery and schema whitelisting
- Optional OpenAI / Azure OpenAI support for intent parsing and insight summarization
- A lightweight React + Vite client for chat-driven analytics exploration

## Repository Structure

- `DBQueryAIEngine.sln`
- `src/DBQueryAIEngine.Api/`
  - ASP.NET Core Web API project
  - Controllers: `ChatController`, `SchemaController`, `TemplatesController`
  - Services: orchestration, schema caching, intent parsing, SQL generation, query execution, insight summarization
  - Options: `OpenAIOptions`, `WarehouseOptions`
  - Models: request/response shapes, schema metadata, generated SQL, chart definitions
  - Configuration: `appsettings.json`, `appsettings.Development.example.json`
- `src/dbqueryaiengine.client/`
  - React + Vite SPA
  - Chat UI, chart rendering with Recharts, and API integration
- `docs/`
  - Supporting documentation and SQL examples
- `scripts/`
  - Setup helper scripts for the development environment

## Key Features

- `POST /api/chat` accepts a natural language question and returns:
  - generated SQL
  - query results
  - a chart definition
  - an AI-generated explanation
- `GET /api/schema` returns allowed warehouse table metadata
- `GET /api/sql-templates` returns reusable analytics SQL templates
- Safe SQL generation using whitelisted tables, parameterization, and constrained aggregation patterns
- Supports Azure OpenAI and OpenAI via `OpenAIOptions`
- Built-in fallback parser and summary builder when LLM configuration is missing or unavailable

## Architecture

### API Pipeline

`ChatOrchestrator` coordinates the analytics flow:
1. `WarehouseSchemaService` loads allowed schema metadata from `INFORMATION_SCHEMA.COLUMNS`
2. `IntentParserService` converts user questions into a constrained `AnalyticsIntent`
3. `SqlGenerationService` produces parameterized SQL and chart metadata
4. `WarehouseQueryService` executes SQL against Fabric DW and returns rows
5. `InsightService` summarizes results using LLM text or a deterministic fallback

### Safety and Guardrails

- Only configured tables are considered via `WarehouseOptions.AllowedTables`
- SQL is built with parameterized values and explicit `TOP (@MaxRows)` limits
- Date grains are normalized to daily, weekly, monthly, or quarterly
- Metric selection is mapped to known numeric columns inside supported tables

### Frontend

The client app in `src/dbqueryaiengine.client/` provides:
- a chat-style interface
- buttons for sample prompts
- generated chart visualization via `recharts`
- SQL preview and tabular result rendering

## Configuration

Copy `src/DBQueryAIEngine.Api/appsettings.Development.example.json` to `src/DBQueryAIEngine.Api/appsettings.json` and update values.

Important settings:

- `ConnectionStrings:FabricWarehouse`
  - Fabric DW warehouse connection string
- `AllowedOrigins`
  - CORS origins allowed to call the API (for the React app)
- `OpenAI`
  - `Provider`: `AzureOpenAI` or `OpenAI`
  - `Endpoint`, `ApiKey`, `Model`, `ApiVersion`
  - `Temperature`, `MaxTokens`
- `Warehouse`
  - `DefaultSchema`
  - `AllowedTables`
  - `MaxRows`
  - `CommandTimeoutSeconds`
  - `SchemaCacheMinutes`

## Running Locally

### API

1. Open a terminal in `src/DBQueryAIEngine.Api`
2. Restore and run:
   ```powershell
   dotnet restore
   dotnet run
   ```
3. The API typically listens on `https://localhost:7194`

### Client

1. Open a terminal in `src/dbqueryaiengine.client`
2. Install dependencies and run:
   ```powershell
   npm install
   npm run dev
   ```
3. Visit the Vite development URL shown in the terminal, usually `http://localhost:5173`

> If the React app cannot reach the API, confirm `VITE_API_BASE_URL` is set or that `AllowedOrigins` includes the client origin.

## API Endpoints

- `POST /api/chat`
  - Request: `{ "message": "...", "conversationId": "optional" }`
  - Response: `ChatResponse` with `sql`, `rows`, `chart`, `explanation`, and `warnings`
- `GET /api/schema`
  - Returns allowed warehouse table metadata
- `GET /api/sql-templates`
  - Returns reusable SQL templates for configured tables and grains

## Development Notes

- The API uses dependency injection and minimal hosting in `Program.cs`
- `OpenAIChatClient` supports both Azure OpenAI and OpenAI endpoints
- `IntentParserService` prefers JSON responses from the LLM, with heuristics fallback for offline or missing LLM configuration
- `WarehouseSchemaService` caches schema metadata using `IMemoryCache`
- `InsightService` limits row input to the first 80 rows when asking the LLM for a summary

## Useful Files

- `src/DBQueryAIEngine.Api/Program.cs` — API startup and service registration
- `src/DBQueryAIEngine.Api/Controllers/ChatController.cs` — main analytics chat endpoint
- `src/DBQueryAIEngine.Api/Services/SqlGenerationService.cs` — SQL construction logic
- `src/DBQueryAIEngine.Api/Services/IntentParserService.cs` — natural language intent extraction
- `src/DBQueryAIEngine.Api/Services/WarehouseSchemaService.cs` — schema discovery and caching
- `src/dbqueryaiengine.client/src/main.tsx` — React chat UI and chart rendering

## Notes

This solution is built as a proof-of-concept for natural language analytics over a Fabric Data Warehouse, with an emphasis on schema safety and query transparency. Replace placeholder configuration values with real warehouse and OpenAI credentials before production use.
 
