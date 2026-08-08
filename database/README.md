# BillingSystem Database Scripts

SQL Server DDL, stored procedures, and seed data for the Multi-Tenant Billing, POS, Inventory & Shop Management SaaS.

## Prerequisites

- **SQL Server 2019+** (or Azure SQL Database, SQL Server Express)
- **SQL Server Management Studio (SSMS)** or `sqlcmd` CLI

## Files

| File | Description |
|------|-------------|
| [`01_Schema.sql`](01_Schema.sql) | Creates the `BillingSystem` database and all 29 tables with constraints, foreign keys, and 50+ indexes |
| [`02_StoredProcedures.sql`](02_StoredProcedures.sql) | 10 stored procedures for invoice generation, dashboard, reports, stock adjustment, and user permissions |
| [`03_SeedData.sql`](03_SeedData.sql) | Idempotent seed data: 3 plans, 19 permissions, demo tenant, 5 roles, admin user, role-permission mappings, sample catalog |
| [`HashGen/`](HashGen/) | .NET 9 console utility to generate PBKDF2 password hashes (used to create the admin password hash) |

## Run Order

**Always execute in this order:**

```
01_Schema.sql  →  02_StoredProcedures.sql  →  03_SeedData.sql
```

### Option A: Using SSMS

1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Open and execute each file in order (F5)

### Option B: Using sqlcmd

```cmd
sqlcmd -S localhost -E -i database\01_Schema.sql
sqlcmd -S localhost -E -i database\02_StoredProcedures.sql
sqlcmd -S localhost -E -i database\03_SeedData.sql
```

### Option C: Using the batch script (Windows)

```cmd
database\run-all.bat
```

Edit `run-all.bat` to change the server name if needed.

## Default Admin Credentials

| Field | Value |
|-------|-------|
| **Email** | `admin@billingsystem.com` |
| **Password** | `Admin@123` |

The password hash was generated using ASP.NET Core Identity's `PasswordHasher<T>` (PBKDF2 with HMAC-SHA256, 256-bit salt, 100,000 iterations) to ensure compatibility with the application's [`PasswordHasherService`](../src/Billing.Identity/Passwords/PasswordHasherService.cs).

## Seed Data Summary

| Entity | Count | Notes |
|--------|-------|-------|
| Plans | 3 | Starter ($19/mo), Professional ($49/mo), Enterprise ($149/mo) |
| Permissions | 19 | All permission constants from [`Roles.cs`](../src/Billing.Shared/Enums/Roles.cs) |
| Tenants | 1 | "Demo Shop" (slug: `demo-shop`) on Professional plan |
| Shops | 1 | "Main Branch" (code: `MAIN-01`) |
| Roles | 5 | GlobalAdmin, ShopAdmin, Manager, Cashier, Staff |
| RolePermissions | 46 | GlobalAdmin=19, ShopAdmin=19, Manager=17, Cashier=6, Staff=4 |
| Users | 1 | System Administrator (GlobalAdmin role) |
| UserRoles | 1 | Admin → GlobalAdmin |
| Settings | 6 | CompanyName, CurrencySymbol, TaxRate, InvoicePrefix, PurchasePrefix, LowStockAlertEnabled |
| Units | 3 | Pieces (pcs), Kilogram (kg), Litre (ltr) |
| Brands | 2 | Generic, Premium Choice |
| Categories | 3 | Grocery (parent), Beverages, Snacks (children) |
| Products | 5 | Mineral Water, Orange Juice, Potato Chips, Chocolate Cookies, Rice |
| Inventory | 5 | One record per product at Main Branch |
| StockMovements | 5 | InitialStock (type=8) for each product |
| Customers | 1 | Walk-in Customer |
| Suppliers | 1 | Global Supplies Inc. |

## Regenerating the Password Hash

If you need a different admin password, update the hash in `03_SeedData.sql`:

```cmd
cd database\HashGen
dotnet run
```

Copy the output hash and replace the `@AdminPasswordHash` variable value in `03_SeedData.sql`.

## Idempotency

All seed data inserts use `IF NOT EXISTS` guards with fixed GUIDs, so the script can be re-run safely without creating duplicates.

## Database Diagram (Simplified)

```
Plans (global)
  └── Tenants (global)
        ├── Shops
        ├── Users ──── UserRoles ──── Roles ──── RolePermissions ──── Permissions (global)
        │              └── RefreshTokens
        ├── Categories (self-ref)
        ├── Brands
        ├── Units
        ├── Products ──┬── Inventory
        │              └── StockMovements
        ├── Customers
        ├── Suppliers
        ├── Sales ──── SaleItems
        │      └── Payments
        ├── Purchases ── PurchaseItems
        ├── Expenses
        ├── Taxes
        ├── Discounts
        ├── Settings
        ├── AuditLogs
        ├── ActivityLogs
        └── Notifications
```
