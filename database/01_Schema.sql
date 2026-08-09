/*===========================================================================
 *  BillingSystem – SQL Server DDL Schema
 *  Multi-Tenant Billing, POS, Inventory & Shop Management SaaS
 *
 *  Run order:  01_Schema.sql  →  02_StoredProcedures.sql  →  03_SeedData.sql
 *
 *  Conventions
 *  ──────────
 *  • Every tenant-scoped table has TenantId + audit columns + soft-delete.
 *  • Global tables (Tenants, Plans, Permissions) carry audit columns but no TenantId.
 *  • RowVersion (ROWVERSION) provides optimistic concurrency.
 *  • All GUID PKs default to NEWID(); application may also supply explicit GUIDs.
 *  • Monetary columns use DECIMAL(18,2); quantity columns use DECIMAL(18,3).
 *==========================================================================*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

/*===========================================================================
 *  Database creation (comment out if database already exists)
 *==========================================================================*/
IF DB_ID('BillingSystem') IS NULL
BEGIN
    CREATE DATABASE BillingSystem;
END
GO

USE BillingSystem;
GO

/*===========================================================================
 *  Drop existing objects (clean rebuild) – order respects FK dependencies
 *==========================================================================*/
IF OBJECT_ID('dbo.Payments',            'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.SaleItems',            'U') IS NOT NULL DROP TABLE dbo.SaleItems;
IF OBJECT_ID('dbo.Sales',                'U') IS NOT NULL DROP TABLE dbo.Sales;
IF OBJECT_ID('dbo.PurchaseItems',         'U') IS NOT NULL DROP TABLE dbo.Purchases;
IF OBJECT_ID('dbo.PurchaseItems',        'U') IS NOT NULL DROP TABLE dbo.PurchaseItems;
IF OBJECT_ID('dbo.Expenses',             'U') IS NOT NULL DROP TABLE dbo.Expenses;
IF OBJECT_ID('dbo.StockMovements',        'U') IS NOT NULL DROP TABLE dbo.StockMovements;
IF OBJECT_ID('dbo.Inventory',            'U') IS NOT NULL DROP TABLE dbo.Inventory;
IF OBJECT_ID('dbo.Products',             'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Units',                'U') IS NOT NULL DROP TABLE dbo.Units;
IF OBJECT_ID('dbo.Brands',               'U') IS NOT NULL DROP TABLE dbo.Brands;
IF OBJECT_ID('dbo.Categories',           'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Discounts',            'U') IS NOT NULL DROP TABLE dbo.Discounts;
IF OBJECT_ID('dbo.Taxes',                'U') IS NOT NULL DROP TABLE dbo.Taxes;
IF OBJECT_ID('dbo.Suppliers',            'U') IS NOT NULL DROP TABLE dbo.Suppliers;
IF OBJECT_ID('dbo.Customers',            'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID('dbo.Notifications',        'U') IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.Settings',             'U') IS NOT NULL DROP TABLE dbo.Settings;
IF OBJECT_ID('dbo.ActivityLogs',         'U') IS NOT NULL DROP TABLE dbo.ActivityLogs;
IF OBJECT_ID('dbo.AuditLogs',            'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID('dbo.RefreshTokens',        'U') IS NOT NULL DROP TABLE dbo.RefreshTokens;
IF OBJECT_ID('dbo.UserRoles',            'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.RolePermissions',      'U') IS NOT NULL DROP TABLE dbo.RolePermissions;
IF OBJECT_ID('dbo.Permissions',          'U') IS NOT NULL DROP TABLE dbo.Permissions;
IF OBJECT_ID('dbo.Roles',                'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Users',                'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Shops',                'U') IS NOT NULL DROP TABLE dbo.Shops;
IF OBJECT_ID('dbo.Plans',                'U') IS NOT NULL DROP TABLE dbo.Plans;
IF OBJECT_ID('dbo.Tenants',              'U') IS NOT NULL DROP TABLE dbo.Tenants;
GO

/*===========================================================================
 *  GLOBAL TABLES (no TenantId)
 *==========================================================================*/

-- ── Plans (subscription tiers) ──────────────────────────────────────────
CREATE TABLE dbo.Plans
(
    Id                  UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Plans_Id         DEFAULT NEWID(),
    Name                NVARCHAR(100)     NOT NULL,
    Description         NVARCHAR(500)     NULL,
    MonthlyPrice        DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Plans_Monthly   DEFAULT (0),
    AnnualPrice         DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Plans_Annual    DEFAULT (0),
    MaxUsers            INT               NOT NULL CONSTRAINT DF_Plans_MaxUsers  DEFAULT (0),
    MaxProducts         INT               NOT NULL CONSTRAINT DF_Plans_MaxProds  DEFAULT (0),
    MaxShops            INT               NOT NULL CONSTRAINT DF_Plans_MaxShops   DEFAULT (0),
    IsActive            BIT               NOT NULL CONSTRAINT DF_Plans_IsActive  DEFAULT (1),
    -- Audit columns
    CreatedBy           UNIQUEIDENTIFIER  NULL,
    CreatedDate         DATETIME2(7)      NOT NULL CONSTRAINT DF_Plans_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy           UNIQUEIDENTIFIER  NULL,
    UpdatedDate         DATETIME2(7)      NULL,
    DeletedBy           UNIQUEIDENTIFIER  NULL,
    DeletedDate         DATETIME2(7)      NULL,
    IsDeleted           BIT               NOT NULL CONSTRAINT DF_Plans_IsDeleted DEFAULT (0),
    RowVersion          ROWVERSION        NOT NULL,
    CONSTRAINT PK_Plans PRIMARY KEY CLUSTERED (Id)
);
GO

-- ── Tenants (the root organisational entity) ────────────────────────────
CREATE TABLE dbo.Tenants
(
    Id                       UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Tenants_Id        DEFAULT NEWID(),
    Name                     NVARCHAR(200)     NOT NULL,
    Slug                     NVARCHAR(100)     NOT NULL,
    Description              NVARCHAR(500)     NULL,
    ContactEmail             NVARCHAR(256)     NULL,
    ContactPhone             NVARCHAR(50)      NULL,
    Address                  NVARCHAR(500)     NULL,
    Country                  NVARCHAR(100)     NULL,
    CurrencyCode             NVARCHAR(10)      NULL CONSTRAINT DF_Tenants_Currency DEFAULT ('USD'),
    TimeZone                 NVARCHAR(50)      NULL CONSTRAINT DF_Tenants_TZ       DEFAULT ('UTC'),
    TaxIdentificationNumber  NVARCHAR(50)      NULL,
    Status                   INT               NOT NULL CONSTRAINT DF_Tenants_Status  DEFAULT (4), -- Trial
    TrialEndsOn              DATETIME2(7)      NULL,
    SubscriptionEndsOn       DATETIME2(7)      NULL,
    PlanId                   UNIQUEIDENTIFIER  NULL,
    MaxUsers                 INT               NOT NULL CONSTRAINT DF_Tenants_MaxUsers DEFAULT (5),
    MaxProducts              INT               NOT NULL CONSTRAINT DF_Tenants_MaxProds DEFAULT (1000),
    -- Audit columns
    CreatedBy                UNIQUEIDENTIFIER  NULL,
    CreatedDate              DATETIME2(7)      NOT NULL CONSTRAINT DF_Tenants_Created DEFAULT SYSUTCDATETIME(),
    UpdatedBy                UNIQUEIDENTIFIER  NULL,
    UpdatedDate              DATETIME2(7)      NULL,
    DeletedBy                UNIQUEIDENTIFIER  NULL,
    DeletedDate              DATETIME2(7)      NULL,
    IsDeleted                BIT               NOT NULL CONSTRAINT DF_Tenants_IsDeleted DEFAULT (0),
    RowVersion               ROWVERSION        NOT NULL,
    CONSTRAINT PK_Tenants PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Tenants_Plans FOREIGN KEY (PlanId)
        REFERENCES dbo.Plans (Id),
    CONSTRAINT UQ_Tenants_Slug UNIQUE NONCLUSTERED (Slug)
);
GO

-- ── Permissions (global catalogue of permission strings) ───────────────
CREATE TABLE dbo.Permissions
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Perms_Id        DEFAULT NEWID(),
    Name         NVARCHAR(100)     NOT NULL,
    Description  NVARCHAR(500)     NULL,
    [Group]      NVARCHAR(100)     NULL,
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Perms_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Perms_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Permissions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Permissions_Name UNIQUE NONCLUSTERED (Name)
);
GO

/*===========================================================================
 *  TENANT-SCOPED TABLES
 *==========================================================================*/

-- ── Shops (physical branches belonging to a tenant) ─────────────────────
CREATE TABLE dbo.Shops
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Shops_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    Name         NVARCHAR(200)     NOT NULL,
    Code         NVARCHAR(50)      NULL,
    Address      NVARCHAR(500)     NULL,
    Phone        NVARCHAR(50)      NULL,
    Email        NVARCHAR(256)     NULL,
    IsActive     BIT               NOT NULL CONSTRAINT DF_Shops_IsActive DEFAULT (1),
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Shops_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Shops_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Shops PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Shops_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── Users ────────────────────────────────────────────────────────────────
CREATE TABLE dbo.Users
(
    Id                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Users_Id            DEFAULT NEWID(),
    TenantId              UNIQUEIDENTIFIER  NOT NULL,
    UserName              NVARCHAR(256)     NOT NULL,
    Email                 NVARCHAR(256)     NOT NULL,
    NormalizedEmail        NVARCHAR(256)     NOT NULL,
    FullName              NVARCHAR(200)     NOT NULL,
    PhoneNumber           NVARCHAR(50)      NULL,
    PasswordHash          NVARCHAR(MAX)     NOT NULL,
    SecurityStamp         NVARCHAR(100)     NULL,
    ConcurrencyStamp      NVARCHAR(100)     NULL,
    EmailConfirmed        BIT               NOT NULL CONSTRAINT DF_Users_EmailConf     DEFAULT (0),
    PhoneNumberConfirmed  BIT               NOT NULL CONSTRAINT DF_Users_PhoneConf     DEFAULT (0),
    TwoFactorEnabled      BIT               NOT NULL CONSTRAINT DF_Users_2FA          DEFAULT (0),
    LockoutEnabled        BIT               NOT NULL CONSTRAINT DF_Users_LockoutEn     DEFAULT (1),
    LockoutEnd            DATETIMEOFFSET(7) NULL,
    AccessFailedCount     INT               NOT NULL CONSTRAINT DF_Users_AccessFailed DEFAULT (0),
    IsActive              BIT               NOT NULL CONSTRAINT DF_Users_IsActive     DEFAULT (1),
    ShopId                UNIQUEIDENTIFIER  NULL,
    LastLoginAt           DATETIME2(7)      NULL,
    LastLoginIp           NVARCHAR(100)     NULL,
    DeviceInfo            NVARCHAR(500)     NULL,
    -- Audit columns
    CreatedBy             UNIQUEIDENTIFIER  NULL,
    CreatedDate           DATETIME2(7)      NOT NULL CONSTRAINT DF_Users_Created      DEFAULT SYSUTCDATETIME(),
    UpdatedBy             UNIQUEIDENTIFIER  NULL,
    UpdatedDate           DATETIME2(7)      NULL,
    DeletedBy             UNIQUEIDENTIFIER  NULL,
    DeletedDate           DATETIME2(7)      NULL,
    IsDeleted             BIT               NOT NULL CONSTRAINT DF_Users_IsDeleted     DEFAULT (0),
    RowVersion             ROWVERSION        NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Users_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id),
    CONSTRAINT FK_Users_Shops FOREIGN KEY (ShopId)
        REFERENCES dbo.Shops (Id)
);
GO

-- ── Roles ────────────────────────────────────────────────────────────────
CREATE TABLE dbo.Roles
(
    Id              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Roles_Id          DEFAULT NEWID(),
    TenantId        UNIQUEIDENTIFIER  NOT NULL,
    Name            NVARCHAR(100)     NOT NULL,
    NormalizedName  NVARCHAR(100)     NOT NULL,
    Description     NVARCHAR(500)     NULL,
    IsSystemRole    BIT               NOT NULL CONSTRAINT DF_Roles_IsSystem    DEFAULT (0),
    -- Audit columns
    CreatedBy       UNIQUEIDENTIFIER  NULL,
    CreatedDate     DATETIME2(7)      NOT NULL CONSTRAINT DF_Roles_Created     DEFAULT SYSUTCDATETIME(),
    UpdatedBy       UNIQUEIDENTIFIER  NULL,
    UpdatedDate     DATETIME2(7)      NULL,
    DeletedBy       UNIQUEIDENTIFIER  NULL,
    DeletedDate     DATETIME2(7)      NULL,
    IsDeleted       BIT               NOT NULL CONSTRAINT DF_Roles_IsDeleted  DEFAULT (0),
    RowVersion      ROWVERSION        NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Roles_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── RolePermissions (many-to-many: Role ↔ Permission) ───────────────────
CREATE TABLE dbo.RolePermissions
(
    Id            UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_RolePerms_Id        DEFAULT NEWID(),
    TenantId      UNIQUEIDENTIFIER  NOT NULL,
    RoleId        UNIQUEIDENTIFIER  NOT NULL,
    PermissionId  UNIQUEIDENTIFIER  NOT NULL,
    -- Audit columns
    CreatedBy     UNIQUEIDENTIFIER  NULL,
    CreatedDate   DATETIME2(7)      NOT NULL CONSTRAINT DF_RolePerms_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy     UNIQUEIDENTIFIER  NULL,
    UpdatedDate   DATETIME2(7)      NULL,
    DeletedBy     UNIQUEIDENTIFIER  NULL,
    DeletedDate   DATETIME2(7)      NULL,
    IsDeleted     BIT               NOT NULL CONSTRAINT DF_RolePerms_IsDeleted DEFAULT (0),
    RowVersion    ROWVERSION        NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_RolePerms_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles (Id),
    CONSTRAINT FK_RolePerms_Permissions FOREIGN KEY (PermissionId)
        REFERENCES dbo.Permissions (Id),
    CONSTRAINT UQ_RolePerms_RolePerm UNIQUE NONCLUSTERED (TenantId, RoleId, PermissionId)
);
GO

-- ── UserRoles (many-to-many: User ↔ Role) ────────────────────────────────
CREATE TABLE dbo.UserRoles
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_UserRoles_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    UserId       UNIQUEIDENTIFIER  NOT NULL,
    RoleId       UNIQUEIDENTIFIER  NOT NULL,
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_UserRoles_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_UserRoles_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles (Id),
    CONSTRAINT UQ_UserRoles_UserRole UNIQUE NONCLUSTERED (TenantId, UserId, RoleId)
);
GO

-- ── RefreshTokens ────────────────────────────────────────────────────────
CREATE TABLE dbo.RefreshTokens
(
    Id              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Refresh_Id        DEFAULT NEWID(),
    TenantId        UNIQUEIDENTIFIER  NOT NULL,
    UserId          UNIQUEIDENTIFIER  NOT NULL,
    TokenHash       NVARCHAR(256)     NOT NULL,
    JwtId           NVARCHAR(100)     NULL,
    ExpiresAt       DATETIME2(7)      NOT NULL,
    RevokedAt       DATETIME2(7)      NULL,
    ReplacedByToken NVARCHAR(256)     NULL,
    CreatedByIp     NVARCHAR(100)     NULL,
    DeviceInfo      NVARCHAR(500)     NULL,
    -- Audit columns
    CreatedBy       UNIQUEIDENTIFIER  NULL,
    CreatedDate     DATETIME2(7)      NOT NULL CONSTRAINT DF_Refresh_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy       UNIQUEIDENTIFIER  NULL,
    UpdatedDate     DATETIME2(7)      NULL,
    DeletedBy       UNIQUEIDENTIFIER  NULL,
    DeletedDate     DATETIME2(7)      NULL,
    IsDeleted       BIT               NOT NULL CONSTRAINT DF_Refresh_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION        NOT NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Refresh_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id)
);
GO

-- ── Categories (product categories, self-referencing hierarchy) ─────────
CREATE TABLE dbo.Categories
(
    Id                UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Cats_Id        DEFAULT NEWID(),
    TenantId          UNIQUEIDENTIFIER  NOT NULL,
    Name              NVARCHAR(200)     NOT NULL,
    Description       NVARCHAR(500)     NULL,
    ParentCategoryId  UNIQUEIDENTIFIER  NULL,
    IsActive          BIT               NOT NULL CONSTRAINT DF_Cats_IsActive DEFAULT (1),
    -- Audit columns
    CreatedBy         UNIQUEIDENTIFIER  NULL,
    CreatedDate       DATETIME2(7)      NOT NULL CONSTRAINT DF_Cats_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy         UNIQUEIDENTIFIER  NULL,
    UpdatedDate       DATETIME2(7)      NULL,
    DeletedBy         UNIQUEIDENTIFIER  NULL,
    DeletedDate       DATETIME2(7)      NULL,
    IsDeleted         BIT               NOT NULL CONSTRAINT DF_Cats_IsDeleted DEFAULT (0),
    RowVersion        ROWVERSION        NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Categories_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id),
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentCategoryId)
        REFERENCES dbo.Categories (Id)
);
GO

-- ── Brands ───────────────────────────────────────────────────────────────
CREATE TABLE dbo.Brands
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Brands_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    Name         NVARCHAR(200)     NOT NULL,
    Description  NVARCHAR(500)     NULL,
    IsActive     BIT               NOT NULL CONSTRAINT DF_Brands_IsActive DEFAULT (1),
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Brands_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Brands_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Brands PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Brands_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── Units (measurement units: pcs, kg, litre, etc.) ─────────────────────
CREATE TABLE dbo.Units
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Units_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    Name         NVARCHAR(100)     NOT NULL,
    Code         NVARCHAR(20)      NOT NULL,
    Description  NVARCHAR(500)     NULL,
    IsActive     BIT               NOT NULL CONSTRAINT DF_Units_IsActive  DEFAULT (1),
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Units_Created    DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Units_IsDeleted  DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Units PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Units_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── Products ─────────────────────────────────────────────────────────────
CREATE TABLE dbo.Products
(
    Id                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Products_Id        DEFAULT NEWID(),
    TenantId              UNIQUEIDENTIFIER  NOT NULL,
    Name                  NVARCHAR(200)     NOT NULL,
    Description           NVARCHAR(MAX)     NULL,
    Sku                   NVARCHAR(100)     NULL,
    Barcode               NVARCHAR(100)     NULL,
    CategoryId            UNIQUEIDENTIFIER  NULL,
    BrandId               UNIQUEIDENTIFIER  NULL,
    UnitId                UNIQUEIDENTIFIER  NULL,
    CostPrice             DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Products_Cost     DEFAULT (0),
    SellingPrice          DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Products_Selling  DEFAULT (0),
    TaxRate               DECIMAL(5, 2)     NOT NULL CONSTRAINT DF_Products_TaxRate  DEFAULT (0),
    IsTaxable             BIT               NOT NULL CONSTRAINT DF_Products_IsTax   DEFAULT (1),
    ReorderLevel          DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_Products_Reorder   DEFAULT (0),
    OpeningStock          DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_Products_Opening  DEFAULT (0),
    CurrentStock          DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_Products_Current   DEFAULT (0),
    ImageUrl              NVARCHAR(500)     NULL,
    IsActive              BIT               NOT NULL CONSTRAINT DF_Products_IsActive  DEFAULT (1),
    TrackInventory        BIT               NOT NULL CONSTRAINT DF_Products_TrackInv DEFAULT (1),
    AllowSaleWithoutStock BIT               NOT NULL CONSTRAINT DF_Products_AllowSale DEFAULT (0),
    -- Audit columns
    CreatedBy             UNIQUEIDENTIFIER  NULL,
    CreatedDate           DATETIME2(7)      NOT NULL CONSTRAINT DF_Products_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy             UNIQUEIDENTIFIER  NULL,
    UpdatedDate           DATETIME2(7)      NULL,
    DeletedBy             UNIQUEIDENTIFIER  NULL,
    DeletedDate           DATETIME2(7)      NULL,
    IsDeleted             BIT               NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT (0),
    RowVersion            ROWVERSION        NOT NULL,
    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Products_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (Id),
    CONSTRAINT FK_Products_Brands FOREIGN KEY (BrandId)
        REFERENCES dbo.Brands (Id),
    CONSTRAINT FK_Products_Units FOREIGN KEY (UnitId)
        REFERENCES dbo.Units (Id)
);
GO

-- ── Inventory (per-product, per-shop stock levels) ───────────────────────
CREATE TABLE dbo.Inventory
(
    Id               UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Inv_Id        DEFAULT NEWID(),
    TenantId         UNIQUEIDENTIFIER  NOT NULL,
    ProductId        UNIQUEIDENTIFIER  NOT NULL,
    ShopId           UNIQUEIDENTIFIER  NULL,
    QuantityOnHand   DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_Inv_OnHand    DEFAULT (0),
    QuantityReserved DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_Inv_Reserved  DEFAULT (0),
    ReorderLevel     DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_Inv_Reorder   DEFAULT (0),
    BatchNumber      NVARCHAR(100)     NULL,
    ExpiryDate       DATETIME2(7)      NULL,
    -- Audit columns
    CreatedBy        UNIQUEIDENTIFIER  NULL,
    CreatedDate      DATETIME2(7)      NOT NULL CONSTRAINT DF_Inv_Created    DEFAULT SYSUTCDATETIME(),
    UpdatedBy        UNIQUEIDENTIFIER  NULL,
    UpdatedDate      DATETIME2(7)      NULL,
    DeletedBy        UNIQUEIDENTIFIER  NULL,
    DeletedDate      DATETIME2(7)      NULL,
    IsDeleted        BIT               NOT NULL CONSTRAINT DF_Inv_IsDeleted  DEFAULT (0),
    RowVersion       ROWVERSION        NOT NULL,
    CONSTRAINT PK_Inventory PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Inventory_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id),
    CONSTRAINT FK_Inventory_Shops FOREIGN KEY (ShopId)
        REFERENCES dbo.Shops (Id)
);
GO

-- ── StockMovements (immutable audit trail of every stock change) ─────────
CREATE TABLE dbo.StockMovements
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_StockMov_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    ProductId    UNIQUEIDENTIFIER  NOT NULL,
    ShopId       UNIQUEIDENTIFIER  NULL,
    MovementType INT               NOT NULL,  -- StockMovementType enum
    Quantity     DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_StockMov_Qty      DEFAULT (0),
    UnitCost     DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_StockMov_Cost     DEFAULT (0),
    Reference    NVARCHAR(100)     NULL,
    ReferenceId  UNIQUEIDENTIFIER  NULL,
    Notes        NVARCHAR(500)     NULL,
    BalanceAfter DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_StockMov_Balance  DEFAULT (0),
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_StockMov_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_StockMov_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_StockMovements PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_StockMov_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id),
    CONSTRAINT FK_StockMov_Shops FOREIGN KEY (ShopId)
        REFERENCES dbo.Shops (Id)
);
GO

-- ── Customers ────────────────────────────────────────────────────────────
CREATE TABLE dbo.Customers
(
    Id              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Customers_Id        DEFAULT NEWID(),
    TenantId        UNIQUEIDENTIFIER  NOT NULL,
    Name            NVARCHAR(200)     NOT NULL,
    Email           NVARCHAR(256)     NULL,
    Phone           NVARCHAR(50)      NULL,
    Address         NVARCHAR(500)     NULL,
    City            NVARCHAR(100)     NULL,
    State           NVARCHAR(100)     NULL,
    Country         NVARCHAR(100)     NULL,
    PostalCode      NVARCHAR(20)      NULL,
    TaxNumber       NVARCHAR(50)      NULL,
    OpeningBalance  DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Customers_Opening  DEFAULT (0),
    CurrentBalance  DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Customers_Current DEFAULT (0),
    CreditLimit     DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Customers_Credit  DEFAULT (0),
    IsActive        BIT               NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1),
    -- Audit columns
    CreatedBy       UNIQUEIDENTIFIER  NULL,
    CreatedDate     DATETIME2(7)      NOT NULL CONSTRAINT DF_Customers_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy       UNIQUEIDENTIFIER  NULL,
    UpdatedDate     DATETIME2(7)      NULL,
    DeletedBy       UNIQUEIDENTIFIER  NULL,
    DeletedDate     DATETIME2(7)      NULL,
    IsDeleted       BIT               NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION        NOT NULL,
    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Customers_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── Suppliers ─────────────────────────────────────────────────────────────
CREATE TABLE dbo.Suppliers
(
    Id              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Suppliers_Id        DEFAULT NEWID(),
    TenantId        UNIQUEIDENTIFIER  NOT NULL,
    Name            NVARCHAR(200)     NOT NULL,
    ContactPerson   NVARCHAR(200)     NULL,
    Email           NVARCHAR(256)     NULL,
    Phone           NVARCHAR(50)      NULL,
    Address         NVARCHAR(500)     NULL,
    City            NVARCHAR(100)     NULL,
    State           NVARCHAR(100)     NULL,
    Country         NVARCHAR(100)     NULL,
    PostalCode      NVARCHAR(20)      NULL,
    TaxNumber       NVARCHAR(50)      NULL,
    OpeningBalance  DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Suppliers_Opening  DEFAULT (0),
    CurrentBalance  DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Suppliers_Current DEFAULT (0),
    IsActive        BIT               NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1),
    -- Audit columns
    CreatedBy       UNIQUEIDENTIFIER  NULL,
    CreatedDate     DATETIME2(7)      NOT NULL CONSTRAINT DF_Suppliers_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy       UNIQUEIDENTIFIER  NULL,
    UpdatedDate     DATETIME2(7)      NULL,
    DeletedBy       UNIQUEIDENTIFIER  NULL,
    DeletedDate     DATETIME2(7)      NULL,
    IsDeleted       BIT               NOT NULL CONSTRAINT DF_Suppliers_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION        NOT NULL,
    CONSTRAINT PK_Suppliers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Suppliers_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── Sales (POS transactions / invoices) ──────────────────────────────────
CREATE TABLE dbo.Sales
(
    Id              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Sales_Id        DEFAULT NEWID(),
    TenantId        UNIQUEIDENTIFIER  NOT NULL,
    InvoiceNumber   NVARCHAR(50)      NOT NULL,
    ShopId           UNIQUEIDENTIFIER  NULL,
    CustomerId      UNIQUEIDENTIFIER  NULL,
    CashierId       UNIQUEIDENTIFIER  NOT NULL,
    SaleDate        DATETIME2(7)      NOT NULL CONSTRAINT DF_Sales_SaleDate  DEFAULT SYSUTCDATETIME(),
    Status          INT               NOT NULL CONSTRAINT DF_Sales_Status   DEFAULT (1),  -- Draft
    PaymentStatus   INT               NOT NULL CONSTRAINT DF_Sales_PayStatus DEFAULT (1),  -- Unpaid
    SubTotal        DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Sales_SubTotal  DEFAULT (0),
    DiscountAmount  DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Sales_Discount  DEFAULT (0),
    TaxAmount       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Sales_Tax       DEFAULT (0),
    RoundOff        DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Sales_RoundOff  DEFAULT (0),
    GrandTotal      DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Sales_Grand     DEFAULT (0),
    PaidAmount      DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Sales_Paid     DEFAULT (0),
    BalanceDue      DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Sales_Balance   DEFAULT (0),
    Notes           NVARCHAR(500)     NULL,
    CouponCode      NVARCHAR(50)      NULL,
    -- Audit columns
    CreatedBy       UNIQUEIDENTIFIER  NULL,
    CreatedDate     DATETIME2(7)      NOT NULL CONSTRAINT DF_Sales_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy       UNIQUEIDENTIFIER  NULL,
    UpdatedDate     DATETIME2(7)      NULL,
    DeletedBy       UNIQUEIDENTIFIER  NULL,
    DeletedDate     DATETIME2(7)      NULL,
    IsDeleted       BIT               NOT NULL CONSTRAINT DF_Sales_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION        NOT NULL,
    CONSTRAINT PK_Sales PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Sales_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id),
    CONSTRAINT FK_Sales_Shops FOREIGN KEY (ShopId)
        REFERENCES dbo.Shops (Id),
    CONSTRAINT FK_Sales_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customers (Id),
    CONSTRAINT FK_Sales_Users FOREIGN KEY (CashierId)
        REFERENCES dbo.Users (Id)
);
GO

-- ── SaleItems (line items belonging to a sale) ───────────────────────────
CREATE TABLE dbo.SaleItems
(
    Id              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_SaleItems_Id        DEFAULT NEWID(),
    TenantId        UNIQUEIDENTIFIER  NOT NULL,
    SaleId          UNIQUEIDENTIFIER  NOT NULL,
    ProductId       UNIQUEIDENTIFIER  NOT NULL,
    ProductName     NVARCHAR(200)     NOT NULL,
    Quantity        DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_SaleItems_Qty      DEFAULT (0),
    UnitPrice       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_SaleItems_Price   DEFAULT (0),
    CostPrice       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_SaleItems_Cost    DEFAULT (0),
    DiscountAmount  DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_SaleItems_Disc    DEFAULT (0),
    TaxRate         DECIMAL(5, 2)     NOT NULL CONSTRAINT DF_SaleItems_TaxRate DEFAULT (0),
    TaxAmount       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_SaleItems_Tax    DEFAULT (0),
    LineTotal       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_SaleItems_Total   DEFAULT (0),
    -- Audit columns
    CreatedBy       UNIQUEIDENTIFIER  NULL,
    CreatedDate     DATETIME2(7)      NOT NULL CONSTRAINT DF_SaleItems_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy       UNIQUEIDENTIFIER  NULL,
    UpdatedDate     DATETIME2(7)      NULL,
    DeletedBy       UNIQUEIDENTIFIER  NULL,
    DeletedDate     DATETIME2(7)      NULL,
    IsDeleted       BIT               NOT NULL CONSTRAINT DF_SaleItems_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION        NOT NULL,
    CONSTRAINT PK_SaleItems PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_SaleItems_Sales FOREIGN KEY (SaleId)
        REFERENCES dbo.Sales (Id),
    CONSTRAINT FK_SaleItems_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id)
);
GO

-- ── Purchases (purchase orders from suppliers) ───────────────────────────
CREATE TABLE dbo.Purchases
(
    Id              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Purchases_Id        DEFAULT NEWID(),
    TenantId        UNIQUEIDENTIFIER  NOT NULL,
    PurchaseNumber  NVARCHAR(50)      NOT NULL,
    ShopId           UNIQUEIDENTIFIER  NULL,
    SupplierId      UNIQUEIDENTIFIER  NOT NULL,
    PurchaseDate    DATETIME2(7)      NOT NULL CONSTRAINT DF_Purchases_PurDate DEFAULT SYSUTCDATETIME(),
    Status          INT               NOT NULL CONSTRAINT DF_Purchases_Status DEFAULT (1),  -- Draft
    SubTotal        DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Purchases_Sub     DEFAULT (0),
    DiscountAmount  DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Purchases_Disc    DEFAULT (0),
    TaxAmount       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Purchases_Tax    DEFAULT (0),
    GrandTotal      DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Purchases_Grand   DEFAULT (0),
    PaidAmount      DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Purchases_Paid   DEFAULT (0),
    BalanceDue      DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Purchases_Bal    DEFAULT (0),
    Notes           NVARCHAR(500)     NULL,
    -- Audit columns
    CreatedBy       UNIQUEIDENTIFIER  NULL,
    CreatedDate     DATETIME2(7)      NOT NULL CONSTRAINT DF_Purchases_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy       UNIQUEIDENTIFIER  NULL,
    UpdatedDate     DATETIME2(7)      NULL,
    DeletedBy       UNIQUEIDENTIFIER  NULL,
    DeletedDate     DATETIME2(7)      NULL,
    IsDeleted       BIT               NOT NULL CONSTRAINT DF_Purchases_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION        NOT NULL,
    CONSTRAINT PK_Purchases PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Purchases_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id),
    CONSTRAINT FK_Purchases_Suppliers FOREIGN KEY (SupplierId)
        REFERENCES dbo.Suppliers (Id)
);
GO

-- ── PurchaseItems (line items belonging to a purchase) ──────────────────
CREATE TABLE dbo.PurchaseItems
(
    Id              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_PurItems_Id        DEFAULT NEWID(),
    TenantId        UNIQUEIDENTIFIER  NOT NULL,
    PurchaseId      UNIQUEIDENTIFIER  NOT NULL,
    ProductId       UNIQUEIDENTIFIER  NOT NULL,
    ProductName     NVARCHAR(200)     NOT NULL,
    Quantity        DECIMAL(18, 3)    NOT NULL CONSTRAINT DF_PurItems_Qty      DEFAULT (0),
    UnitCost        DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_PurItems_Cost    DEFAULT (0),
    TaxRate         DECIMAL(5, 2)     NOT NULL CONSTRAINT DF_PurItems_TaxRate DEFAULT (0),
    TaxAmount       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_PurItems_Tax    DEFAULT (0),
    LineTotal       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_PurItems_Total   DEFAULT (0),
    -- Audit columns
    CreatedBy       UNIQUEIDENTIFIER  NULL,
    CreatedDate     DATETIME2(7)      NOT NULL CONSTRAINT DF_PurItems_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy       UNIQUEIDENTIFIER  NULL,
    UpdatedDate     DATETIME2(7)      NULL,
    DeletedBy       UNIQUEIDENTIFIER  NULL,
    DeletedDate     DATETIME2(7)      NULL,
    IsDeleted       BIT               NOT NULL CONSTRAINT DF_PurItems_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION        NOT NULL,
    CONSTRAINT PK_PurchaseItems PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_PurItems_Purchases FOREIGN KEY (PurchaseId)
        REFERENCES dbo.Purchases (Id),
    CONSTRAINT FK_PurItems_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (Id)
);
GO

-- ── Payments (payments received against a sale) ─────────────────────────
CREATE TABLE dbo.Payments
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Payments_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    SaleId       UNIQUEIDENTIFIER  NOT NULL,
    Method       INT               NOT NULL,  -- PaymentMethod enum
    Amount       DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Payments_Amount  DEFAULT (0),
    Reference    NVARCHAR(100)     NULL,
    Notes        NVARCHAR(500)     NULL,
    PaidAt       DATETIME2(7)      NOT NULL CONSTRAINT DF_Payments_PaidAt   DEFAULT SYSUTCDATETIME(),
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Payments_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Payments_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Payments_Sales FOREIGN KEY (SaleId)
        REFERENCES dbo.Sales (Id)
);
GO

-- ── Expenses ─────────────────────────────────────────────────────────────
CREATE TABLE dbo.Expenses
(
    Id             UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Expenses_Id        DEFAULT NEWID(),
    TenantId       UNIQUEIDENTIFIER  NOT NULL,
    Title          NVARCHAR(200)     NOT NULL,
    CategoryId     UNIQUEIDENTIFIER  NULL,
    Amount         DECIMAL(18, 2)    NOT NULL CONSTRAINT DF_Expenses_Amount  DEFAULT (0),
    ExpenseDate    DATETIME2(7)      NOT NULL CONSTRAINT DF_Expenses_Date    DEFAULT SYSUTCDATETIME(),
    PaymentMethod  INT               NOT NULL,  -- PaymentMethod enum
    Reference      NVARCHAR(100)     NULL,
    Notes          NVARCHAR(500)     NULL,
    -- Audit columns
    CreatedBy      UNIQUEIDENTIFIER  NULL,
    CreatedDate    DATETIME2(7)      NOT NULL CONSTRAINT DF_Expenses_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy      UNIQUEIDENTIFIER  NULL,
    UpdatedDate    DATETIME2(7)      NULL,
    DeletedBy      UNIQUEIDENTIFIER  NULL,
    DeletedDate    DATETIME2(7)      NULL,
    IsDeleted      BIT               NOT NULL CONSTRAINT DF_Expenses_IsDeleted DEFAULT (0),
    RowVersion     ROWVERSION        NOT NULL,
    CONSTRAINT PK_Expenses PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Expenses_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── Taxes (tax definitions per tenant) ───────────────────────────────────
CREATE TABLE dbo.Taxes
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Taxes_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    Name         NVARCHAR(100)     NOT NULL,
    Rate         DECIMAL(5, 2)     NOT NULL CONSTRAINT DF_Taxes_Rate      DEFAULT (0),
    IsInclusive  BIT               NOT NULL CONSTRAINT DF_Taxes_Inclusive DEFAULT (0),
    IsActive     BIT               NOT NULL CONSTRAINT DF_Taxes_IsActive DEFAULT (1),
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Taxes_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Taxes_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Taxes PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Taxes_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── Discounts (coupon / discount definitions) ─────────────────────────────
CREATE TABLE dbo.Discounts
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Discounts_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    Name         NVARCHAR(100)     NOT NULL,
    Code         NVARCHAR(50)      NULL,
    Percentage   DECIMAL(5, 2)     NOT NULL CONSTRAINT DF_Discounts_Pct    DEFAULT (0),
    FlatAmount   DECIMAL(18, 2)     NULL,
    ValidFrom    DATETIME2(7)      NULL,
    ValidTo      DATETIME2(7)      NULL,
    IsActive     BIT               NOT NULL CONSTRAINT DF_Discounts_IsActive DEFAULT (1),
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Discounts_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Discounts_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Discounts PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Discounts_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id)
);
GO

-- ── AuditLogs (entity-level change tracking) ─────────────────────────────
CREATE TABLE dbo.AuditLogs
(
    Id          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_AuditLogs_Id        DEFAULT NEWID(),
    TenantId    UNIQUEIDENTIFIER  NOT NULL,
    UserId      UNIQUEIDENTIFIER  NULL,
    UserName    NVARCHAR(256)     NULL,
    Action      NVARCHAR(100)     NOT NULL,
    EntityName  NVARCHAR(100)     NOT NULL,
    EntityId    UNIQUEIDENTIFIER  NULL,
    OldValues   NVARCHAR(MAX)     NULL,
    NewValues   NVARCHAR(MAX)     NULL,
    IpAddress   NVARCHAR(100)     NULL,
    UserAgent   NVARCHAR(500)     NULL,
    -- Audit columns
    CreatedBy   UNIQUEIDENTIFIER  NULL,
    CreatedDate DATETIME2(7)      NOT NULL CONSTRAINT DF_AuditLogs_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy   UNIQUEIDENTIFIER  NULL,
    UpdatedDate DATETIME2(7)      NULL,
    DeletedBy   UNIQUEIDENTIFIER  NULL,
    DeletedDate DATETIME2(7)      NULL,
    IsDeleted   BIT               NOT NULL CONSTRAINT DF_AuditLogs_IsDeleted DEFAULT (0),
    RowVersion  ROWVERSION        NOT NULL,
    CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id)
);
GO

-- ── ActivityLogs (user activity tracking) ────────────────────────────────
CREATE TABLE dbo.ActivityLogs
(
    Id          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_ActivityLogs_Id        DEFAULT NEWID(),
    TenantId    UNIQUEIDENTIFIER  NOT NULL,
    UserId      UNIQUEIDENTIFIER  NULL,
    UserName    NVARCHAR(256)     NULL,
    Activity    NVARCHAR(200)     NOT NULL,
    Module      NVARCHAR(100)     NULL,
    IpAddress   NVARCHAR(100)     NULL,
    UserAgent   NVARCHAR(500)     NULL,
    -- Audit columns
    CreatedBy   UNIQUEIDENTIFIER  NULL,
    CreatedDate DATETIME2(7)      NOT NULL CONSTRAINT DF_ActivityLogs_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy   UNIQUEIDENTIFIER  NULL,
    UpdatedDate DATETIME2(7)      NULL,
    DeletedBy   UNIQUEIDENTIFIER  NULL,
    DeletedDate DATETIME2(7)      NULL,
    IsDeleted   BIT               NOT NULL CONSTRAINT DF_ActivityLogs_IsDeleted DEFAULT (0),
    RowVersion  ROWVERSION        NOT NULL,
    CONSTRAINT PK_ActivityLogs PRIMARY KEY CLUSTERED (Id)
);
GO

-- ── Settings (tenant-scoped key-value configuration) ─────────────────────
CREATE TABLE dbo.Settings
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Settings_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    [Key]        NVARCHAR(100)     NOT NULL,
    [Value]      NVARCHAR(MAX)     NULL,
    [Group]      NVARCHAR(100)     NULL,
    Description  NVARCHAR(500)     NULL,
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Settings_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Settings_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Settings PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Settings_Tenants FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenants (Id),
    CONSTRAINT UQ_Settings_TenantKey UNIQUE NONCLUSTERED (TenantId, [Key])
);
GO

-- ── Notifications (in-app notifications for users) ──────────────────────
CREATE TABLE dbo.Notifications
(
    Id           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Notif_Id        DEFAULT NEWID(),
    TenantId     UNIQUEIDENTIFIER  NOT NULL,
    UserId       UNIQUEIDENTIFIER  NULL,
    Title        NVARCHAR(200)     NOT NULL,
    Message      NVARCHAR(MAX)     NOT NULL,
    Type         NVARCHAR(50)      NULL,
    IsRead       BIT               NOT NULL CONSTRAINT DF_Notif_IsRead   DEFAULT (0),
    Link         NVARCHAR(500)     NULL,
    -- Audit columns
    CreatedBy    UNIQUEIDENTIFIER  NULL,
    CreatedDate  DATETIME2(7)      NOT NULL CONSTRAINT DF_Notif_Created   DEFAULT SYSUTCDATETIME(),
    UpdatedBy    UNIQUEIDENTIFIER  NULL,
    UpdatedDate  DATETIME2(7)      NULL,
    DeletedBy    UNIQUEIDENTIFIER  NULL,
    DeletedDate  DATETIME2(7)      NULL,
    IsDeleted    BIT               NOT NULL CONSTRAINT DF_Notif_IsDeleted DEFAULT (0),
    RowVersion   ROWVERSION        NOT NULL,
    CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Notif_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id)
);
GO

/*===========================================================================
 *  INDEXES
 *  Every tenant-scoped table gets a composite index on (TenantId, IsDeleted)
 *  plus targeted indexes on frequently-filtered / FK columns.
 *==========================================================================*/

-- Tenants
CREATE NONCLUSTERED INDEX IX_Tenants_Status     ON dbo.Tenants (Status, IsDeleted) INCLUDE (Name, Slug);
CREATE NONCLUSTERED INDEX IX_Tenants_CreatedDate ON dbo.Tenants (CreatedDate);

-- Shops
CREATE NONCLUSTERED INDEX IX_Shops_Tenant  ON dbo.Shops (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Shops_Active ON dbo.Shops (TenantId, IsActive, IsDeleted);

-- Users
CREATE NONCLUSTERED INDEX IX_Users_Tenant_Email    ON dbo.Users (TenantId, NormalizedEmail, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Users_Tenant_UserName  ON dbo.Users (TenantId, UserName, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Users_Tenant          ON dbo.Users (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Users_Shop            ON dbo.Users (ShopId) WHERE ShopId IS NOT NULL;

-- Roles
CREATE NONCLUSTERED INDEX IX_Roles_Tenant_Name ON dbo.Roles (TenantId, NormalizedName, IsDeleted);

-- RolePermissions
CREATE NONCLUSTERED INDEX IX_RolePerms_Tenant_Role ON dbo.RolePermissions (TenantId, RoleId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_RolePerms_Permission ON dbo.RolePermissions (PermissionId);

-- UserRoles
CREATE NONCLUSTERED INDEX IX_UserRoles_Tenant_User ON dbo.UserRoles (TenantId, UserId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_UserRoles_Role       ON dbo.UserRoles (RoleId);

-- RefreshTokens
CREATE NONCLUSTERED INDEX IX_Refresh_Tenant_Hash  ON dbo.RefreshTokens (TenantId, TokenHash, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Refresh_User        ON dbo.RefreshTokens (UserId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Refresh_Expires      ON dbo.RefreshTokens (ExpiresAt) WHERE RevokedAt IS NULL;

-- Categories
CREATE NONCLUSTERED INDEX IX_Cats_Tenant      ON dbo.Categories (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Cats_Parent      ON dbo.Categories (ParentCategoryId) WHERE ParentCategoryId IS NOT NULL;

-- Brands
CREATE NONCLUSTERED INDEX IX_Brands_Tenant ON dbo.Brands (TenantId, IsDeleted);

-- Units
CREATE NONCLUSTERED INDEX IX_Units_Tenant ON dbo.Units (TenantId, IsDeleted);

-- Products
CREATE NONCLUSTERED INDEX IX_Products_Tenant    ON dbo.Products (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Products_Tenant_Sku     ON dbo.Products (TenantId, Sku, IsDeleted)  WHERE Sku IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Products_Tenant_Barcode ON dbo.Products (TenantId, Barcode, IsDeleted) WHERE Barcode IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Products_Tenant_Name    ON dbo.Products (TenantId, Name) INCLUDE (IsActive);
CREATE NONCLUSTERED INDEX IX_Products_Category       ON dbo.Products (CategoryId) WHERE CategoryId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Products_Brand           ON dbo.Products (BrandId)   WHERE BrandId   IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Products_LowStock        ON dbo.Products (TenantId, IsDeleted, IsActive) INCLUDE (CurrentStock, ReorderLevel) WHERE IsDeleted = 0 AND IsActive = 1;

-- Inventory
CREATE NONCLUSTERED INDEX IX_Inv_Tenant_Product ON dbo.Inventory (TenantId, ProductId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Inv_Product        ON dbo.Inventory (ProductId);
CREATE NONCLUSTERED INDEX IX_Inv_Shop           ON dbo.Inventory (ShopId) WHERE ShopId IS NOT NULL;

-- StockMovements
CREATE NONCLUSTERED INDEX IX_StockMov_Tenant_Product ON dbo.StockMovements (TenantId, ProductId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_StockMov_Product       ON dbo.StockMovements (ProductId);
CREATE NONCLUSTERED INDEX IX_StockMov_CreatedDate   ON dbo.StockMovements (TenantId, CreatedDate DESC);

-- Customers
CREATE NONCLUSTERED INDEX IX_Customers_Tenant      ON dbo.Customers (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Customers_Tenant_Phone ON dbo.Customers (TenantId, Phone, IsDeleted) WHERE Phone IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Customers_Tenant_Email ON dbo.Customers (TenantId, Email, IsDeleted) WHERE Email IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Customers_Tenant_Name  ON dbo.Customers (TenantId, Name) INCLUDE (IsActive);

-- Suppliers
CREATE NONCLUSTERED INDEX IX_Suppliers_Tenant      ON dbo.Suppliers (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Suppliers_Tenant_Name  ON dbo.Suppliers (TenantId, Name) INCLUDE (IsActive);

-- Sales
CREATE NONCLUSTERED INDEX IX_Sales_Tenant           ON dbo.Sales (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Sales_Tenant_Invoice   ON dbo.Sales (TenantId, InvoiceNumber, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Sales_Tenant_Date      ON dbo.Sales (TenantId, SaleDate DESC) INCLUDE (Status, GrandTotal);
CREATE NONCLUSTERED INDEX IX_Sales_Tenant_Status   ON dbo.Sales (TenantId, Status, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Sales_Customer         ON dbo.Sales (CustomerId) WHERE CustomerId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Sales_Cashier          ON dbo.Sales (CashierId);
CREATE NONCLUSTERED INDEX IX_Sales_Shop             ON dbo.Sales (ShopId) WHERE ShopId IS NOT NULL;

-- SaleItems
CREATE NONCLUSTERED INDEX IX_SaleItems_Sale     ON dbo.SaleItems (SaleId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_SaleItems_Product  ON dbo.SaleItems (ProductId);
CREATE NONCLUSTERED INDEX IX_SaleItems_Tenant   ON dbo.SaleItems (TenantId, IsDeleted);

-- Purchases
CREATE NONCLUSTERED INDEX IX_Purchases_Tenant           ON dbo.Purchases (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Purchases_Tenant_Number   ON dbo.Purchases (TenantId, PurchaseNumber, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Purchases_Tenant_Date      ON dbo.Purchases (TenantId, PurchaseDate DESC) INCLUDE (Status, GrandTotal);
CREATE NONCLUSTERED INDEX IX_Purchases_Supplier        ON dbo.Purchases (SupplierId);
CREATE NONCLUSTERED INDEX IX_Purchases_Shop             ON dbo.Purchases (ShopId) WHERE ShopId IS NOT NULL;

-- PurchaseItems
CREATE NONCLUSTERED INDEX IX_PurItems_Purchase ON dbo.PurchaseItems (PurchaseId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_PurItems_Product  ON dbo.PurchaseItems (ProductId);
CREATE NONCLUSTERED INDEX IX_PurItems_Tenant   ON dbo.PurchaseItems (TenantId, IsDeleted);

-- Payments
CREATE NONCLUSTERED INDEX IX_Payments_Sale    ON dbo.Payments (SaleId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Payments_Tenant  ON dbo.Payments (TenantId, IsDeleted);

-- Expenses
CREATE NONCLUSTERED INDEX IX_Expenses_Tenant      ON dbo.Expenses (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Expenses_Tenant_Date ON dbo.Expenses (TenantId, ExpenseDate DESC);

-- Taxes
CREATE NONCLUSTERED INDEX IX_Taxes_Tenant ON dbo.Taxes (TenantId, IsDeleted);

-- Discounts
CREATE NONCLUSTERED INDEX IX_Discounts_Tenant ON dbo.Discounts (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Discounts_Code   ON dbo.Discounts (TenantId, Code, IsDeleted) WHERE Code IS NOT NULL;

-- AuditLogs
CREATE NONCLUSTERED INDEX IX_AuditLogs_Tenant       ON dbo.AuditLogs (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Tenant_Date  ON dbo.AuditLogs (TenantId, CreatedDate DESC);
CREATE NONCLUSTERED INDEX IX_AuditLogs_Entity       ON dbo.AuditLogs (EntityName, EntityId) WHERE EntityId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_AuditLogs_User         ON dbo.AuditLogs (UserId) WHERE UserId IS NOT NULL;

-- ActivityLogs
CREATE NONCLUSTERED INDEX IX_ActivityLogs_Tenant      ON dbo.ActivityLogs (TenantId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_ActivityLogs_Tenant_Date ON dbo.ActivityLogs (TenantId, CreatedDate DESC);
CREATE NONCLUSTERED INDEX IX_ActivityLogs_User        ON dbo.ActivityLogs (UserId) WHERE UserId IS NOT NULL;

-- Settings
CREATE NONCLUSTERED INDEX IX_Settings_Tenant ON dbo.Settings (TenantId, IsDeleted);

-- Notifications
CREATE NONCLUSTERED INDEX IX_Notif_Tenant_User ON dbo.Notifications (TenantId, UserId, IsDeleted);
CREATE NONCLUSTERED INDEX IX_Notif_Unread       ON dbo.Notifications (TenantId, UserId, IsRead) WHERE IsRead = 0;

-- Permissions
CREATE NONCLUSTERED INDEX IX_Permissions_Group ON dbo.Permissions ([Group], IsDeleted);

PRINT 'Schema created successfully.';
GO
