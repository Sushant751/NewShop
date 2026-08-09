# 🚀 100% Free Production Deployment Guide

This guide walks you through deploying the **NewShop Multi-Tenant ERP & POS System** completely for **FREE**.

---

## 🏗️ Architecture Overview

| Component | Technology | Free Host | Deployment Time |
|---|---|---|---|
| **Frontend** | React 18 + Vite + MUI + Redux | **Vercel** / **Netlify** | ~1 min |
| **Backend API** | ASP.NET Core (.NET 9) Web API | **Render.com** (Docker) | ~3 mins |
| **Database** | MS SQL Server (Dapper/T-SQL) | **Azure SQL (Free)** / **MonsterASP** / **Somee** | ~2 mins |

---

## Step 1: Set up the Free Database (MS SQL Server)

### Option A: Azure SQL Database (Recommended - Lifetime Free)
1. Sign up on [Azure Free Account](https://azure.microsoft.com/free/).
2. In Azure Portal ➔ Create **SQL Database** ➔ Select **Free Tier** (100,000 vCore seconds/month, 32 GB free).
3. Under **Networking**, enable **"Allow Azure services and resources to access this server"** and add your IP.
4. Open the **Query editor** in Azure Portal (or Azure Data Studio / SSMS) and run the scripts from the `/database` folder in order:
   - `database/01_Schema.sql`
   - `database/02_StoredProcedures.sql`
   - `database/03_SeedData.sql`
5. Note your Connection String:
   ```
   Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=BillingSystem;User ID=YOUR_ADMIN;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;
   ```

### Option B: MonsterASP.net (No Credit Card Required)
1. Go to [MonsterASP.net](https://www.monsterasp.net) and register a free account.
2. Go to **MS SQL Databases** ➔ Create a new Database named `BillingSystem`.
3. Open Web SSMS or Connect SSMS ➔ Run the 3 SQL scripts in `database/`.
4. Copy the provided connection string.

---

## Step 2: Deploy Backend API on Render.com (Free)

1. Go to [render.com](https://render.com) and log in with your GitHub account (`Sushant751`).
2. Click **New +** ➔ **Web Service**.
3. Select your repository: **`Sushant751/NewShop`**.
4. Configure the service:
   - **Name**: `newshop-api`
   - **Region**: `Singapore` (or nearest to you)
   - **Environment**: `Docker`
   - **Branch**: `main`
   - **Instance Type**: `Free`
5. Under **Environment Variables**, add:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = *(Your Database connection string from Step 1)*
   - `JwtSettings__SecretKey` = `YourSuperSecretKeyMustBe32CharsLong123!`
   - `JwtSettings__Issuer` = `BillingApi`
   - `JwtSettings__Audience` = `BillingSpa`
6. Click **Create Web Service**.
7. Once deployed, copy your API URL (e.g. `https://newshop-api.onrender.com`).

---

## Step 3: Deploy Frontend on Vercel (Free)

1. Go to [vercel.com](https://vercel.com) and log in with GitHub.
2. Click **Add New...** ➔ **Project**.
3. Import **`Sushant751/NewShop`**.
4. Configure the project:
   - **Root Directory**: Click *Edit* and select **`frontend`**.
   - **Framework Preset**: `Vite` (auto-detected).
5. Open **Environment Variables** and add:
   - `VITE_API_URL` = `https://newshop-api.onrender.com` *(Use your Render API URL from Step 2)*
6. Click **Deploy**.
7. Your app is live with automatic SSL (e.g. `https://newshop-frontend.vercel.app`)!

---

## 🔑 Default Seed Credentials (After Running Seed Script)

| Account | Email | Password | Role |
|---|---|---|---|
| **App Global Admin** | `admin@billingsystem.com` | `Admin@123` | GlobalAdmin (Consolidated views) |
| **Demo Shop Admin** | `shopadmin@demo.com` | `ShopAdmin@123` | ShopAdmin (Store operations) |
| **Store Cashier** | `cashier@demo.com` | `Cashier@123` | Cashier (POS & Sales only) |
