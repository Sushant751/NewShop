# 🛒 Multi-Tenant Billing, POS, Inventory & Shop Management SaaS

A production-ready, enterprise-grade **Multi-Tenant SaaS** application for small and medium businesses — covering **Point of Sale (POS)**, **Inventory**, **Purchases**, **Customers/Suppliers**, **Reporting (P&L, GST, Sales, Top Products)**, and **Dashboard analytics**.

Built with **Clean Architecture**, **CQRS**, **Dapper (no Entity Framework Core)**, **JWT + Refresh Tokens**, **role/permission-based authorization**, and a modern **React + TypeScript + Material UI** frontend.

---

## ✨ Features

| Module | Capabilities |
|--------|-------------|
| **Authentication** | Register, Login, JWT access + refresh tokens, change password, forgot/reset password, account lockout, session/device tracking |
| **Multi-Tenancy** | Single shared database, shared schema, automatic `TenantId` filtering on every query, per-tenant isolation |
| **POS** | Product search, cart management, multi-payment, customer linking, discount/tax calculation, invoice generation |
| **Inventory** | Stock tracking per shop, stock movements (purchase, sale, adjustment, initial, return), low-stock alerts, inventory valuation |
| **Products** | Full CRUD, categories, brands, units, GST/tax rates, barcode/SKU, active/inactive toggle |
| **Purchases** | Supplier purchase orders, multi-item entry, status tracking (Pending/Received/Cancelled) |
| **Customers** | CRUD, loyalty points, credit balance, search/filter |
| **Suppliers** | CRUD, contact details, search/filter |
| **Reports** | Profit & Loss, Sales report, GST report, Payment summary, Inventory valuation, Top products, Reports dashboard — all with **CSV export** |
| **Dashboard** | Revenue, orders, AOV, low-stock count, daily sales trend chart, top products bar chart |
| **Authorization** | 5 roles × 19 permissions, route guarding, nav filtering, API-level permission checks |

---

## 🏗️ Tech Stack

### Backend
| Technology | Purpose |
|-----------|---------|
| **.NET 9 (ASP.NET Core)** | Web API framework |
| **C# 13** | Primary language |
| **Dapper** | Micro-ORM — sole data access layer (parameterized SQL, `DynamicParameters`) |
| **SQL Server 2022** | Relational database |
| **ASP.NET Core Identity** | Password hashing only (PBKDF2) |
| **JWT + Refresh Tokens** | Stateless authentication with rotation |
| **MediatR** | CQRS (commands, queries, handlers) |
| **FluentValidation** | Request DTO validation |
| **AutoMapper** | Entity ↔ DTO mapping |
| **Serilog** | Structured logging (console + file sinks) |
| **Redis** | Distributed caching (`ICacheService`) |
| **Swashbuckle / Swagger** | API documentation & UI |
| **AspNetCoreRateLimit** | API rate limiting |

### Frontend
| Technology | Purpose |
|-----------|---------|
| **React 18** | UI library |
| **TypeScript 5** | Type-safe development |
| **Vite 6** | Build tool & dev server |
| **Redux Toolkit** | Global state (auth slice) |
| **React Query 3** | Server state & data fetching |
| **React Router 6** | Client-side routing |
| **Axios** | HTTP client with JWT interceptors & refresh queue |
| **Material UI 6** | Component library & theming |
| **React Hook Form** | Form management |
| **Recharts** | Dashboard charts |

### Infrastructure
| Technology | Purpose |
|-----------|---------|
| **Docker** | Containerization (multi-stage builds) |
| **docker-compose** | Orchestration (db, redis, api, frontend) |
| **nginx** | SPA serving + reverse proxy to API |

---

## 📂 Project Structure

```
NewShop/
├── BillingSystem.sln                 # Solution file (8 projects + 2 test projects)
├── Directory.Build.props             # Central .NET 9 build properties
├── docker-compose.yml                # Full-stack orchestration
├── .env.example                      # Environment variable template
├── .dockerignore
│
├── src/                              # ── Backend (Clean Architecture) ──
│   ├── Billing.Shared/               #   Shared kernel: Result<T>, enums, constants, exceptions
│   ├── Billing.Domain/               #   Domain entities, base classes (no dependencies)
│   ├── Billing.Contracts/            #   Application abstractions (interfaces)
│   ├── Billing.Application/          #   CQRS: commands, queries, handlers, DTOs, validators
│   ├── Billing.Persistence/          #   Dapper repositories, UnitOfWork, ConnectionFactory, TenantContext
│   ├── Billing.Infrastructure/       #   Cross-cutting: caching, current-user service
│   ├── Billing.Identity/             #   JWT token service, auth service, password hasher
│   └── Billing.API/                   #   Controllers, middleware, DI, Swagger, Program.cs
│
├── tests/                            # ── Test Projects ──
│   ├── Billing.UnitTests/
│   └── Billing.IntegrationTests/
│
├── database/                         # ── SQL Server Scripts ──
│   ├── 01_Schema.sql                 #   29 tables, constraints, 50+ indexes
│   ├── 02_StoredProcedures.sql       #   10 stored procedures
│   ├── 03_SeedData.sql               #   Plans, permissions, demo tenant, roles, admin, catalog
│   ├── README.md
│   ├── run-all.bat
│   └── HashGen/                       #   PBKDF2 password hash generator utility
│
└── frontend/                         # ── React Frontend ──
    ├── Dockerfile                    #   Multi-stage: node build → nginx serve
    ├── nginx.conf                    #   SPA fallback + /api proxy
    ├── package.json
    ├── vite.config.ts
    └── src/
        ├── api/                      #   Axios client, endpoints, query client
        ├── components/               #   Layout, ProtectedRoute
        ├── pages/                    #   Login, Register, Dashboard, POS, Products, Sales,
        │                             #   Customers, Suppliers, Purchases, Reports, Settings
        ├── store/                    #   Redux Toolkit (auth slice)
        ├── types/                    #   TypeScript interfaces & enums
        ├── utils/                    #   Helpers (getErrorMessage, formatCurrency)
        ├── theme.ts
        ├── App.tsx                   #   Routes
        └── main.tsx                  #   Entry point
```

### Clean Architecture Dependency Rules

```
Billing.API  →  Billing.Application  →  Billing.Contracts  →  Billing.Domain
     ↓                ↓
Billing.Identity   Billing.Persistence
     ↓                ↓
Billing.Infrastructure  →  Billing.Shared (shared by all)
```

> **Billing.Domain** has zero external dependencies. **Billing.Persistence** depends only on Dapper + Domain. The API layer composes everything via Dependency Injection.

---

## 🚀 Quick Start

### Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ |
| [Node.js](https://nodejs.org/) | 20+ |
| [SQL Server](https://www.microsoft.com/sql-server) | 2019+ (or Docker) |
| [Redis](https://redis.io/) | 7+ (or Docker) |
| [Docker](https://docker.com/) (optional) | 24+ |

---

### Option 1: Docker (Recommended — All-in-One)

```bash
# 1. Create your environment file
cp .env.example .env          # Linux/macOS
copy .env.example .env        # Windows cmd

# 2. (Optional) Edit .env to set a strong DB password & JWT secret

# 3. Build and start all services
docker-compose up -d --build

# 4. Check status
docker-compose ps
```

| Service | URL | Port |
|---------|-----|------|
| **Frontend** | http://localhost:8081 | 8081 |
| **API (Swagger)** | http://localhost:5000/swagger | 5000 |
| **API (Health)** | http://localhost:5000/health | 5000 |
| **SQL Server** | localhost,1433 | 1433 |
| **Redis** | localhost:6379 | 6379 |

> **Note:** The database schema (`01_Schema.sql`) is auto-mounted into the SQL Server container's init directory. If the container starts fresh, run the remaining scripts manually (see [Database Setup](#database-setup) below) or use `docker exec` to apply them.

**Stop everything:**
```bash
docker-compose down              # stop containers
docker-compose down -v           # stop + delete volumes (data loss!)
```

---

### Option 2: Local Development

#### 1. Database Setup

Execute the SQL scripts in order against your SQL Server instance:

```bash
# Using sqlcmd (Windows auth)
sqlcmd -S localhost -E -i database/01_Schema.sql
sqlcmd -S localhost -E -i database/02_StoredProcedures.sql
sqlcmd -S localhost -E -i database/03_SeedData.sql

# Or use the batch script
database\run-all.bat

# Or open in SSMS and execute each file (F5)
```

See [`database/README.md`](database/README.md) for full details.

#### 2. Backend

```bash
# Restore & build
dotnet restore BillingSystem.sln
dotnet build BillingSystem.sln

# Set connection string (user secrets or appsettings.Development.json)
cd src/Billing.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=BillingSystem;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true;"
dotnet user-secrets set "Cache:ConnectionString" "localhost:6379"
dotnet user-secrets set "Jwt:SecretKey" "SuperSecretKey_Change_In_Production_At_Least_32_Characters_Long_2026!"

# Run the API (listens on http://localhost:5000)
dotnet run
```

Swagger UI: **http://localhost:5000/swagger**

#### 3. Frontend

```bash
cd frontend
npm install
npm run dev          # Vite dev server → http://localhost:5173
```

The Vite dev server proxies `/api` → `http://localhost:5000` (see [`vite.config.ts`](frontend/vite.config.ts)).

---

## 🔑 Default Credentials

| Field | Value |
|-------|-------|
| **Email** | `admin@billingsystem.com` |
| **Password** | `Admin@123` |
| **Role** | GlobalAdmin (all 19 permissions) |

> ⚠️ **Change this password immediately** in any non-demo deployment.

---

## 👥 Roles & Permissions

### Roles (5)

| Role | Description | Permissions |
|------|-------------|------------|
| **GlobalAdmin** | Full system access | All 19 |
| **ShopAdmin** | Tenant-level admin | All 19 |
| **Manager** | Day-to-day operations | 17 (no StaffManage, SettingsManage limited) |
| **Cashier** | POS operations | 6 (Products.View, Sales.Create/Cancel, Customers.View, Inventory.View, Reports.View) |
| **Staff** | Basic access | 4 (Products.View, Sales.Create, Customers.View, Inventory.View) |

### Permissions (19)

| Category | Permissions |
|----------|------------|
| **Products** | `Products.View`, `Products.Create`, `Products.Edit`, `Products.Delete` |
| **Sales** | `Sales.Create`, `Sales.Cancel` |
| **Customers** | `Customers.View`, `Customers.Create`, `Customers.Edit`, `Customers.Delete` |
| **Purchases** | `Purchases.View`, `Purchases.Create` |
| **Inventory** | `Inventory.View`, `Inventory.Adjust` |
| **Reports** | `Reports.View` |
| **Expenses** | `Expenses.View`, `Expenses.Manage` |
| **Settings** | `Settings.Manage`, `Staff.Manage` |

---

## 🌐 API Reference

### Base URL
- Local dev: `http://localhost:5000/api`
- Docker: `http://localhost:5000/api`

### Standard Response Format

All API endpoints return a consistent envelope:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { },
  "errors": []
}
```

On failure:
```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": ["'Name' must not be empty.", "'Price' must be positive."]
}
```

### Endpoints

#### Authentication (`/api/auth`)

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `POST` | `/auth/login` | Login with email + password → JWT + refresh token | Public |
| `POST` | `/auth/register` | Register new tenant + admin user | Public |
| `POST` | `/auth/refresh` | Refresh access token | Refresh token |
| `POST` | `/auth/revoke` | Revoke a refresh token | Authenticated |
| `POST` | `/auth/logout` | Logout (revoke current session) | Authenticated |
| `POST` | `/auth/change-password` | Change current user's password | Authenticated |
| `POST` | `/auth/forgot-password` | Request password reset email | Public |
| `POST` | `/auth/reset-password` | Reset password with token | Public |

#### Products (`/api/products`)

| Method | Path | Description | Permission |
|--------|------|-------------|------------|
| `GET` | `/products` | List/search products (paginated) | `Products.View` |
| `GET` | `/products/{id}` | Get product by ID | `Products.View` |
| `POST` | `/products` | Create product | `Products.Create` |
| `PUT` | `/products/{id}` | Update product | `Products.Edit` |
| `DELETE` | `/products/{id}` | Delete product | `Products.Delete` |

#### Sales (`/api/sales`)

| Method | Path | Description | Permission |
|--------|------|-------------|------------|
| `GET` | `/sales` | List sales (paginated, filterable) | `Sales.Create` |
| `GET` | `/sales/{id}` | Get sale detail (items + payments) | `Sales.Create` |
| `POST` | `/sales` | Create sale (POS checkout) | `Sales.Create` |
| `POST` | `/sales/{id}/cancel` | Cancel a sale | `Sales.Cancel` |

#### Customers (`/api/customers`)

| Method | Path | Description | Permission |
|--------|------|-------------|------------|
| `GET` | `/customers` | List/search customers | `Customers.View` |
| `GET` | `/customers/{id}` | Get customer by ID | `Customers.View` |
| `POST` | `/customers` | Create customer | `Customers.Create` |
| `PUT` | `/customers/{id}` | Update customer | `Customers.Edit` |
| `DELETE` | `/customers/{id}` | Delete customer | `Customers.Delete` |

#### Suppliers (`/api/suppliers`)

| Method | Path | Description | Permission |
|--------|------|-------------|------------|
| `GET` | `/suppliers` | List/search suppliers | `Purchases.View` |
| `GET` | `/suppliers/{id}` | Get supplier by ID | `Purchases.View` |
| `POST` | `/suppliers` | Create supplier | `Purchases.Create` |
| `PUT` | `/suppliers/{id}` | Update supplier | `Purchases.Create` |
| `DELETE` | `/suppliers/{id}` | Delete supplier | `Purchases.Create` |

#### Purchases (`/api/purchases`)

| Method | Path | Description | Permission |
|--------|------|-------------|------------|
| `GET` | `/purchases` | List purchases (paginated) | `Purchases.View` |
| `GET` | `/purchases/{id}` | Get purchase detail | `Purchases.View` |
| `POST` | `/purchases` | Create purchase order | `Purchases.Create` |

#### Dashboard (`/api/dashboard`)

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `GET` | `/dashboard?from=&to=` | Dashboard summary (KPIs, daily sales, top products) | Authenticated |

#### Reports (`/api/reports`)

| Method | Path | Description | Permission |
|--------|------|-------------|------------|
| `GET` | `/reports/profit-loss?from=&to=` | Profit & Loss statement | `Reports.View` |
| `GET` | `/reports/sales?from=&to=` | Sales report summary | `Reports.View` |
| `GET` | `/reports/sales/export?from=&to=` | Sales CSV export | `Reports.View` |
| `GET` | `/reports/gst?from=&to=` | GST report | `Reports.View` |
| `GET` | `/reports/gst/export?from=&to=` | GST CSV export | `Reports.View` |
| `GET` | `/reports/payments?from=&to=` | Payment method summary | `Reports.View` |
| `GET` | `/reports/inventory-valuation` | Inventory valuation | `Reports.View` |
| `GET` | `/reports/inventory-valuation/export` | Inventory valuation CSV export | `Reports.View` |
| `GET` | `/reports/top-products?from=&to=&top=` | Top-selling products | `Reports.View` |
| `GET` | `/reports/dashboard?from=&to=` | Reports dashboard summary | `Reports.View` |

---

## 🔐 Security Architecture

### Authentication Flow

```
┌──────────┐     Login      ┌───────────┐    JWT (15 min)    ┌──────────┐
│  Client  │ ──────────────▶│  Auth API │ ─────────────────▶│  Client  │
│          │◀──────────────│           │◀─────────────────│          │
│          │  JWT + Refresh │           │  Refresh (7 days) │          │
└──────────┘                └───────────┘                   └──────────┘
      │                                                          │
      │  Access token expired? → auto-refresh via interceptor     │
      │  Refresh failed?     → redirect to /login                │
      └──────────────────────────────────────────────────────────┘
```

- **Access Token**: JWT, 15-minute expiry, contains `sub` (user ID), `tenant_id`, `role`, `permissions` claims
- **Refresh Token**: Cryptographically random, stored hashed in DB, 7-day expiry, rotation on each use
- **Password Hashing**: ASP.NET Core Identity PBKDF2 (HMAC-SHA256, 256-bit salt, 100,000 iterations)
- **Frontend Interceptors**: Axios request/response interceptors handle automatic token refresh with request queuing (no duplicate refresh calls)

### Multi-Tenancy

```
HTTP Request
    │
    ▼
TenantContextMiddleware  ──▶  Extracts TenantId from JWT claims
    │                          Populates scoped ITenantContext
    ▼
Controller → Handler → Repository
    │
    ▼
Every SQL query automatically filters by @TenantId
(from ITenantContext) — no cross-tenant data leakage
```

- **Strategy**: Single shared database, shared schema, `TenantId` column on every tenant-scoped table
- **Isolation**: `ITenantContext` is scoped per HTTP request, populated by middleware from JWT `tenant_id` claim
- **Enforcement**: All repository queries include `WHERE TenantId = @TenantId` — enforced at the data access layer

---

## ⚙️ Configuration

### Backend (`appsettings.json`)

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=BillingSystem;..."
  },
  "Jwt": {
    "Issuer": "BillingSystem",
    "Audience": "BillingSystemClients",
    "SecretKey": "<at-least-32-chars>",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  },
  "Cache": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "billing"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:8081"]
  }
}
```

> In development, override with `dotnet user-secrets` or `appsettings.Development.json`. In Docker, environment variables use `__` (double underscore) notation (e.g., `Jwt__SecretKey`).

### Frontend (`vite.config.ts`)

The dev server proxies `/api` → `http://localhost:5000`. In production (Docker), nginx proxies `/api` → `http://billing-api:8080`.

### Docker Environment (`.env`)

| Variable | Default | Description |
|----------|---------|-------------|
| `DB_SA_PASSWORD` | `YourStrong!Passw0rd` | SQL Server SA password (min 8 chars, mixed case + symbols) |
| `DB_PORT` | `1433` | Host port for SQL Server |
| `REDIS_PORT` | `6379` | Host port for Redis |
| `JWT_SECRET_KEY` | (see `.env.example`) | JWT signing key (min 32 chars) |
| `API_PORT` | `5000` | Host port for the API |
| `FRONTEND_PORT` | `8081` | Host port for the frontend |

---

## 🧪 Testing

### Backend Tests

```bash
# Run all tests
dotnet test BillingSystem.sln

# Run only unit tests
dotnet test tests/Billing.UnitTests

# Run only integration tests
dotnet test tests/Billing.IntegrationTests

# With coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

### Frontend

```bash
cd frontend
npm run build       # Type-check (tsc) + production build
npm run lint        # ESLint (if configured)
```

---

## 📊 Database Schema

The database contains **29 tables** organized into logical groups:

| Group | Tables |
|-------|--------|
| **Tenancy** | Plans, Tenants, Shops, Settings |
| **Identity** | Users, Roles, UserRoles, Permissions, RolePermissions, RefreshTokens, AuditLogs |
| **Catalog** | Categories, Brands, Units, Products, Inventory, StockMovements |
| **Transactions** | Sales, SaleItems, Payments, Purchases, PurchaseItems |
| **CRM** | Customers, Suppliers |
| **System** | Coupons, Expenses |

Every tenant-scoped table includes:
- `TenantId` (FK → Tenants) — for multi-tenant isolation
- `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` — audit columns
- Appropriate indexes on `TenantId`, foreign keys, and frequently queried columns

See [`database/README.md`](database/README.md) for the full schema diagram and seed data details.

---

## 🐳 Docker Architecture

```
                    ┌─────────────────────────────────────────┐
                    │           docker-compose                 │
                    │                                          │
   :8081 ──────────▶│  ┌──────────┐    /api    ┌──────────┐   │
   (Browser)         │  │ frontend │ ────────▶│   api    │   │
                    │  │ (nginx)  │           │ (.NET 9) │   │
                    │  └──────────┘           └────┬─────┘   │
                    │                               │         │
                    │                    ┌──────────┴──┐      │
                    │                    ▼             ▼      │
                    │              ┌─────────┐  ┌────────┐    │
                    │              │   db    │  │ redis  │    │
                    │              │(mssql)  │  │(cache) │    │
                    │              └─────────┘  └────────┘    │
                    └─────────────────────────────────────────┘
```

| Container | Image | Port | Purpose |
|-----------|-------|------|---------|
| `billing-db` | `mssql/server:2022-latest` | 1433 | SQL Server database |
| `billing-redis` | `redis:7-alpine` | 6379 | Distributed cache |
| `billing-api` | Custom (multi-stage .NET 9) | 5000→8080 | Backend API |
| `billing-frontend` | Custom (node→nginx) | 8081→80 | React SPA |

---

## 🔧 Build & Deploy

### Build Backend

```bash
dotnet build BillingSystem.sln -c Release
dotnet publish src/Billing.API -c Release -o ./publish
```

### Build Frontend

```bash
cd frontend
npm run build       # Output: frontend/dist/
```

### Docker Build (individual)

```bash
docker build -f src/Billing.API/Dockerfile -t billing-api .
docker build -f frontend/Dockerfile -t billing-frontend .
```

---

## 📝 License

This project is proprietary. All rights reserved.

---

## 🤝 Contributing

1. Follow the Clean Architecture dependency rules (no inward dependencies)
2. Use CQRS pattern — commands mutate, queries read
3. All SQL must be parameterized (Dapper `DynamicParameters`)
4. Every tenant-scoped query must filter by `TenantId`
5. Validate requests with FluentValidation
6. Map entities to DTOs with AutoMapper (never expose entities directly)
7. Return `Result<T>` from all application operations
#   N e w S h o p  
 #   N e w S h o p  
 