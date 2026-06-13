# Production Architecture Notes

## Data Layer

- Keep raw Fabric DW tables partitioned by date where your Fabric capacity and table design allow it.
- Refresh `Agg*Daily` tables incrementally and derive weekly, monthly, and quarterly trends from those aggregates.
- Add dimensions such as product, store, region, customer segment, status, and media type only where business users actually filter or compare.
- Cache schema metadata in the API and cache common result sets by user, intent, grain, date range, and dimension.

## AI Safety

- Do not let the LLM execute arbitrary SQL.
- Let the LLM identify intent, metric, grain, table, date range, and dimensions.
- Generate SQL only from approved templates and scanned schema metadata.
- Log generated SQL, parameters, row count, duration, user id, and model id for audit.

## Scale

- Use short row limits for the chat response and return chart-ready aggregates, not detail exports.
- Add asynchronous export workflows for large downloads.
- Add API rate limiting, cancellation tokens, request timeouts, and per-user authorization filters.
- Put OpenAI calls behind retry, timeout, and circuit-breaker policies.

## Recommended Deployment

- React: Azure Static Web Apps or App Service.
- .NET API: Azure App Service, Container Apps, or AKS.
- Secrets: Azure Key Vault or managed identity-backed configuration.
- Warehouse auth: Microsoft Entra ID / managed identity where possible.
- Observability: Application Insights with dependency tracking for Fabric DW and OpenAI.
