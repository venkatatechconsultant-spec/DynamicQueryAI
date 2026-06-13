/*
Production aggregate strategy for high-volume Fabric Data Warehouse analytics.

Run and adapt these scripts in your Fabric DW after confirming real column names.
The API can start from detail tables, but production chat analytics should point
to daily aggregate tables refreshed by Fabric Pipelines, Data Factory, notebooks,
or scheduled SQL jobs.
*/

-- Schema scan used by the API.
SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE,
       CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME IN ('TrxDetails', 'FactSales', 'FactInspections', 'Fact Inspections', 'FactMediaDetails', 'VoucherTransactionDetails')
ORDER BY TABLE_NAME, ORDINAL_POSITION;

-- Daily sales aggregate. Replace SalesDate, SalesAmount, CustomerId, ProductId, StoreId, and Region with your actual column names.
CREATE TABLE dbo.AggSalesDaily
AS
SELECT
    CAST(SalesDate AS date) AS PeriodStart,
    ProductId,
    StoreId,
    Region,
    COUNT_BIG(*) AS TransactionCount,
    COUNT(DISTINCT CustomerId) AS CustomerCount,
    SUM(TRY_CONVERT(decimal(18,2), SalesAmount)) AS SalesAmount
FROM dbo.FactSales
GROUP BY CAST(SalesDate AS date), ProductId, StoreId, Region;

-- Daily usage aggregate. Replace TransactionDate and usage metrics with your real columns.
CREATE TABLE dbo.AggUsageDaily
AS
SELECT
    CAST(TransactionDate AS date) AS PeriodStart,
    ProductId,
    StoreId,
    COUNT_BIG(*) AS UsageCount,
    SUM(TRY_CONVERT(decimal(18,2), UsageAmount)) AS UsageAmount
FROM dbo.TrxDetails
GROUP BY CAST(TransactionDate AS date), ProductId, StoreId;

-- Daily inspection aggregate. Use either dbo.FactInspections or dbo.[Fact Inspections] depending on your real table name.
CREATE TABLE dbo.AggInspectionsDaily
AS
SELECT
    CAST(InspectionDate AS date) AS PeriodStart,
    StoreId,
    InspectionStatus,
    COUNT_BIG(*) AS InspectionCount
FROM dbo.FactInspections
GROUP BY CAST(InspectionDate AS date), StoreId, InspectionStatus;

-- Daily media aggregate. Replace CreatedDate, MediaType, and related columns with your real schema.
CREATE TABLE dbo.AggMediaDaily
AS
SELECT
    CAST(CreatedDate AS date) AS PeriodStart,
    MediaType,
    StoreId,
    COUNT_BIG(*) AS MediaCount
FROM dbo.FactMediaDetails
GROUP BY CAST(CreatedDate AS date), MediaType, StoreId;

-- Daily voucher aggregate. Replace TransactionDate, VoucherAmount, and status columns with your real schema.
CREATE TABLE dbo.AggVoucherDaily
AS
SELECT
    CAST(TransactionDate AS date) AS PeriodStart,
    VoucherStatus,
    StoreId,
    COUNT_BIG(*) AS VoucherCount,
    SUM(TRY_CONVERT(decimal(18,2), VoucherAmount)) AS VoucherAmount
FROM dbo.VoucherTransactionDetails
GROUP BY CAST(TransactionDate AS date), VoucherStatus, StoreId;

-- Weekly/monthly/quarterly templates should aggregate from the daily aggregate tables, not from detail fact tables.
DECLARE @StartDate date = DATEADD(month, -12, CAST(GETUTCDATE() AS date));
DECLARE @EndDate date = DATEADD(day, 1, CAST(GETUTCDATE() AS date));

SELECT
    DATEFROMPARTS(YEAR(PeriodStart), MONTH(PeriodStart), 1) AS PeriodStart,
    SUM(SalesAmount) AS MetricValue
FROM dbo.AggSalesDaily
WHERE PeriodStart >= @StartDate
  AND PeriodStart < @EndDate
GROUP BY DATEFROMPARTS(YEAR(PeriodStart), MONTH(PeriodStart), 1)
ORDER BY PeriodStart;
