# 🚀 Complete Deployment Guide: Microsoft Azure & On-Premises Local Server

This guide provides step-by-step instructions with exact configuration templates and credential settings for deploying the **NewShop Multi-Tenant ERP & POS System** to:
1. ☁️ **Microsoft Azure Cloud** (Azure SQL + Azure App Service + Azure Static Web Apps)
2. 🖥️ **On-Premises / Local Server** (Windows Server with IIS / Linux Server with Docker & LAN POS)

---

# Part 1: Microsoft Azure Cloud Deployment

```
                          ┌───────────────────────────┐
                          │         End User          │
                          └─────────────┬─────────────┘
                                        │
                                        ▼
                      ┌───────────────────────────────────┐
                      │     Azure Static Web Apps (SPA)   │
                      │   React + Vite (Frontend Bundle)  │
                      └─────────────────┬─────────────────┘
                                        │ HTTPS / JWT
                                        ▼
                      ┌───────────────────────────────────┐
                      │    Azure App Service (Linux B1/F1)│
                      │       .NET 9 Web API Container    │
                      └─────────────────┬─────────────────┘
                                        │ Encrypted SQL (Port 1433)
                                        ▼
                      ┌───────────────────────────────────┐
                      │        Azure SQL Database         │
                      │  (Serverless / Lifetime Free Tier)│
                      └───────────────────────────────────┘
```

---

### Step 1.1: Provision Azure SQL Database

1. Sign in to [Azure Portal](https://portal.azure.com).
2. Click **Create a Resource** ➔ Search for **SQL Database** ➔ Click **Create**.
3. Fill in the Database details:
   - **Subscription**: Your Azure subscription
   - **Resource Group**: `rg-newshop-prod`
   - **Database Name**: `BillingSystem`
   - **Server**: Click *Create new* ➔ Server name: `sql-newshop-prod` (e.g. `sql-newshop-prod.database.windows.net`)
   - **Authentication**: Use SQL Authentication
   - **Server Admin Login**: `dbadmin`
   - **Password**: `StrongDbPassword@2026!`
   - **Compute + Storage**: Select **Serverless** or **Free Tier** (32 GB free, 100k vCore-seconds).
4. **Networking & Firewall**:
   - Go to **Networking** tab.
   - Set **Connectivity method**: *Public endpoint*.
   - Set **"Allow Azure services and resources to access this server"**: **YES** (Crucial for App Service connection).
   - Add your client IP to access Query Editor.
5. **Run Database Migrations**:
   - In Azure Portal, open your SQL Database ➔ Click **Query editor (preview)**.
   - Log in as `dbadmin`.
   - Run the 3 scripts in order from the `database/` directory:
     1. [`database/01_Schema.sql`](file:///d:/Projects/ERP/NewShop/database/01_Schema.sql)
     2. [`database/02_StoredProcedures.sql`](file:///d:/Projects/ERP/NewShop/database/02_StoredProcedures.sql)
     3. [`database/03_SeedData.sql`](file:///d:/Projects/ERP/NewShop/database/03_SeedData.sql)
6. **Your Azure SQL Connection String**:
   ```text
   Server=tcp:sql-newshop-prod.database.windows.net,1433;Initial Catalog=BillingSystem;Persist Security Info=False;User ID=dbadmin;Password=StrongDbPassword@2026!;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```

---

### Step 1.2: Deploy Backend API on Azure App Service

1. In Azure Portal ➔ Click **Create a Resource** ➔ **Web App** ➔ Click **Create**.
2. Configure App Service:
   - **Resource Group**: `rg-newshop-prod`
   - **Name**: `newshop-api-prod` (URL will be `https://newshop-api-prod.azurewebsites.net`)
   - **Publish**: `Docker Container` (or `Code` ➔ `.NET 9 (STS)` ➔ `Linux`)
   - **Operating System**: `Linux`
   - **Pricing Plan**: `Free F1` (for test) or `Basic B1` (recommended for production)
3. **Environment & Connection Strings Configuration**:
   - Go to **Settings** ➔ **Environment variables** (or **Configuration**):
   
   | Name | Value | Type |
   |---|---|---|
   | **`ConnectionStrings__DefaultConnection`** | `Server=tcp:sql-newshop-prod.database.windows.net,1433;Initial Catalog=BillingSystem;User ID=dbadmin;Password=StrongDbPassword@2026!;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` | SQLAzure / Custom |
   | **`ASPNETCORE_ENVIRONMENT`** | `Production` | String |
   | **`JwtSettings__SecretKey`** | `YourSuperSecretProductionKeyMustBeAtLeast32Chars123!` | String |
   | **`JwtSettings__Issuer`** | `BillingApi` | String |
   | **`JwtSettings__Audience`** | `BillingSpa` | String |
   | **`JwtSettings__AccessTokenExpirationMinutes`** | `60` | String |
   | **`JwtSettings__RefreshTokenExpirationDays`** | `7` | String |

4. **Deploy via GitHub Actions or VS Code**:
   - In Azure App Service ➔ **Deployment Center** ➔ Select **GitHub** ➔ Repo: `Sushant751/NewShop` ➔ Branch: `main` ➔ Build: `.NET`.
   - Azure will automatically commit a `.github/workflows/azure-api.yml` and deploy the API.

---

### Step 1.3: Deploy Frontend on Azure Static Web Apps

1. In Azure Portal ➔ **Create a Resource** ➔ **Static Web App** ➔ Click **Create**.
2. Configure:
   - **Resource Group**: `rg-newshop-prod`
   - **Name**: `newshop-frontend`
   - **Plan type**: **Free**
   - **Deployment details**: Select **GitHub** ➔ Account: `Sushant751` ➔ Repo: `NewShop` ➔ Branch: `main`.
3. **Build Presets**:
   - **App location**: `frontend`
   - **Api location**: *(leave empty)*
   - **Output location**: `dist`
4. **Environment Variables**:
   - Under **Configuration** ➔ **Application Settings** ➔ Add:
     - `VITE_API_URL`: `https://newshop-api-prod.azurewebsites.net`
5. Click **Review + Create**. Azure will deploy the frontend to a global edge CDN with free auto-renewing SSL!

---

# Part 2: On-Premises / Local Server Deployment (Windows Server or Linux LAN)

For deployment in a local store, retail supermarket, or warehouse on a local server connected via Local Area Network (LAN):

```
                        Local Store Wi-Fi / Ethernet LAN
                                 (192.168.1.0/24)
                                        │
            ┌───────────────────────────┼───────────────────────────┐
            │                           │                           │
            ▼                           ▼                           ▼
   ┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
   │ POS Terminal 1  │         │ POS Terminal 2  │         │ Manager PC / Tab│
   │ (192.168.1.50)  │         │ (192.168.1.51)  │         │ (192.168.1.60)  │
   └────────┬────────┘         └────────┬────────┘         └────────┬────────┘
            │                           │                           │
            └───────────────────────────┼───────────────────────────┘
                                        │ HTTP: http://192.168.1.100
                                        ▼
                      ┌───────────────────────────────────┐
                      │    LOCAL SERVER (192.168.1.100)   │
                      │                                   │
                      │  • IIS / Nginx (Port 80 / 443)    │
                      │  • .NET 9 API (Port 5000)         │
                      │  • SQL Server Express (Port 1433) │
                      │  • Thermal Receipt Printer (USB)  │
                      └───────────────────────────────────┘
```

---

### Step 2.1: Local Database Setup (SQL Server Express)

1. Download and install [SQL Server 2022 Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Free).
2. Install [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup).
3. Open **SQL Server Configuration Manager**:
   - Enable **TCP/IP** protocol under *SQL Server Network Configuration*.
   - Set TCP Port to **1433**.
   - Restart the *SQL Server (SQLEXPRESS)* service.
4. Open SSMS ➔ Connect to `localhost\SQLEXPRESS`:
   - Right-click server ➔ **Properties** ➔ **Security** ➔ Enable **SQL Server and Windows Authentication mode**.
   - Create a SQL login: `billinguser` with password `LocalDbPass@123`.
5. Open query window and run:
   - `01_Schema.sql`, `02_StoredProcedures.sql`, `03_SeedData.sql` from the `database/` folder.
6. **Local Connection String**:
   ```text
   Server=localhost;Database=BillingSystem;User Id=billinguser;Password=LocalDbPass@123;TrustServerCertificate=True;MultipleActiveResultSets=True;
   ```

---

### Step 2.2: Publish & Host Backend API on Local Server

#### Option A: Run as a Windows Service (No IIS needed)

1. Publish the backend API to a folder (e.g. `C:\NewShop\API`):
   ```powershell
   dotnet publish "src\Billing.API\Billing.API.csproj" -c Release -o "C:\NewShop\API"
   ```
2. Create `C:\NewShop\API\appsettings.Production.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=BillingSystem;User Id=billinguser;Password=LocalDbPass@123;TrustServerCertificate=True;MultipleActiveResultSets=True;"
     },
     "JwtSettings": {
       "SecretKey": "LocalSuperSecretKeyMustBe32CharactersLong123!",
       "Issuer": "BillingApi",
       "Audience": "BillingSpa",
       "AccessTokenExpirationMinutes": 60,
       "RefreshTokenExpirationDays": 7
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```
3. Install and start as a Windows background service using PowerShell:
   ```powershell
   # Create Windows Service to auto-start with Windows
   sc.exe create "NewShopAPI" binPath= "C:\NewShop\API\Billing.API.exe --urls=http://0.0.0.0:5000" start= auto
   sc.exe start "NewShopAPI"
   ```

#### Option B: Host in IIS (Internet Information Services)
1. Install **ASP.NET Core Hosting Bundle for .NET 9.0**.
2. Open **IIS Manager** ➔ Create Website ➔ Physical path: `C:\NewShop\API` ➔ Port: `5000`.
3. Set Application Pool to: **No Managed Code**.

---

### Step 2.3: Build & Host Frontend on Local Server

1. Build the production React app with the local server API URL:
   ```powershell
   cd frontend
   $env:VITE_API_URL="http://192.168.1.100:5000"
   npm run build
   ```
2. Copy `frontend/dist` files to `C:\NewShop\Web`.
3. In IIS Manager:
   - Add Website: `NewShopWeb` ➔ Physical path: `C:\NewShop\Web` ➔ Port: `80` (HTTP).
   - Ensure the [IIS URL Rewrite Module](https://www.iis.net/downloads/microsoft/url-rewrite) is installed and add `web.config` inside `C:\NewShop\Web`:
     ```xml
     <?xml version="1.0" encoding="utf-8"?>
     <configuration>
       <system.webServer>
         <rewrite>
           <rules>
             <rule name="SPA Fallback" stopProcessing="true">
               <match url=".*" />
               <conditions logicalGrouping="MatchAll">
                 <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
                 <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
               </conditions>
               <action type="Rewrite" url="/index.html" />
             </rule>
           </rules>
         </rewrite>
       </system.webServer>
     </configuration>
     ```

---

### Step 2.4: Configure Windows Firewall for LAN Access

To allow billing terminals and tablet devices on the store Wi-Fi / LAN to connect to the server:

Open PowerShell as Administrator and run:
```powershell
# Allow Inbound Web traffic (Frontend on Port 80, Backend API on Port 5000)
New-NetFirewallRule -DisplayName "NewShop Frontend (HTTP 80)" -Direction Inbound -LocalPort 80 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "NewShop Backend API (HTTP 5000)" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow

# Allow SQL Server for external reporting if needed (Port 1433)
New-NetFirewallRule -DisplayName "SQL Server (TCP 1433)" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow
```

Now, any tablet, laptop, or POS machine connected to the store Wi-Fi can open:
👉 **`http://192.168.1.100`** to access the POS billing system.

---

# Part 3: Option C – 1-Click All-in-One Local Docker Deployment

If your local server has **Docker Desktop** or Linux Docker installed, you can launch the complete stack (SQL Server + Backend API + Frontend Nginx) with a single command:

Create `docker-compose.local.yml`:
```yaml
version: '3.8'

services:
  # 1. Local MS SQL Server 2022
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: newshop_local_db
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=LocalSaPassword@2026!
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql

  # 2. .NET 9 Backend Web API
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: newshop_local_api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=BillingSystem;User Id=sa;Password=LocalSaPassword@2026!;TrustServerCertificate=True;
      - JwtSettings__SecretKey=LocalDockerSuperSecretKey32Characters123!
      - JwtSettings__Issuer=BillingApi
      - JwtSettings__Audience=BillingSpa
    ports:
      - "5000:8080"
    depends_on:
      - sqlserver

  # 3. Frontend Web App
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: newshop_local_web
    ports:
      - "80:80"
    depends_on:
      - api

volumes:
  sql_data:
```

### Launch with:
```bash
docker compose -f docker-compose.local.yml up -d
```

---

# Part 4: Credentials Reference Sheet

| Environment | Role | Email / User | Password | Access Details |
|---|---|---|---|---|
| **Azure / Cloud** | 👑 Global Admin | `admin@billingsystem.com` | `Admin@123` | `https://<your-app>.azurestaticapps.net` |
| **Azure / Cloud** | 🏪 Demo Shop Admin | `shopadmin@demo.com` | `ShopAdmin@123` | `https://<your-app>.azurestaticapps.net` |
| **Azure / Cloud** | 💳 Store Cashier | `cashier@demo.com` | `Cashier@123` | `https://<your-app>.azurestaticapps.net` |
| **Azure DB** | SQL Server Admin | `dbadmin` | `StrongDbPassword@2026!` | `sql-newshop-prod.database.windows.net` |
| **Local Server** | 👑 Global Admin | `admin@billingsystem.com` | `Admin@123` | `http://192.168.1.100` |
| **Local Server** | 🏪 Store Admin | `shopadmin@demo.com` | `ShopAdmin@123` | `http://192.168.1.100` |
| **Local DB** | SQL Server User | `billinguser` | `LocalDbPass@123` | `localhost,1433` (SQLEXPRESS) |
