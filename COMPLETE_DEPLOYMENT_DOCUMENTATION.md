# 📖 NewShop – Complete Production Deployment Documentation

A comprehensive, step-by-step guide explaining **how the system was deployed for 100% FREE** and **how you can redeploy or update it in the future**.

---

## 🏗️ 1. Architecture Overview

```
                          ┌───────────────────────────┐
                          │     User Web Browser      │
                          └─────────────┬─────────────┘
                                        │
                         HTTPS Requests │ (SPA Client Routes)
                                        ▼
                      ┌───────────────────────────────────┐
                      │          VERCEL (Frontend)        │
                      │   React 18 + Vite + MUI + Redux   │
                      │  https://<your-app>.vercel.app    │
                      └─────────────────┬─────────────────┘
                                        │
                      REST API / JWT    │ (VITE_API_URL)
                      Axios Interceptor │
                                        ▼
                      ┌───────────────────────────────────┐
                      │          RENDER (Backend)         │
                      │     ASP.NET Core (.NET 9.0)       │
                      │  https://newshop-api.onrender.com │
                      └─────────────────┬─────────────────┘
                                        │
                      T-SQL / Dapper    │ Connection String (Port 1433)
                      Encrypted SSL     │
                                        ▼
                      ┌───────────────────────────────────┐
                      │      CLOUD MS SQL SERVER (DB)     │
                      │   db63059.public.databaseasp.net  │
                      │  (29 Tables, Views & Stored Procs)│
                      └───────────────────────────────────┘
```

---

## 📋 2. How the System Was Deployed (Step-by-Step Record)

### Step 2.1: Git Repository Preparation
1. **Created Root `.gitignore`**: Excluded `bin/`, `obj/`, `node_modules/`, `dist/`, `.vs/`, and log files to keep the repository lightweight and secure.
2. **Created GitHub Repository**: Initialized remote repository [**github.com/Sushant751/NewShop**](https://github.com/Sushant751/NewShop) and pushed all code to branch `main`.

---

### Step 2.2: Cloud Database Setup & Migration (MS SQL Server)
1. **Cloud Host**: `db63059.public.databaseasp.net`
2. **Database Name**: `db63059`
3. **Automated Migration Executed**:
   - `database/01_Schema.sql`: Created 29 relational tables (`Tenants`, `Users`, `Roles`, `RolePermissions`, `Permissions`, `Products`, `Categories`, `Brands`, `Units`, `Sales`, `SaleItems`, `Purchases`, `PurchaseItems`, `Suppliers`, `Customers`, `Inventory`, `StockMovements`, `Payments`, `Settings`, etc.).
   - `database/02_StoredProcedures.sql`: Created system stored procedures and triggers.
   - `database/03_SeedData.sql`: Seeded default roles, permissions, Demo Shop tenant, and administrative accounts.

---

### Step 2.3: Backend Web API Deployment on Render.com
1. **Multi-Stage Containerization (`Dockerfile`)**:
   - Built on `mcr.microsoft.com/dotnet/sdk:9.0` with Release optimization.
   - Packaged on lightweight `mcr.microsoft.com/dotnet/aspnet:9.0` runtime.
   - Configured `ASPNETCORE_URLS=http://+:8080`.
2. **Deployed Web Service on Render**:
   - **Environment**: Docker (Free Plan)
   - **Region**: Singapore
   - **Branch**: `main`
3. **Configured Environment Variables on Render**:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = `Server=db63059.public.databaseasp.net; Database=db63059; User Id=db63059; Password=Y-m5a9=M+cC2; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;`
   - `JwtSettings__SecretKey` = `YourSuperSecretProductionKeyMustBeAtLeast32Chars123!`
   - `JwtSettings__Issuer` = `BillingApi`
   - `JwtSettings__Audience` = `BillingSpa`
4. **Live API URL**: `https://newshop-api.onrender.com`

---

### Step 2.4: Frontend SPA Deployment on Vercel
1. **Configured Dynamic API Base URL (`client.ts`)**:
   - Updated `apiClient` to read `import.meta.env.VITE_API_URL` dynamically in production.
2. **Added Vite Environment Types (`frontend/src/vite-env.d.ts`)**:
   - Added `/// <reference types="vite/client" />` and `ImportMetaEnv` interface.
3. **Configured SPA Client-Side Rewrites**:
   - Added `frontend/vercel.json` and `public/_redirects` to route all page requests (`/dashboard`, `/pos`, etc.) to `/index.html`.
4. **Configured Root Monorepo Build (`package.json` & `vercel.json`)**:
   - Configured direct Vite build via Node: `"build": "cd frontend && npm install && node ./node_modules/vite/bin/vite.js build && cd .. && node -e \"const fs=require('fs'); fs.cpSync('frontend/dist', 'dist', {recursive:true}); fs.cpSync('frontend/dist', 'public', {recursive:true});\""`.
   - Set `"outputDirectory": "public"`.
5. **Configured Environment Variable on Vercel**:
   - `VITE_API_URL` = `https://newshop-api.onrender.com`
6. **Live App URL**: Active on Vercel (e.g. `https://<your-project>.vercel.app`).

---

## 🔑 3. Default Seed Credentials

| Role | Email | Password | Access Scope |
|---|---|---|---|
| 👑 **Global Administrator** | `admin@billingsystem.com` | `Admin@123` | Platform Owner – "App Admin" badge, consolidated multi-shop analytics, manage users across all shops |
| 🏪 **Demo Shop Admin** | `shopadmin@demo.com` | `ShopAdmin@123` | Store Operations – Full POS, Products, Purchases, Sales, Reports |
| 💳 **Cashier** | `cashier@demo.com` | `Cashier@123` | Checkout Counter – POS & Sales Receipt creation |

---

## 🔄 4. How to Update / Redeploy in the Future

Whenever you make code changes or add new features:

### Step 1: Make Code Changes Locally
Make your edits in `frontend/` or `src/`.

### Step 2: Test Locally
- Run backend: `dotnet run --project src\Billing.API\Billing.API.csproj`
- Run frontend: `cd frontend && npm run dev`

### Step 3: Push to GitHub
```bash
git add .
git commit -m "feat: your new feature description"
git push origin main
```

### Step 4: Automatic CI/CD Deployment
- **Vercel** will automatically detect the new commit on `main`, build, and deploy the frontend in ~20 seconds.
- **Render** will automatically detect the new commit on `main`, build the Docker container, and deploy the backend in ~2 minutes.

---

## 🛠️ 5. Troubleshooting & Common Issues

| Issue | Cause | Solution |
|---|---|---|
| **Vercel 404 NOT_FOUND** | Missing SPA rewrites or wrong output dir | Verify `vercel.json` has `"outputDirectory": "public"` and rewrites pointing to `/index.html`. |
| **CORS Errors in Browser** | Backend blocking frontend domain | [`Program.cs`](file:///d:/Projects/ERP/NewShop/src/Billing.API/Program.cs#L97) has `SetIsOriginAllowed(_ => true)` to allow any Vercel domain. |
| **Backend 500 on Dashboard** | Database connection or SQL query issue | Verify `ConnectionStrings__DefaultConnection` on Render contains the correct cloud DB credentials. |
| **API Cold Start Delay** | Render Free Web Service sleeps after 15 min of inactivity | On free tier, the first request after idle takes ~30-50s to spin up. Subsequent requests respond in milliseconds. |

---

## 🏷️ 6. Adding a Custom Domain (Optional)

1. **Frontend Domain** (e.g. `app.yourdomain.com`):
   - In Vercel ➔ **Settings** ➔ **Domains** ➔ Add `app.yourdomain.com`.
   - Add the CNAME record in your DNS provider (Cloudflare, GoDaddy, Namecheap).
2. **Backend API Domain** (e.g. `api.yourdomain.com`):
   - In Render ➔ **Settings** ➔ **Custom Domains** ➔ Add `api.yourdomain.com`.
   - Update `VITE_API_URL` in Vercel to `https://api.yourdomain.com`.
