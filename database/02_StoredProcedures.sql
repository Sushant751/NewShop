/*===========================================================================
 *  BillingSystem – SQL Server Stored Procedures
 *
 *  Run after:  01_Schema.sql
 *  Run before: 03_SeedData.sql
 *
 *  These procedures encapsulate complex multi-statement operations that
 *  are awkward to express as inline Dapper queries (e.g. sales with items,
 *  dashboard aggregation, invoice number generation).
 *==========================================================================*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

USE BillingSystem;
GO

/*===========================================================================
 *  sp_GenerateInvoiceNumber
 *  Generates a sequential invoice number per tenant per day:
 *  INV-{YYYY}{MM}{DD}-{NNNN}
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GenerateInvoiceNumber
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Prefix NVARCHAR(20) = 'INV-' + CONVERT(NVARCHAR(8), SYSUTCDATETIME(), 112) + '-';
    DECLARE @NextSeq INT;

    -- Atomically reserve the next sequence number for today
    -- Uses an application lock to serialise concurrent calls within the session
    BEGIN TRAN;
    DECLARE @LockResult INT;
    DECLARE @LockResource NVARCHAR(100) = 'InvoiceNumber_' + CONVERT(NVARCHAR(50), @TenantId);
    EXEC @LockResult = sp_getapplock
        @Resource = @LockResource,
        @LockMode  = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 5000;

    IF @LockResult < 0
    BEGIN
        ROLLBACK;
        -- Fallback: use a random suffix to avoid collision
        SELECT @Prefix + RIGHT('0000' + CONVERT(NVARCHAR(10), ABS(CHECKSUM(NEWID())) % 10000), 4) AS InvoiceNumber;
        RETURN;
    END

    SELECT @NextSeq = ISNULL(MAX(TRY_CAST(SUBSTRING(InvoiceNumber, 14, 4) AS INT)), 0) + 1
    FROM dbo.Sales
    WHERE TenantId = @TenantId
      AND InvoiceNumber LIKE @Prefix + '%'
      AND IsDeleted = 0;

    DECLARE @InvoiceNumber NVARCHAR(50) = @Prefix + RIGHT('0000' + CONVERT(NVARCHAR(10), @NextSeq), 4);

    COMMIT;

    SELECT @InvoiceNumber AS InvoiceNumber;
END
GO

/*===========================================================================
 *  sp_GeneratePurchaseNumber
 *  Generates a sequential purchase number per tenant per day:
 *  PO-{YYYY}{MM}{DD}-{NNNN}
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GeneratePurchaseNumber
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Prefix NVARCHAR(20) = 'PO-' + CONVERT(NVARCHAR(8), SYSUTCDATETIME(), 112) + '-';
    DECLARE @NextSeq INT;

    BEGIN TRAN;
    DECLARE @LockResult INT;
    DECLARE @LockResource NVARCHAR(100) = 'PurchaseNumber_' + CONVERT(NVARCHAR(50), @TenantId);
    EXEC @LockResult = sp_getapplock
        @Resource = @LockResource,
        @LockMode  = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 5000;

    IF @LockResult < 0
    BEGIN
        ROLLBACK;
        SELECT @Prefix + RIGHT('0000' + CONVERT(NVARCHAR(10), ABS(CHECKSUM(NEWID())) % 10000), 4) AS PurchaseNumber;
        RETURN;
    END

    SELECT @NextSeq = ISNULL(MAX(TRY_CAST(SUBSTRING(PurchaseNumber, 13, 4) AS INT)), 0) + 1
    FROM dbo.Purchases
    WHERE TenantId = @TenantId
      AND PurchaseNumber LIKE @Prefix + '%'
      AND IsDeleted = 0;

    DECLARE @PurchaseNumber NVARCHAR(50) = @Prefix + RIGHT('0000' + CONVERT(NVARCHAR(10), @NextSeq), 4);

    COMMIT;

    SELECT @PurchaseNumber AS PurchaseNumber;
END
GO

/*===========================================================================
 *  sp_GetDashboardSummary
 *  Returns aggregated KPIs for a tenant within a date range.
 *  Mirrors the DashboardSummary record used by IReportRepository.
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardSummary
    @TenantId UNIQUEIDENTIFIER,
    @FromDate  DATETIME2(7),
    @ToDate    DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL((SELECT SUM(GrandTotal) FROM dbo.Sales
                 WHERE TenantId = @TenantId AND IsDeleted = 0
                   AND Status = 3  -- Completed
                   AND SaleDate >= @FromDate AND SaleDate < @ToDate), 0)  AS TotalSales,

        ISNULL((SELECT SUM(GrandTotal) FROM dbo.Purchases
                 WHERE TenantId = @TenantId AND IsDeleted = 0
                   AND PurchaseDate >= @FromDate AND PurchaseDate < @ToDate), 0) AS TotalPurchases,

        ISNULL((SELECT SUM(Amount) FROM dbo.Expenses
                 WHERE TenantId = @TenantId AND IsDeleted = 0
                   AND ExpenseDate >= @FromDate AND ExpenseDate < @ToDate), 0) AS TotalExpenses,

        0 AS TotalProfit,  -- computed in application layer after COGS

        ISNULL((SELECT COUNT(*) FROM dbo.Sales
                 WHERE TenantId = @TenantId AND IsDeleted = 0
                   AND Status = 3
                   AND SaleDate >= @FromDate AND SaleDate < @ToDate), 0) AS SalesCount,

        ISNULL((SELECT COUNT(*) FROM dbo.Products
                 WHERE TenantId = @TenantId AND IsDeleted = 0), 0) AS ProductCount,

        ISNULL((SELECT COUNT(*) FROM dbo.Customers
                 WHERE TenantId = @TenantId AND IsDeleted = 0), 0) AS CustomerCount,

        ISNULL((SELECT COUNT(*) FROM dbo.Products
                 WHERE TenantId = @TenantId AND IsDeleted = 0 AND IsActive = 1
                   AND CurrentStock <= ReorderLevel), 0) AS LowStockCount;
END
GO

/*===========================================================================
 *  sp_GetTopProducts
 *  Returns the top N products by revenue for a tenant within a date range.
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GetTopProducts
    @TenantId UNIQUEIDENTIFIER,
    @FromDate  DATETIME2(7),
    @ToDate    DATETIME2(7),
    @TopCount  INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopCount)
        si.ProductId,
        p.Name                          AS ProductName,
        SUM(si.Quantity)                AS QuantitySold,
        SUM(si.LineTotal)               AS Revenue
    FROM dbo.SaleItems si
    INNER JOIN dbo.Sales s ON s.Id = si.SaleId AND s.TenantId = si.TenantId
    INNER JOIN dbo.Products p ON p.Id = si.ProductId
    WHERE si.TenantId = @TenantId
      AND si.IsDeleted = 0
      AND s.IsDeleted = 0
      AND s.Status = 3  -- Completed
      AND s.SaleDate >= @FromDate
      AND s.SaleDate < @ToDate
    GROUP BY si.ProductId, p.Name
    ORDER BY Revenue DESC;
END
GO

/*===========================================================================
 *  sp_GetSalesByDay
 *  Returns daily sales totals for charting (last N days).
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GetSalesByDay
    @TenantId UNIQUEIDENTIFIER,
    @FromDate  DATETIME2(7),
    @ToDate    DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CONVERT(DATE, s.SaleDate)  AS SaleDay,
        COUNT(*)                   AS SalesCount,
        ISNULL(SUM(s.GrandTotal), 0) AS TotalAmount
    FROM dbo.Sales s
    WHERE s.TenantId = @TenantId
      AND s.IsDeleted = 0
      AND s.Status = 3  -- Completed
      AND s.SaleDate >= @FromDate
      AND s.SaleDate < @ToDate
    GROUP BY CONVERT(DATE, s.SaleDate)
    ORDER BY SaleDay;
END
GO

/*===========================================================================
 *  sp_GetProfitAndLoss
 *  Returns revenue, COGS, expenses, and gross profit for a date range.
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GetProfitAndLoss
    @TenantId UNIQUEIDENTIFIER,
    @FromDate  DATETIME2(7),
    @ToDate    DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Revenue DECIMAL(18, 2) =
        ISNULL((SELECT SUM(GrandTotal) FROM dbo.Sales
                 WHERE TenantId = @TenantId AND IsDeleted = 0
                   AND Status = 3
                   AND SaleDate >= @FromDate AND SaleDate < @ToDate), 0);

    DECLARE @CostOfGoods DECIMAL(18, 2) =
        ISNULL((SELECT SUM(si.Quantity * si.CostPrice)
                  FROM dbo.SaleItems si
                  INNER JOIN dbo.Sales s ON s.Id = si.SaleId
                  WHERE si.TenantId = @TenantId AND si.IsDeleted = 0
                    AND s.Status = 3
                    AND s.SaleDate >= @FromDate AND s.SaleDate < @ToDate), 0);

    DECLARE @Expenses DECIMAL(18, 2) =
        ISNULL((SELECT SUM(Amount) FROM dbo.Expenses
                 WHERE TenantId = @TenantId AND IsDeleted = 0
                   AND ExpenseDate >= @FromDate AND ExpenseDate < @ToDate), 0);

    DECLARE @GrossProfit DECIMAL(18, 2) = @Revenue - @CostOfGoods - @Expenses;

    SELECT
        @Revenue     AS Revenue,
        @CostOfGoods AS CostOfGoods,
        @Expenses    AS Expenses,
        @GrossProfit AS GrossProfit;
END
GO

/*===========================================================================
 *  sp_GetLowStockProducts
 *  Returns products at or below their reorder level for a tenant.
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GetLowStockProducts
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Products
    WHERE TenantId = @TenantId
      AND IsDeleted = 0
      AND IsActive = 1
      AND TrackInventory = 1
      AND CurrentStock <= ReorderLevel
    ORDER BY (CurrentStock - ReorderLevel) ASC;
END
GO

/*===========================================================================
 *  sp_AdjustStock
 *  Performs a manual stock adjustment with full audit trail.
 *  @AdjustmentType: 1 = AdjustmentIn, 2 = AdjustmentOut
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.AdjustStock
    @TenantId     UNIQUEIDENTIFIER,
    @ProductId    UNIQUEIDENTIFIER,
    @ShopId       UNIQUEIDENTIFIER = NULL,
    @Quantity     DECIMAL(18, 3),   -- positive for in, negative for out
    @MovementType INT,              -- 5 = AdjustmentIn, 6 = AdjustmentOut
    @Notes        NVARCHAR(500) = NULL,
    @UserId       UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @BalanceAfter DECIMAL(18, 3);
    DECLARE @UnitCost DECIMAL(18, 2);

    SELECT @UnitCost = CostPrice FROM dbo.Products
     WHERE Id = @ProductId AND TenantId = @TenantId AND IsDeleted = 0;

    -- Update product stock
    UPDATE dbo.Products
       SET CurrentStock = CurrentStock + @Quantity
     WHERE Id = @ProductId AND TenantId = @TenantId AND IsDeleted = 0;

    SELECT @BalanceAfter = CurrentStock FROM dbo.Products
     WHERE Id = @ProductId AND TenantId = @TenantId;

    -- Record the movement
    INSERT INTO dbo.StockMovements
        (Id, TenantId, ProductId, ShopId, MovementType, Quantity, UnitCost,
         Reference, Notes, BalanceAfter, CreatedDate, CreatedBy, IsDeleted)
    VALUES
        (NEWID(), @TenantId, @ProductId, @ShopId, @MovementType, @Quantity, @UnitCost,
         'ADJUSTMENT', @Notes, @BalanceAfter, SYSUTCDATETIME(), @UserId, 0);

    COMMIT;

    SELECT @BalanceAfter AS NewBalance;
END
GO

/*===========================================================================
 *  sp_GetUserPermissions
 *  Returns the distinct set of permission names for a user within a tenant.
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GetUserPermissions
    @TenantId UNIQUEIDENTIFIER,
    @UserId   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT p.Name
    FROM dbo.UserRoles ur
    INNER JOIN dbo.RolePermissions rp
        ON rp.RoleId = ur.RoleId AND rp.TenantId = ur.TenantId
    INNER JOIN dbo.Permissions p
        ON p.Id = rp.PermissionId
    WHERE ur.TenantId = @TenantId
      AND ur.UserId = @UserId
      AND ur.IsDeleted = 0
      AND rp.IsDeleted = 0
      AND p.IsDeleted = 0;
END
GO

/*===========================================================================
 *  sp_GetUserRoles
 *  Returns the role names assigned to a user within a tenant.
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_GetUserRoles
    @TenantId UNIQUEIDENTIFIER,
    @UserId   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.Name
    FROM dbo.UserRoles ur
    INNER JOIN dbo.Roles r
        ON r.Id = ur.RoleId AND r.TenantId = ur.TenantId
    WHERE ur.TenantId = @TenantId
      AND ur.UserId = @UserId
      AND ur.IsDeleted = 0
      AND r.IsDeleted = 0;
END
GO

/*===========================================================================
 *  sp_RevokeAllUserTokens
 *  Revokes all active refresh tokens for a user (used on password change
 *  or explicit "logout all devices").
 *==========================================================================*/
CREATE OR ALTER PROCEDURE dbo.sp_RevokeAllUserTokens
    @TenantId UNIQUEIDENTIFIER,
    @UserId   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
       SET RevokedAt = SYSUTCDATETIME(),
           UpdatedDate = SYSUTCDATETIME()
     WHERE UserId = @UserId
       AND TenantId = @TenantId
       AND RevokedAt IS NULL
       AND IsDeleted = 0;
END
GO

PRINT 'Stored procedures created successfully.';
GO
