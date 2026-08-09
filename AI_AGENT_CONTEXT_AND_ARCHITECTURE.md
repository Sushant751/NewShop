# 🧠 NewShop — AI Agent Context & System Architecture

> **Notice for AI Agents (Antigravity, Cursor, GitHub Copilot, Claude, GPT, etc.):**
> Read this entire document BEFORE analyzing or modifying any code in this repository. It contains the complete architectural blueprint, multi-tenancy rules, database conventions, and critical bug-prevention constraints for the **NewShop Multi-Tenant ERP & POS System**.

---

## 📑 Table of Contents
1. [System Overview & Tech Stack](#1-system-overview--tech-stack)
2. [Clean Architecture & Folder Structure](#2-clean-architecture--folder-structure)
3. [Multi-Tenancy & Role-Based Access Control (RBAC)](#3-multi-tenancy--role-based-access-control-rbac)
4. [Database Design & Dapper Conventions](#4-database-design--dapper-conventions)
5. [CQRS & MediatR Pattern Rules](#5-cqrs--mediatr-pattern-rules)
6. [Frontend (React + Vite + Redux + MUI) Guidelines](#6-frontend-react--vite--redux--mui-guidelines)
7. [Caching & Performance Strategy](#7-caching--performance-strategy)
8. [Live Deployment Topology & Environment Variables](#8-live-deployment-topology--environment-variables)
9. [Common Gotchas & Bug-Prevention Checklist](#9-common-gotchas--bug-prevention-checklist)

---

## 1. System Overview & Tech Stack

**NewShop** is a production-grade, enterprise multi-tenant Software-as-a-Service (SaaS) ERP, POS, and inventory management platform.

### Core Technologies:
- **Backend**: ASP.NET Core 9.0 (.NET 9 C# 13), MediatR CQRS, FluentValidation, Serilog.
- **Data Access**: Dapper Micro-ORM, Microsoft SQL Server 2022+ (T-SQL Stored Procedures & parameterized queries).
- **Authentication**: JWT Bearer Tokens (HMAC-SHA256) with Refresh Token Rotation and PBKDF2 Password Hashing.
- **Caching**: Distributed Redis with graceful In-Memory fallback.
- **Frontend**: React 18, TypeScript, Vite 5, Redux Toolkit, React Query, Material-UI (Mantis Theme), Recharts.
- **Deployment**:
  - Frontend: Vercel (Global Edge CDN) / Netlify / Azure Static Web Apps.
  - Backend API: Render.com (Docker Linux Container) / Azure App Service / IIS.
  - Database: Cloud MS SQL Server (`db63059.public.databaseasp.net`) / Azure SQL.

---

## 2. Clean Architecture & Folder Structure

```
d:\Projects\ERP\NewShop\
├── .agents/
│   └── AGENTS.md                  # AI agent workspace rules & guardrails
├── database/
│   ├── 01_Schema.sql              # 29 DDL Tables, Indexes & Foreign Keys
│   ├── 02_StoredProcedures.sql    # Business Logic SPs, Views & Triggers
│   └── 03_SeedData.sql            # Initial Tenants, Permissions, Demo & Admin Accounts
├── frontend/                      # React 18 + Vite + TypeScript SPA
│   ├── src/
│   │   ├── api/                   # Axios client, JWT interceptor, endpoints
│   │   ├── components/            # Layout, ProtectedRoute, Navigation, Common UI
│   │   ├── pages/                 # Dashboard, POS, Products, Sales, Reports, Staff, Settings
│   │   ├── store/                 # Redux Toolkit slices (auth, pos, cart, theme)
│   │   └── types/                 # TypeScript interfaces, DTOs, Permissions, Roles
│   ├── public/                    # _redirects (SPA routing), vite.svg favicon
│   ├── vercel.json                # Vercel SPA client rewrite configuration
│   └── vite.config.ts             # Vite build & proxy settings
├── src/
│   ├── Billing.Domain/            # Entities, Enums, Value Objects (0 dependencies)
│   ├── Billing.Contracts/         # Interfaces, Result<T>, PagedResult<T>
│   ├── Billing.Application/       # MediatR Commands, Queries, Handlers, Validators, DTOs
│   ├── Billing.Persistence/       # Dapper Repositories, DB Connections, SQL Migrations
│   ├── Billing.Infrastructure/    # Redis Cache, Serilog, Token Generation, Email
│   ├── Billing.Identity/          # User Authentication, Password Hashing, JWT Service
│   └── Billing.API/               # Controllers, Middleware, Swagger, Composition Root
├── tests/
│   ├── Billing.UnitTests/         # Domain & Handler Unit Tests (xUnit, FluentAssertions)
│   └── Billing.IntegrationTests/  # Controller & DB Integration Tests
├── Dockerfile                     # Multi-stage .NET 9 Release build container
├── package.json                   # Root monorepo build scripts
└── BillingSystem.sln              # Visual Studio / .NET Solution File
```

### ⚠️ Strict Architectural Invariants:
1. **`Billing.Domain`** has **zero external references**. Never add persistence or API libraries to Domain.
2. **`Billing.Application`** contains all business workflows via CQRS. It references only `Domain` and `Contracts`.
3. **`Billing.Persistence`** implements repository interfaces using Dapper and raw SQL/Stored Procedures.
4. **`Billing.API`** is a thin entry point. Controllers MUST NEVER query the database or execute business logic directly.

---

## 3. Multi-Tenancy & Role-Based Access Control (RBAC)

The system uses a **Single Shared Database with Row-Level Tenant Partitioning (`TenantId`)**:

```
                              Tenant Hierarchy
                              
                    ┌───────────────────────────────────┐
                    │    App Admin (GlobalAdmin)        │
                    │  (Platform Owner / Super Admin)   │
                    └─────────────────┬─────────────────┘
                                      │ Aggregated Visibility
                     ┌────────────────┴────────────────┐
                     ▼                                 ▼
           ┌───────────────────┐             ┌───────────────────┐
           │     Tenant A      │             │     Tenant B      │
           │   ("Demo Shop")   │             │("Subhadra Compute")│
           ├───────────────────┤             ├───────────────────┤
           │ • ShopAdmin       │             │ • ShopAdmin       │
           │ • Store Manager   │             │ • Store Manager   │
           │ • Cashier         │             │ • Cashier         │
           │ • Store Inventory │             │ • Store Inventory │
           └───────────────────┘             └───────────────────┘
```

### Tenant & Role Invariants:
1. **Global Admin (`Roles.GlobalAdmin`)**:
   - `admin@billingsystem.com` / `Admin@123`.
   - Brand name in header is explicitly **`App Admin`**.
   - Receives **consolidated platform metrics** (aggregates sales, revenue, orders, products across all tenants).
   - Manages all system users across all shops on the `/staff` page.
   - Store operational tools (POS, Catalog, Purchases, Customer List) are hidden from Global Admin navigation.
2. **Shop Admin & Staff (`Roles.ShopAdmin`, `Roles.Cashier`, `Roles.Manager`)**:
   - Strictly isolated by their `TenantId`.
   - Can never see, query, or modify another store's products, sales, stock, or users.

---

## 4. Database Design & Dapper Conventions

### Core Relational Tables (29 Tables):
- **Tenants & Platform**: `Tenants`, `Plans`, `Settings`, `AuditLogs`, `ActivityLogs`.
- **Identity & Access**: `Users`, `Roles`, `UserRoles`, `Permissions`, `RolePermissions`, `RefreshTokens`.
- **Product Catalog**: `Products`, `Categories`, `Brands`, `Units`, `Taxes`, `Discounts`.
- **Inventory & Stock**: `Inventory`, `StockMovements`, `Suppliers`, `Purchases`, `PurchaseItems`.
- **POS & Sales**: `Shops`, `Sales`, `SaleItems`, `Payments`, `Customers`.

### T-SQL / Dapper Rules:
- Primary keys are GUIDs (`UNIQUEIDENTIFIER`), defaulted to `NEWID()`.
- Money columns: `DECIMAL(18,2)`. Quantity columns: `DECIMAL(18,3)`.
- Audit columns on all tables: `CreatedAt DATETIME2 DEFAULT GETUTCDATE()`, `CreatedBy UNIQUEIDENTIFIER`, `UpdatedAt DATETIME2`, `UpdatedBy UNIQUEIDENTIFIER`, `IsDeleted BIT DEFAULT 0`.
- Optimistic Concurrency: `RowVersion ROWVERSION NOT NULL`.
- Always use parameterized queries (`@TenantId`, `@Id`) to prevent SQL injection.

---

## 5. CQRS & MediatR Pattern Rules

Every operation is divided into **Commands** (mutations) and **Queries** (reads):

```csharp
// Example Query
public record GetDashboardQuery(DateTime? From, DateTime? To, bool IsGlobalAdmin = false) 
    : IRequest<Result<DashboardDto>>;

// Example Command
public record CreateSaleCommand(Guid CustomerId, List<SaleItemDto> Items, decimal DiscountAmount, string PaymentMethod) 
    : IRequest<Result<SaleDto>>;
```

- Handlers return `Result<T>` or `PagedResult<T>` from `Billing.Shared.Results`.
- Controllers unwrap results: `return result.IsSuccess ? Ok(result) : BadRequest(result);`.

---

## 6. Frontend (React + Vite + Redux + MUI) Guidelines

1. **Rules of Hooks**:
   - All hooks (`useAppSelector`, `useQuery`, `useState`, `useLocation`) MUST be called at the very top of components before any `if (isLoading)` or `if (error)` early returns.
2. **Dynamic Base URL**:
   - Configured in `frontend/src/api/client.ts` using `import.meta.env.VITE_API_URL`.
3. **Mantis Theme & Styling**:
   - Primary blue: `#4680ff`, Admin blue: `#1890ff`, Success green: `#52c41a`.
   - Currency formatting: Always use `formatCurrency(val)` from `../utils/helpers` (formats as INR `₹`).

---

## 7. Caching & Performance Strategy

- Redis is the primary distributed cache; falls back automatically to In-Memory cache if Redis is unavailable.
- **Cache Key Pattern**:
  ```csharp
  var scope = request.IsGlobalAdmin ? "dashboard:global" : $"dashboard:{_tenantContext.TenantId}";
  var cacheKey = $"{scope}:{from:yyyyMMdd}:{to:yyyyMMdd}";
  ```
- Clear user query caches on logout in `Layout.tsx` (`queryClient.clear()`) to prevent state leaks between accounts.

---

## 8. Live Deployment Topology & Environment Variables

| Component | Provider | Live URL / Host |
|---|---|---|
| **Frontend** | Vercel (Edge CDN) | `https://<your-app>.vercel.app` |
| **Backend API** | Render (Docker Linux) | `https://newshop-api.onrender.com` |
| **Database** | Cloud MS SQL Server | `db63059.public.databaseasp.net` (DB: `db63059`) |

### Critical Environment Variables:
- `ConnectionStrings__DefaultConnection`: `Server=db63059.public.databaseasp.net; Database=db63059; User Id=db63059; Password=...; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;`
- `JwtSettings__SecretKey`: At least 32 characters long random secret.
- `JwtSettings__Issuer`: `BillingApi`
- `JwtSettings__Audience`: `BillingSpa`
- `VITE_API_URL`: `https://newshop-api.onrender.com`

---

## 9. Common Gotchas & Bug-Prevention Checklist

| Trap | Prevention |
|---|---|
| **Early returns before hooks** | Always declare all `useAppSelector`, `useState`, `useQuery` before any `if (isLoading)` returns. |
| **Cache key date collisions** | Always append `:from:yyyyMMdd:to:yyyyMMdd` to dashboard and report cache keys. |
| **Global Admin shop name** | Always show `"App Admin"` when `user.roles.includes('GlobalAdmin')`. |
| **Linux build permissions** | In `package.json`, use `node ./node_modules/vite/bin/vite.js build` rather than relying on `/node_modules/.bin/` symlinks. |
| **SPA 404 on deep links** | Maintain `rewrites` to `/index.html` in `frontend/vercel.json` and `_redirects`. |
| **Direct SQL execution** | Always execute SQL via Dapper in `Billing.Persistence`, never in Controllers or Handlers directly. |
