/*===========================================================================
 *  BillingSystem â€“ SQL Server Seed Data
 *  Multi-Tenant Billing, POS, Inventory & Shop Management SaaS
 *
 *  Run order:  01_Schema.sql  â†’  02_StoredProcedures.sql  â†’  03_SeedData.sql
 *
 *  This script is IDEMPOTENT â€“ it uses fixed GUIDs and MERGE / NOT EXISTS
 *  guards so it can be re-run safely without creating duplicates.
 *
 *  Default admin credentials
 *  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
 *    Email     : admin@billingsystem.com
 *    Password  : Admin@123
 *    (Password hash generated with ASP.NET Core Identity PBKDF2 hasher)
 *==========================================================================*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

USE BillingSystem;
GO

/*===========================================================================
 *  Fixed GUIDs (used throughout for deterministic, re-runnable seeding)
 *==========================================================================*/
-- Plans
DECLARE @PlanStarter      UNIQUEIDENTIFIER = 'A1111111-0000-0000-0000-000000000001';
DECLARE @PlanProfessional  UNIQUEIDENTIFIER = 'A1111111-0000-0000-0000-000000000002';
DECLARE @PlanEnterprise    UNIQUEIDENTIFIER = 'A1111111-0000-0000-0000-000000000003';

-- Tenant
DECLARE @TenantDemo        UNIQUEIDENTIFIER = 'B2222222-0000-0000-0000-000000000001';

-- Shop
DECLARE @ShopMain          UNIQUEIDENTIFIER = 'C3333333-0000-0000-0000-000000000001';

-- Roles
DECLARE @RoleGlobalAdmin   UNIQUEIDENTIFIER = 'D4444444-0000-0000-0000-000000000001';
DECLARE @RoleShopAdmin     UNIQUEIDENTIFIER = 'D4444444-0000-0000-0000-000000000002';
DECLARE @RoleManager       UNIQUEIDENTIFIER = 'D4444444-0000-0000-0000-000000000003';
DECLARE @RoleCashier       UNIQUEIDENTIFIER = 'D4444444-0000-0000-0000-000000000004';
DECLARE @RoleStaff         UNIQUEIDENTIFIER = 'D4444444-0000-0000-0000-000000000005';

-- User
DECLARE @UserAdmin         UNIQUEIDENTIFIER = 'E5555555-0000-0000-0000-000000000001';
DECLARE @UserShopAdmin     UNIQUEIDENTIFIER = 'E5555555-0000-0000-0000-000000000002';
DECLARE @UserClerk         UNIQUEIDENTIFIER = 'E5555555-0000-0000-0000-000000000003';

-- Permissions (19 total)
DECLARE @PermProductsView        UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000001';
DECLARE @PermProductsCreate      UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000002';
DECLARE @PermProductsEdit        UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000003';
DECLARE @PermProductsDelete      UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000004';
DECLARE @PermSalesCreate         UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000005';
DECLARE @PermSalesCancel         UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000006';
DECLARE @PermCustomersView       UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000007';
DECLARE @PermCustomersCreate     UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000008';
DECLARE @PermCustomersEdit       UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000009';
DECLARE @PermCustomersDelete     UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-00000000000A';
DECLARE @PermPurchasesView       UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-00000000000B';
DECLARE @PermPurchasesCreate     UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-00000000000C';
DECLARE @PermInventoryView       UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-00000000000D';
DECLARE @PermInventoryAdjust     UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-00000000000E';
DECLARE @PermReportsView        UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-00000000000F';
DECLARE @PermExpensesView       UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000010';
DECLARE @PermExpensesManage     UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000011';
DECLARE @PermSettingsManage     UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000012';
DECLARE @PermStaffManage         UNIQUEIDENTIFIER = 'F6666666-0000-0000-0000-000000000013';

-- Password hash for "Admin@123" (PBKDF2 via ASP.NET Core Identity PasswordHasher)
DECLARE @AdminPasswordHash NVARCHAR(MAX) = N'AQAAAAEAACcQAAAAEPipEIHquSW36OUKZexoLJIq0a8iTBlx6vEsBkadIiPMFU5Dr5nNaDs49uSzbXjfdQ==';
DECLARE @ShopAdminPasswordHash NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEM1NJaxvg62UMcJGS0LVk2rmZTHgvJK9jaaXnntte/Nv92cP2KPc9gvNOWqd8hoO2g==';
DECLARE @ClerkPasswordHash NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEJ4OXDpoNKYyQeCAXvA5dXnRYBqQ6HcSDkZRri5TultHMz6yyZpF0JNaPgDudUZOOw==';

-- SecurityStamp & ConcurrencyStamp
DECLARE @SecurityStamp NVARCHAR(100) = LOWER(REPLACE(NEWID(), '-', ''));
DECLARE @ConcurrencyStamp NVARCHAR(100) = LOWER(REPLACE(NEWID(), '-', ''));

/*===========================================================================
 *  1.  Subscription Plans
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Id = @PlanStarter)
    INSERT INTO dbo.Plans (Id, Name, Description, MonthlyPrice, AnnualPrice, MaxUsers, MaxProducts, MaxShops, IsActive, CreatedDate)
    VALUES (@PlanStarter, N'Starter', N'Perfect for small shops getting started with billing and POS.',
            19.00, 190.00, 3, 500, 1, 1, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Id = @PlanProfessional)
    INSERT INTO dbo.Plans (Id, Name, Description, MonthlyPrice, AnnualPrice, MaxUsers, MaxProducts, MaxShops, IsActive, CreatedDate)
    VALUES (@PlanProfessional, N'Professional', N'For growing businesses with multiple shops and staff.',
            49.00, 490.00, 10, 5000, 5, 1, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Id = @PlanEnterprise)
    INSERT INTO dbo.Plans (Id, Name, Description, MonthlyPrice, AnnualPrice, MaxUsers, MaxProducts, MaxShops, IsActive, CreatedDate)
    VALUES (@PlanEnterprise, N'Enterprise', N'Unlimited everything for large multi-location operations.',
            149.00, 1490.00, 50, 50000, 20, 1, SYSUTCDATETIME());

/*===========================================================================
 *  2.  Permissions (global catalogue â€“ 19 permissions)
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermProductsView)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermProductsView, N'Products.View', N'View products and inventory', N'Products', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermProductsCreate)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermProductsCreate, N'Products.Create', N'Create new products', N'Products', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermProductsEdit)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermProductsEdit, N'Products.Edit', N'Edit existing products', N'Products', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermProductsDelete)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermProductsDelete, N'Products.Delete', N'Delete products', N'Products', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermSalesCreate)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermSalesCreate, N'Sales.Create', N'Create new sales / POS transactions', N'Sales', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermSalesCancel)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermSalesCancel, N'Sales.Cancel', N'Cancel or return sales', N'Sales', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermCustomersView)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermCustomersView, N'Customers.View', N'View customer list', N'Customers', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermCustomersCreate)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermCustomersCreate, N'Customers.Create', N'Create new customers', N'Customers', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermCustomersEdit)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermCustomersEdit, N'Customers.Edit', N'Edit customer details', N'Customers', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermCustomersDelete)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermCustomersDelete, N'Customers.Delete', N'Delete customers', N'Customers', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermPurchasesView)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermPurchasesView, N'Purchases.View', N'View purchase orders', N'Purchases', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermPurchasesCreate)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermPurchasesCreate, N'Purchases.Create', N'Create purchase orders', N'Purchases', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermInventoryView)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermInventoryView, N'Inventory.View', N'View inventory levels', N'Inventory', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermInventoryAdjust)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermInventoryAdjust, N'Inventory.Adjust', N'Adjust stock levels manually', N'Inventory', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermReportsView)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermReportsView, N'Reports.View', N'View reports and analytics', N'Reports', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermExpensesView)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermExpensesView, N'Expenses.View', N'View expenses', N'Expenses', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermExpensesManage)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermExpensesManage, N'Expenses.Manage', N'Create and manage expenses', N'Expenses', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermSettingsManage)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermSettingsManage, N'Settings.Manage', N'Manage tenant and shop settings', N'Settings', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = @PermStaffManage)
    INSERT INTO dbo.Permissions (Id, Name, Description, [Group], CreatedDate)
    VALUES (@PermStaffManage, N'Staff.Manage', N'Manage staff users and roles', N'Staff', SYSUTCDATETIME());

/*===========================================================================
 *  3.  Demo Tenant
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantDemo)
    INSERT INTO dbo.Tenants
    (
        Id, Name, Slug, Description, ContactEmail, ContactPhone, Address,
        Country, CurrencyCode, TimeZone, TaxIdentificationNumber,
        Status, TrialEndsOn, SubscriptionEndsOn, PlanId, MaxUsers, MaxProducts,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @TenantDemo, N'Demo Shop', N'demo-shop',
        N'Demonstration tenant for testing and evaluation.',
        N'owner@demoshop.com', N'+1-555-0100', N'123 Market Street, Suite 100, San Francisco, CA 94103',
        N'United States', N'USD', N'UTC', N'TAX-DEMO-001',
        1,  -- Active (TenantStatus.Active = 1)
        NULL, DATEADD(day, 365, SYSUTCDATETIME()),
        @PlanProfessional, 10, 5000,
        SYSUTCDATETIME(), 0
    );

/*===========================================================================
 *  4.  Demo Shop (physical branch)
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Shops WHERE Id = @ShopMain)
    INSERT INTO dbo.Shops
    (
        Id, TenantId, Name, Code, Address, Phone, Email, IsActive, CreatedDate, IsDeleted
    )
    VALUES
    (
        @ShopMain, @TenantDemo, N'Main Branch', N'MAIN-01',
        N'123 Market Street, Suite 100, San Francisco, CA 94103',
        N'+1-555-0100', N'main@demoshop.com', 1, SYSUTCDATETIME(), 0
    );

/*===========================================================================
 *  5.  System Roles (5 roles for the demo tenant)
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = @RoleGlobalAdmin)
    INSERT INTO dbo.Roles (Id, TenantId, Name, NormalizedName, Description, IsSystemRole, CreatedDate, IsDeleted)
    VALUES (@RoleGlobalAdmin, @TenantDemo, N'GlobalAdmin', N'GLOBALADMIN',
            N'Full system administrator with unrestricted access.', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = @RoleShopAdmin)
    INSERT INTO dbo.Roles (Id, TenantId, Name, NormalizedName, Description, IsSystemRole, CreatedDate, IsDeleted)
    VALUES (@RoleShopAdmin, @TenantDemo, N'ShopAdmin', N'SHOPADMIN',
            N'Shop administrator â€“ manages all operations within their shop.', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = @RoleManager)
    INSERT INTO dbo.Roles (Id, TenantId, Name, NormalizedName, Description, IsSystemRole, CreatedDate, IsDeleted)
    VALUES (@RoleManager, @TenantDemo, N'Manager', N'MANAGER',
            N'Store manager â€“ manages products, sales, purchases, and reports.', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = @RoleCashier)
    INSERT INTO dbo.Roles (Id, TenantId, Name, NormalizedName, Description, IsSystemRole, CreatedDate, IsDeleted)
    VALUES (@RoleCashier, @TenantDemo, N'Cashier', N'CASHIER',
            N'Cashier â€“ processes sales and manages customers.', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = @RoleStaff)
    INSERT INTO dbo.Roles (Id, TenantId, Name, NormalizedName, Description, IsSystemRole, CreatedDate, IsDeleted)
    VALUES (@RoleStaff, @TenantDemo, N'Staff', N'STAFF',
            N'General staff â€“ basic view and sales access.', 1, SYSUTCDATETIME(), 0);

/*===========================================================================
 *  6.  Role-Permission Mappings
 *      GlobalAdmin & ShopAdmin â†’ ALL 19 permissions
 *      Manager                 â†’ Products(all), Sales(all), Customers(all),
 *                                 Purchases(all), Inventory(all), Reports.View,
 *                                 Expenses(all)
 *      Cashier                 â†’ Products.View, Sales.Create, Sales.Cancel,
 *                                 Customers.View, Customers.Create, Inventory.View
 *      Staff                   â†’ Products.View, Sales.Create, Customers.View,
 *                                 Inventory.View
 *==========================================================================*/

-- Helper: insert a role-permission row if it does not already exist
-- (uses the unique constraint UQ_RolePerms_RolePerm on TenantId, RoleId, PermissionId)

-- â”€â”€ GlobalAdmin: ALL permissions â”€â”€
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermProductsView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermProductsView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermProductsCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermProductsCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermProductsEdit)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermProductsEdit, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermProductsDelete)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermProductsDelete, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermSalesCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermSalesCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermSalesCancel)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermSalesCancel, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermCustomersView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermCustomersView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermCustomersCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermCustomersCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermCustomersEdit)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermCustomersEdit, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermCustomersDelete)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermCustomersDelete, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermPurchasesView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermPurchasesView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermPurchasesCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermPurchasesCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermInventoryView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermInventoryView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermInventoryAdjust)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermInventoryAdjust, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermReportsView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermReportsView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermExpensesView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermExpensesView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermExpensesManage)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermExpensesManage, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermSettingsManage)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermSettingsManage, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleGlobalAdmin AND PermissionId = @PermStaffManage)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleGlobalAdmin, @PermStaffManage, SYSUTCDATETIME(), 0);

-- â”€â”€ ShopAdmin: ALL permissions â”€â”€
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermProductsView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermProductsView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermProductsCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermProductsCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermProductsEdit)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermProductsEdit, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermProductsDelete)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermProductsDelete, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermSalesCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermSalesCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermSalesCancel)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermSalesCancel, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermCustomersView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermCustomersView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermCustomersCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermCustomersCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermCustomersEdit)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermCustomersEdit, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermCustomersDelete)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermCustomersDelete, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermPurchasesView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermPurchasesView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermPurchasesCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermPurchasesCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermInventoryView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermInventoryView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermInventoryAdjust)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermInventoryAdjust, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermReportsView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermReportsView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermExpensesView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermExpensesView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermExpensesManage)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermExpensesManage, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermSettingsManage)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermSettingsManage, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleShopAdmin AND PermissionId = @PermStaffManage)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleShopAdmin, @PermStaffManage, SYSUTCDATETIME(), 0);

-- â”€â”€ Manager: Products(all), Sales(all), Customers(all), Purchases(all), Inventory(all), Reports.View, Expenses(all) â”€â”€
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermProductsView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermProductsView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermProductsCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermProductsCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermProductsEdit)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermProductsEdit, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermProductsDelete)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermProductsDelete, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermSalesCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermSalesCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermSalesCancel)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermSalesCancel, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermCustomersView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermCustomersView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermCustomersCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermCustomersCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermCustomersEdit)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermCustomersEdit, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermCustomersDelete)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermCustomersDelete, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermPurchasesView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermPurchasesView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermPurchasesCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermPurchasesCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermInventoryView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermInventoryView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermInventoryAdjust)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermInventoryAdjust, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermReportsView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermReportsView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermExpensesView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermExpensesView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleManager AND PermissionId = @PermExpensesManage)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleManager, @PermExpensesManage, SYSUTCDATETIME(), 0);

-- â”€â”€ Cashier: Products.View, Sales.Create, Sales.Cancel, Customers.View, Customers.Create, Inventory.View â”€â”€
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleCashier AND PermissionId = @PermProductsView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleCashier, @PermProductsView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleCashier AND PermissionId = @PermSalesCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleCashier, @PermSalesCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleCashier AND PermissionId = @PermSalesCancel)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleCashier, @PermSalesCancel, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleCashier AND PermissionId = @PermCustomersView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleCashier, @PermCustomersView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleCashier AND PermissionId = @PermCustomersCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleCashier, @PermCustomersCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleCashier AND PermissionId = @PermInventoryView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleCashier, @PermInventoryView, SYSUTCDATETIME(), 0);

-- â”€â”€ Staff: Products.View, Sales.Create, Customers.View, Inventory.View â”€â”€
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleStaff AND PermissionId = @PermProductsView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleStaff, @PermProductsView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleStaff AND PermissionId = @PermSalesCreate)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleStaff, @PermSalesCreate, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleStaff AND PermissionId = @PermCustomersView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleStaff, @PermCustomersView, SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE TenantId = @TenantDemo AND RoleId = @RoleStaff AND PermissionId = @PermInventoryView)
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, PermissionId, CreatedDate, IsDeleted) VALUES (@TenantDemo, @RoleStaff, @PermInventoryView, SYSUTCDATETIME(), 0);

/*===========================================================================
 *  7.  Admin User
 *      Email: admin@billingsystem.com   Password: Admin@123
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserAdmin)
    INSERT INTO dbo.Users
    (
        Id, TenantId, UserName, Email, NormalizedEmail, FullName, PhoneNumber,
        PasswordHash, SecurityStamp, ConcurrencyStamp,
        EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, LockoutEnd, AccessFailedCount,
        IsActive, ShopId, LastLoginAt, LastLoginIp, DeviceInfo,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @UserAdmin, @TenantDemo,
        N'admin@billingsystem.com', N'admin@billingsystem.com', N'ADMIN@BILLINGSYSTEM.COM',
        N'System Administrator', N'+1-555-0101',
        @AdminPasswordHash,
        @SecurityStamp, @ConcurrencyStamp,
        1,  -- EmailConfirmed
        0,  -- PhoneNumberConfirmed
        0,  -- TwoFactorEnabled
        1,  -- LockoutEnabled
        NULL,  -- LockoutEnd
        0,  -- AccessFailedCount
        1,  -- IsActive
        @ShopMain,
        NULL, NULL, NULL,  -- LastLoginAt, LastLoginIp, DeviceInfo
        SYSUTCDATETIME(), 0
    );

/*===========================================================================
 *  8.  Assign GlobalAdmin role to the admin user
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE TenantId = @TenantDemo AND UserId = @UserAdmin AND RoleId = @RoleGlobalAdmin)
    INSERT INTO dbo.UserRoles (TenantId, UserId, RoleId, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @UserAdmin, @RoleGlobalAdmin, SYSUTCDATETIME(), 0);

/*===========================================================================
 *  8b. ShopAdmin User
 *      Email: shopadmin@demo.com   Password: ShopAdmin@123
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserShopAdmin)
    INSERT INTO dbo.Users
    (
        Id, TenantId, UserName, Email, NormalizedEmail, FullName, PhoneNumber,
        PasswordHash, SecurityStamp, ConcurrencyStamp,
        EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, LockoutEnd, AccessFailedCount,
        IsActive, ShopId, LastLoginAt, LastLoginIp, DeviceInfo,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @UserShopAdmin, @TenantDemo,
        N'shopadmin@demo.com', N'shopadmin@demo.com', N'SHOPADMIN@DEMO.COM',
        N'Shop Admin', N'+1-555-0202',
        @ShopAdminPasswordHash,
        @SecurityStamp, @ConcurrencyStamp,
        1,  -- EmailConfirmed
        0,  -- PhoneNumberConfirmed
        0,  -- TwoFactorEnabled
        1,  -- LockoutEnabled
        NULL,  -- LockoutEnd
        0,  -- AccessFailedCount
        1,  -- IsActive
        @ShopMain,
        NULL, NULL, NULL,  -- LastLoginAt, LastLoginIp, DeviceInfo
        SYSUTCDATETIME(), 0
    );

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE TenantId = @TenantDemo AND UserId = @UserShopAdmin AND RoleId = @RoleShopAdmin)
    INSERT INTO dbo.UserRoles (TenantId, UserId, RoleId, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @UserShopAdmin, @RoleShopAdmin, SYSUTCDATETIME(), 0);

/*===========================================================================
 *  8c. Clerk User
 *      Email: clerk@demo.com   Password: Clerk@123
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @UserClerk)
    INSERT INTO dbo.Users
    (
        Id, TenantId, UserName, Email, NormalizedEmail, FullName, PhoneNumber,
        PasswordHash, SecurityStamp, ConcurrencyStamp,
        EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, LockoutEnd, AccessFailedCount,
        IsActive, ShopId, LastLoginAt, LastLoginIp, DeviceInfo,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @UserClerk, @TenantDemo,
        N'clerk@demo.com', N'clerk@demo.com', N'CLERK@DEMO.COM',
        N'Clerk User', N'+1-555-0303',
        @ClerkPasswordHash,
        @SecurityStamp, @ConcurrencyStamp,
        1,  -- EmailConfirmed
        0,  -- PhoneNumberConfirmed
        0,  -- TwoFactorEnabled
        1,  -- LockoutEnabled
        NULL,  -- LockoutEnd
        0,  -- AccessFailedCount
        1,  -- IsActive
        @ShopMain,
        NULL, NULL, NULL,  -- LastLoginAt, LastLoginIp, DeviceInfo
        SYSUTCDATETIME(), 0
    );

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE TenantId = @TenantDemo AND UserId = @UserClerk AND RoleId = @RoleCashier)
    INSERT INTO dbo.UserRoles (TenantId, UserId, RoleId, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @UserClerk, @RoleCashier, SYSUTCDATETIME(), 0);

/*===========================================================================
 *  9.  Default tenant settings
 *==========================================================================*/
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE TenantId = @TenantDemo AND [Key] = N'CompanyName')
    INSERT INTO dbo.Settings (TenantId, [Key], Value, [Group], Description, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, N'CompanyName', N'Demo Shop', N'General', N'Display name for invoices and receipts', SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE TenantId = @TenantDemo AND [Key] = N'CurrencySymbol')
    INSERT INTO dbo.Settings (TenantId, [Key], Value, [Group], Description, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, N'CurrencySymbol', N'$', N'General', N'Symbol used for currency display', SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE TenantId = @TenantDemo AND [Key] = N'TaxRate')
    INSERT INTO dbo.Settings (TenantId, [Key], Value, [Group], Description, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, N'TaxRate', N'0', N'Tax', N'Default tax rate percentage', SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE TenantId = @TenantDemo AND [Key] = N'InvoicePrefix')
    INSERT INTO dbo.Settings (TenantId, [Key], Value, [Group], Description, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, N'InvoicePrefix', N'INV', N'Invoice', N'Prefix for auto-generated invoice numbers', SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE TenantId = @TenantDemo AND [Key] = N'PurchasePrefix')
    INSERT INTO dbo.Settings (TenantId, [Key], Value, [Group], Description, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, N'PurchasePrefix', N'PO', N'Purchase', N'Prefix for auto-generated purchase order numbers', SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE TenantId = @TenantDemo AND [Key] = N'LowStockAlertEnabled')
    INSERT INTO dbo.Settings (TenantId, [Key], Value, [Group], Description, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, N'LowStockAlertEnabled', N'true', N'Inventory', N'Enable low stock alert notifications', SYSUTCDATETIME(), 0);

/*===========================================================================
 *  10.  Sample catalog data (optional â€“ helps with first-run testing)
 *==========================================================================*/

-- â”€â”€ Units â”€â”€
DECLARE @UnitPcs UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @UnitKg  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @UnitLtr UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';

IF NOT EXISTS (SELECT 1 FROM dbo.Units WHERE Id = @UnitPcs)
    INSERT INTO dbo.Units (Id, TenantId, Name, Code, Description, IsActive, CreatedDate, IsDeleted)
    VALUES (@UnitPcs, @TenantDemo, N'Pieces', N'pcs', N'Individual pieces', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Units WHERE Id = @UnitKg)
    INSERT INTO dbo.Units (Id, TenantId, Name, Code, Description, IsActive, CreatedDate, IsDeleted)
    VALUES (@UnitKg, @TenantDemo, N'Kilogram', N'kg', N'Weight in kilograms', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Units WHERE Id = @UnitLtr)
    INSERT INTO dbo.Units (Id, TenantId, Name, Code, Description, IsActive, CreatedDate, IsDeleted)
    VALUES (@UnitLtr, @TenantDemo, N'Litre', N'ltr', N'Volume in litres', 1, SYSUTCDATETIME(), 0);

-- â”€â”€ Brands â”€â”€
DECLARE @BrandGeneric UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';
DECLARE @BrandPremium  UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Id = @BrandGeneric)
    INSERT INTO dbo.Brands (Id, TenantId, Name, Description, IsActive, CreatedDate, IsDeleted)
    VALUES (@BrandGeneric, @TenantDemo, N'Generic', N'Generic / unbranded items', 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Id = @BrandPremium)
    INSERT INTO dbo.Brands (Id, TenantId, Name, Description, IsActive, CreatedDate, IsDeleted)
    VALUES (@BrandPremium, @TenantDemo, N'Premium Choice', N'Premium quality products', 1, SYSUTCDATETIME(), 0);

-- â”€â”€ Categories â”€â”€
DECLARE @CatGrocery  UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000001';
DECLARE @CatBeverage UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000002';
DECLARE @CatSnacks   UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000003';

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Id = @CatGrocery)
    INSERT INTO dbo.Categories (Id, TenantId, Name, Description, ParentCategoryId, IsActive, CreatedDate, IsDeleted)
    VALUES (@CatGrocery, @TenantDemo, N'Grocery', N'General grocery items', NULL, 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Id = @CatBeverage)
    INSERT INTO dbo.Categories (Id, TenantId, Name, Description, ParentCategoryId, IsActive, CreatedDate, IsDeleted)
    VALUES (@CatBeverage, @TenantDemo, N'Beverages', N'Drinks and beverages', @CatGrocery, 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Id = @CatSnacks)
    INSERT INTO dbo.Categories (Id, TenantId, Name, Description, ParentCategoryId, IsActive, CreatedDate, IsDeleted)
    VALUES (@CatSnacks, @TenantDemo, N'Snacks', N'Snack foods', @CatGrocery, 1, SYSUTCDATETIME(), 0);

-- â”€â”€ Sample Products â”€â”€
DECLARE @Prod1 UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000001';
DECLARE @Prod2 UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000002';
DECLARE @Prod3 UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000003';
DECLARE @Prod4 UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000004';
DECLARE @Prod5 UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000005';

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @Prod1)
    INSERT INTO dbo.Products
    (
        Id, TenantId, Name, Description, Sku, Barcode,
        CategoryId, BrandId, UnitId,
        CostPrice, SellingPrice, TaxRate, IsTaxable,
        ReorderLevel, OpeningStock, CurrentStock,
        ImageUrl, IsActive, TrackInventory, AllowSaleWithoutStock,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @Prod1, @TenantDemo, N'Mineral Water 1L', N'1 litre mineral water bottle',
        N'BEV-001', N'8901234500011',
        @CatBeverage, @BrandGeneric, @UnitLtr,
        8.00, 15.00, 0, 0,
        50, 200, 200,
        NULL, 1, 1, 0,
        SYSUTCDATETIME(), 0
    );

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @Prod2)
    INSERT INTO dbo.Products
    (
        Id, TenantId, Name, Description, Sku, Barcode,
        CategoryId, BrandId, UnitId,
        CostPrice, SellingPrice, TaxRate, IsTaxable,
        ReorderLevel, OpeningStock, CurrentStock,
        ImageUrl, IsActive, TrackInventory, AllowSaleWithoutStock,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @Prod2, @TenantDemo, N'Orange Juice 500ml', N'500ml fresh orange juice',
        N'BEV-002', N'8901234500028',
        @CatBeverage, @BrandPremium, @UnitLtr,
        25.00, 45.00, 5, 1,
        30, 100, 100,
        NULL, 1, 1, 0,
        SYSUTCDATETIME(), 0
    );

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @Prod3)
    INSERT INTO dbo.Products
    (
        Id, TenantId, Name, Description, Sku, Barcode,
        CategoryId, BrandId, UnitId,
        CostPrice, SellingPrice, TaxRate, IsTaxable,
        ReorderLevel, OpeningStock, CurrentStock,
        ImageUrl, IsActive, TrackInventory, AllowSaleWithoutStock,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @Prod3, @TenantDemo, N'Potato Chips 150g', N'150g pack of potato chips',
        N'SNK-001', N'8901234500035',
        @CatSnacks, @BrandPremium, @UnitPcs,
        12.00, 25.00, 5, 1,
        40, 150, 150,
        NULL, 1, 1, 0,
        SYSUTCDATETIME(), 0
    );

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @Prod4)
    INSERT INTO dbo.Products
    (
        Id, TenantId, Name, Description, Sku, Barcode,
        CategoryId, BrandId, UnitId,
        CostPrice, SellingPrice, TaxRate, IsTaxable,
        ReorderLevel, OpeningStock, CurrentStock,
        ImageUrl, IsActive, TrackInventory, AllowSaleWithoutStock,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @Prod4, @TenantDemo, N'Chocolate Cookies 200g', N'200g pack of chocolate cookies',
        N'SNK-002', N'8901234500042',
        @CatSnacks, @BrandGeneric, @UnitPcs,
        18.00, 35.00, 5, 1,
        25, 80, 80,
        NULL, 1, 1, 0,
        SYSUTCDATETIME(), 0
    );

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @Prod5)
    INSERT INTO dbo.Products
    (
        Id, TenantId, Name, Description, Sku, Barcode,
        CategoryId, BrandId, UnitId,
        CostPrice, SellingPrice, TaxRate, IsTaxable,
        ReorderLevel, OpeningStock, CurrentStock,
        ImageUrl, IsActive, TrackInventory, AllowSaleWithoutStock,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @Prod5, @TenantDemo, N'Rice 5kg Bag', N'5kg bag of premium rice',
        N'GRO-001', N'8901234500059',
        @CatGrocery, @BrandPremium, @UnitKg,
        250.00, 320.00, 5, 1,
        20, 60, 60,
        NULL, 1, 1, 0,
        SYSUTCDATETIME(), 0
    );

-- â”€â”€ Inventory records for sample products â”€â”€
IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE TenantId = @TenantDemo AND ProductId = @Prod1 AND ShopId = @ShopMain)
    INSERT INTO dbo.Inventory (TenantId, ProductId, ShopId, QuantityOnHand, QuantityReserved, ReorderLevel, BatchNumber, ExpiryDate, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod1, @ShopMain, 200, 0, 50, N'BATCH-001', NULL, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE TenantId = @TenantDemo AND ProductId = @Prod2 AND ShopId = @ShopMain)
    INSERT INTO dbo.Inventory (TenantId, ProductId, ShopId, QuantityOnHand, QuantityReserved, ReorderLevel, BatchNumber, ExpiryDate, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod2, @ShopMain, 100, 0, 30, N'BATCH-002', DATEADD(month, 3, SYSUTCDATETIME()), SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE TenantId = @TenantDemo AND ProductId = @Prod3 AND ShopId = @ShopMain)
    INSERT INTO dbo.Inventory (TenantId, ProductId, ShopId, QuantityOnHand, QuantityReserved, ReorderLevel, BatchNumber, ExpiryDate, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod3, @ShopMain, 150, 0, 40, N'BATCH-003', DATEADD(month, 6, SYSUTCDATETIME()), SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE TenantId = @TenantDemo AND ProductId = @Prod4 AND ShopId = @ShopMain)
    INSERT INTO dbo.Inventory (TenantId, ProductId, ShopId, QuantityOnHand, QuantityReserved, ReorderLevel, BatchNumber, ExpiryDate, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod4, @ShopMain, 80, 0, 25, N'BATCH-004', DATEADD(month, 4, SYSUTCDATETIME()), SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.Inventory WHERE TenantId = @TenantDemo AND ProductId = @Prod5 AND ShopId = @ShopMain)
    INSERT INTO dbo.Inventory (TenantId, ProductId, ShopId, QuantityOnHand, QuantityReserved, ReorderLevel, BatchNumber, ExpiryDate, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod5, @ShopMain, 60, 0, 20, N'BATCH-005', NULL, SYSUTCDATETIME(), 0);

-- â”€â”€ Initial stock movements â”€â”€
IF NOT EXISTS (SELECT 1 FROM dbo.StockMovements WHERE TenantId = @TenantDemo AND ProductId = @Prod1 AND MovementType = 8)
    INSERT INTO dbo.StockMovements (TenantId, ProductId, ShopId, MovementType, Quantity, UnitCost, Reference, ReferenceId, Notes, BalanceAfter, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod1, @ShopMain, 8, 200, 8.00, N'Initial Stock', NULL, N'Opening stock', 200, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.StockMovements WHERE TenantId = @TenantDemo AND ProductId = @Prod2 AND MovementType = 8)
    INSERT INTO dbo.StockMovements (TenantId, ProductId, ShopId, MovementType, Quantity, UnitCost, Reference, ReferenceId, Notes, BalanceAfter, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod2, @ShopMain, 8, 100, 25.00, N'Initial Stock', NULL, N'Opening stock', 100, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.StockMovements WHERE TenantId = @TenantDemo AND ProductId = @Prod3 AND MovementType = 8)
    INSERT INTO dbo.StockMovements (TenantId, ProductId, ShopId, MovementType, Quantity, UnitCost, Reference, ReferenceId, Notes, BalanceAfter, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod3, @ShopMain, 8, 150, 12.00, N'Initial Stock', NULL, N'Opening stock', 150, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.StockMovements WHERE TenantId = @TenantDemo AND ProductId = @Prod4 AND MovementType = 8)
    INSERT INTO dbo.StockMovements (TenantId, ProductId, ShopId, MovementType, Quantity, UnitCost, Reference, ReferenceId, Notes, BalanceAfter, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod4, @ShopMain, 8, 80, 18.00, N'Initial Stock', NULL, N'Opening stock', 80, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.StockMovements WHERE TenantId = @TenantDemo AND ProductId = @Prod5 AND MovementType = 8)
    INSERT INTO dbo.StockMovements (TenantId, ProductId, ShopId, MovementType, Quantity, UnitCost, Reference, ReferenceId, Notes, BalanceAfter, CreatedDate, IsDeleted)
    VALUES (@TenantDemo, @Prod5, @ShopMain, 8, 60, 250.00, N'Initial Stock', NULL, N'Opening stock', 60, SYSUTCDATETIME(), 0);

/*===========================================================================
 *  11.  Sample customer
 *==========================================================================*/
DECLARE @CustomerWalkIn UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerWalkIn)
    INSERT INTO dbo.Customers
    (
        Id, TenantId, Name, Email, Phone, Address, City, State, Country, PostalCode,
        TaxNumber, OpeningBalance, CurrentBalance, CreditLimit, IsActive,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @CustomerWalkIn, @TenantDemo, N'Walk-in Customer', NULL, NULL,
        NULL, NULL, NULL, NULL, NULL,
        NULL, 0, 0, 0, 1,
        SYSUTCDATETIME(), 0
    );

/*===========================================================================
 *  12.  Sample supplier
 *==========================================================================*/
DECLARE @SupplierGlobal UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE Id = @SupplierGlobal)
    INSERT INTO dbo.Suppliers
    (
        Id, TenantId, Name, ContactPerson, Email, Phone, Address, City, State, Country, PostalCode,
        TaxNumber, OpeningBalance, CurrentBalance, IsActive,
        CreatedDate, IsDeleted
    )
    VALUES
    (
        @SupplierGlobal, @TenantDemo, N'Global Supplies Inc.', N'John Smith',
        N'orders@globalsupplies.com', N'+1-555-0200',
        N'500 Supply Chain Ave, New York, NY 10001',
        N'New York', N'NY', N'United States', N'10001',
        N'TAX-GS-001', 0, 0, 1,
        SYSUTCDATETIME(), 0
    );

/*===========================================================================
 *  Verification queries (run to confirm seed data)
 *==========================================================================*/
/*
SELECT 'Plans'           AS [Table], COUNT(*) AS [Count] FROM dbo.Plans           WHERE IsDeleted = 0
UNION ALL SELECT 'Tenants',          COUNT(*) FROM dbo.Tenants          WHERE IsDeleted = 0
UNION ALL SELECT 'Permissions',      COUNT(*) FROM dbo.Permissions      WHERE IsDeleted = 0
UNION ALL SELECT 'Shops',            COUNT(*) FROM dbo.Shops            WHERE IsDeleted = 0
UNION ALL SELECT 'Roles',           COUNT(*) FROM dbo.Roles            WHERE IsDeleted = 0
UNION ALL SELECT 'RolePermissions', COUNT(*) FROM dbo.RolePermissions WHERE IsDeleted = 0
UNION ALL SELECT 'Users',           COUNT(*) FROM dbo.Users            WHERE IsDeleted = 0
UNION ALL SELECT 'UserRoles',       COUNT(*) FROM dbo.UserRoles        WHERE IsDeleted = 0
UNION ALL SELECT 'Settings',        COUNT(*) FROM dbo.Settings         WHERE IsDeleted = 0
UNION ALL SELECT 'Units',           COUNT(*) FROM dbo.Units            WHERE IsDeleted = 0
UNION ALL SELECT 'Brands',          COUNT(*) FROM dbo.Brands           WHERE IsDeleted = 0
UNION ALL SELECT 'Categories',      COUNT(*) FROM dbo.Categories       WHERE IsDeleted = 0
UNION ALL SELECT 'Products',        COUNT(*) FROM dbo.Products         WHERE IsDeleted = 0
UNION ALL SELECT 'Inventory',       COUNT(*) FROM dbo.Inventory        WHERE IsDeleted = 0
UNION ALL SELECT 'StockMovements',  COUNT(*) FROM dbo.StockMovements   WHERE IsDeleted = 0
UNION ALL SELECT 'Customers',       COUNT(*) FROM dbo.Customers        WHERE IsDeleted = 0
UNION ALL SELECT 'Suppliers',       COUNT(*) FROM dbo.Suppliers        WHERE IsDeleted = 0;

-- Expected counts:
-- Plans=3, Tenants=1, Permissions=19, Shops=1, Roles=5,
-- RolePermissions=46, Users=1, UserRoles=1, Settings=6,
-- Units=3, Brands=2, Categories=3, Products=5, Inventory=5,
-- StockMovements=5, Customers=1, Suppliers=1
*/

/*===========================================================================
 *  End of seed data
 *==========================================================================*/
PRINT '============================================';
PRINT '  BillingSystem seed data loaded successfully';
PRINT '  Admin login: admin@billingsystem.com';
PRINT '  Admin password: Admin@123';
PRINT '  ShopAdmin login: shopadmin@demo.com';
PRINT '  ShopAdmin password: ShopAdmin@123';
PRINT '  Clerk login: clerk@demo.com';
PRINT '  Clerk password: Clerk@123';
PRINT '============================================';
