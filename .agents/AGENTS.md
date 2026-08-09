# 🤖 NewShop ERP & POS — AI Agent & Developer Rules

This document provides mandatory architectural rules, constraints, and operational patterns for any AI agent or developer making changes to the **NewShop Multi-Tenant ERP & POS System**.

---

## 🏛️ 1. Architecture & Layering Rules

The backend strictly follows **Clean Architecture** with CQRS and MediatR:

```
┌─────────────────────────────────────────────────────────────┐
│                       Billing.API                           │
│  (Controllers, Swagger, Middleware, CORS, Composition Root) │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    Billing.Application                      │
│   (Commands, Queries, Handlers, Validators, DTOs, Mappings) │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
               ▼                               ▼
┌─────────────────────────────┐ ┌─────────────────────────────┐
│     Billing.Persistence     │ │    Billing.Infrastructure   │
│ (Dapper, SQL Repositories,  │ │  (Redis Cache, Serilog,     │
│  Transactions, Migrations)  │ │   Token Service, Email)     │
└──────────────┬──────────────┘ └──────────────┬──────────────┘
               │                               │
               ▼                               ▼
┌─────────────────────────────────────────────────────────────┐
│                      Billing.Contracts                      │
│     (Interfaces, Common Abstractions, Generic Results)      │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                       Billing.Domain                        │
│   (Entities, Value Objects, Enums, Domain Exceptions)       │
│                * ZERO EXTERNAL DEPENDENCIES *               │
└─────────────────────────────────────────────────────────────┘
```

### 🚫 STRICT LAYER PROHIBITIONS:
1. **`Billing.Domain`** must NEVER reference any other project or third-party package (except standard BCL).
2. **`Billing.Application`** must NEVER reference `Billing.Persistence`, `Billing.Infrastructure`, or concrete DB drivers.
3. **`Billing.API` Controllers** must NEVER execute SQL or business logic directly — they only dispatch MediatR commands/queries and return `IActionResult`.
4. **Data Access** is strictly performed via **Dapper with parameterized SQL queries / Stored Procedures**. Do NOT add Entity Framework Core unless explicitly requested.

---

## 🏢 2. Multi-Tenancy Rules & Partitioning

1. **Shared Database, Isolated by `TenantId`**:
   - Every tenant-scoped entity and table MUST carry a `TenantId` (GUID).
   - All standard queries MUST filter by `TenantId = @TenantId`.
2. **Tenant Context Resolution**:
   - Injected via `ITenantContext` (resolved from JWT claims `tenant_id` or header `X-Tenant-Id`).
3. **Global Admin (Platform Owner) vs Shop Admin / Staff**:
   - **Global Admin (`admin@billingsystem.com`, Role: `GlobalAdmin`)**:
     - Shop name is always displayed as **`App Admin`**.
     - Sees consolidated, aggregated metrics across **ALL shops** in Dashboard and Platform Reports.
     - Can view and manage users/staff across **ALL tenants** (`/staff` page includes the "Shop / Tenant" column).
     - Operational modules (Products, POS, Purchases, Sales History, Customers, Suppliers) are hidden from Global Admin navigation.
   - **Shop Admin & Staff (`Roles.ShopAdmin`, `Roles.Cashier`, `Roles.Manager`)**:
     - Strict isolation: Only see and operate on records where `TenantId == currentTenantId`.
     - Can never view or modify another shop's products, sales, customers, or staff.

---

## ⚡ 3. Caching & Cache Key Strategy

Redis is used for caching with an automatic in-memory fallback (`RedisCacheService`).

### Cache Key Rules:
- Cache keys MUST include:
  1. Prefix and scope (`global` vs `tenantId`).
  2. All query parameters / date ranges (`from:yyyyMMdd:to:yyyyMMdd`).
- **Correct Pattern**:
  ```csharp
  var scope = request.IsGlobalAdmin ? "dashboard:global" : $"dashboard:{_tenantContext.TenantId}";
  var cacheKey = $"{scope}:{from:yyyyMMdd}:{to:yyyyMMdd}";
  ```
- **Never** cache with just the TenantId without query parameters — this causes current vs previous comparison queries to overwrite and return identical data!

---

## ⚛️ 4. Frontend (React + Vite + Redux) Rules

1. **Rules of Hooks**:
   - `useAppSelector`, `useQuery`, `useState`, and other React hooks MUST be called **unconditionally at the top** of functional components, **BEFORE** any `if (isLoading)` or `if (error)` early return statements.
2. **Dynamic API URL**:
   - `frontend/src/api/client.ts` uses `import.meta.env.VITE_API_URL` with fallback to `/api`.
   - Never hardcode localhost or production URLs in API calls.
3. **Currency & Formatting**:
   - Use standard helper `formatCurrency(val)` from `../utils/helpers` (INR `₹` formatting).
4. **Vercel & SPA Routing**:
   - Frontend SPA routes are rewritten to `/index.html` via `frontend/vercel.json`.
   - The production build runs `vite build` directly via Node.js to avoid Linux `.bin` permission issues.

---

## 🗄️ 5. Database & SQL Conventions

1. **SQL Server Engine**: Microsoft SQL Server 2022+ (T-SQL syntax).
2. **Primary Keys**: GUIDs (`UNIQUEIDENTIFIER`), defaulting to `NEWID()`.
3. **Optimistic Concurrency**: `RowVersion ROWVERSION NOT NULL` on transactional tables.
4. **Soft Delete**: `IsDeleted BIT NOT NULL DEFAULT 0`.
5. **Audit Columns**: `CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()`, `CreatedBy UNIQUEIDENTIFIER`, `UpdatedAt DATETIME2`, `UpdatedBy UNIQUEIDENTIFIER`.
6. **Data Types**:
   - Monetary values: `DECIMAL(18,2)`.
   - Quantities / Measurements: `DECIMAL(18,3)`.
   - Strings / Text: `NVARCHAR(...)`.

---

## 🔐 6. Authentication & Permissions Matrix

- **Password Hashing**: ASP.NET Core Identity PBKDF2 with HMAC-SHA256 (10,000+ iterations).
- **JWT Lifetimes**:
  - Access Token: 60 minutes.
  - Refresh Token: 7 days with rotation and database revocation tracking (`RefreshTokens` table).

### Default System Accounts:
- **Global Administrator**: `admin@billingsystem.com` / `Admin@123`
- **Demo Shop Admin**: `shopadmin@demo.com` / `ShopAdmin@123`
- **Store Cashier**: `cashier@demo.com` / `Cashier@123`

---

## 🧪 7. Verification Checklist Before Committing Changes

Before pushing any code or completing a task, you MUST verify:
1. `dotnet build` compiles cleanly with 0 errors.
2. `npx tsc --noEmit` (in `frontend/`) passes with 0 type errors.
3. `npm run build` (in `frontend/`) completes successfully.
4. Multi-tenancy integrity is preserved: GlobalAdmin sees aggregated data / "App Admin", while Shop tenants are isolated.
5. All environment variables have proper defaults and documentation.
