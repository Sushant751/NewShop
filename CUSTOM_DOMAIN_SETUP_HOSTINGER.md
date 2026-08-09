# 🌐 Custom Domain Setup Guide: `tradoindia.in` (Hostinger ➔ Vercel & Render)

This guide walks you through connecting your purchased Hostinger domain **`tradoindia.in`** to your live **NewShop ERP & POS** deployment.

---

## 🏗️ 1. Target Domain Architecture

| Subdomain / URL | Target Service | Purpose |
| :--- | :--- | :--- |
| **`tradoindia.in`** | **Vercel** (Frontend) | Main Web Application & POS |
| **`www.tradoindia.in`** | **Vercel** (Frontend) | Redirect to `tradoindia.in` |
| **`api.tradoindia.in`** | **Render** (Backend) | ASP.NET Core 9 Web API & Swagger |

---

## 🛠️ Step 1: Add Custom Domain to Vercel (Frontend)

1. Open your [Vercel Dashboard](https://vercel.com/dashboard).
2. Click on your project (**`new-shop`**).
3. Navigate to **Settings** (top tab) ➔ **Domains** (left sidebar).
4. In the text box, enter: **`tradoindia.in`** and click **Add**.
5. Vercel will recommend adding **`www.tradoindia.in`** with redirect. Select **"Redirect to tradoindia.in"** (Recommended).
6. Vercel will display the required DNS records:
   - **Type `A`**: Name `@`, Value `76.76.21.21`
   - **Type `CNAME`**: Name `www`, Value `cname.vercel-dns.com`

---

## 🛠️ Step 2: Add Custom Domain to Render (Backend API)

1. Open your [Render Dashboard](https://dashboard.render.com/).
2. Click on your Web Service (**`newshop-api`**).
3. Navigate to **Settings** (left sidebar) ➔ scroll down to **Custom Domains**.
4. Click **Add Custom Domain**.
5. Enter: **`api.tradoindia.in`** and click **Save**.
6. Render will show the DNS instructions:
   - **Type `CNAME`**: Name `api`, Value `newshop-api.onrender.com`

---

## 🛠️ Step 3: Add DNS Records in Hostinger hPanel

1. Log into your [Hostinger Account](https://hpanel.hostinger.com/).
2. Go to **Domains** ➔ Click **Manage** next to **`tradoindia.in`**.
3. In the left menu, click **DNS / Nameservers** (or **DNS Records**).
4. Add / Update the following **3 DNS Records**:

### 📋 DNS Table to Enter in Hostinger:

| Type | Name / Host | Points to / Value | TTL | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **A** | `@` | `76.76.21.21` | `300` (or Default) | Routes `tradoindia.in` to Vercel |
| **CNAME** | `www` | `cname.vercel-dns.com` | `300` (or Default) | Routes `www.tradoindia.in` to Vercel |
| **CNAME** | `api` | `newshop-api.onrender.com` | `300` (or Default) | Routes `api.tradoindia.in` to Render API |

> [!NOTE]
> If Hostinger already has an existing default **A Record** with Name `@` (such as parking IP `145.14.145.4` or Hostinger preview page), click the **Edit/Delete** button to remove the old IP and replace it with `76.76.21.21`.

---

## 🛠️ Step 4: Update Vercel Environment Variables

Once DNS is configured:

1. In your **Vercel Dashboard** ➔ Go to **Settings** ➔ **Environment Variables**.
2. Find `VITE_API_URL` (or add it if missing):
   - **Key**: `VITE_API_URL`
   - **Value**: `https://api.tradoindia.in`
3. Click **Save**.
4. Go to **Deployments** tab ➔ Click **Redeploy** on the latest production deployment so the frontend builds with the new API domain.

---

## 🔒 Step 5: Automatic SSL Certificates

- **Vercel** will automatically issue a free **Let's Encrypt SSL/TLS Certificate** for `tradoindia.in` and `www.tradoindia.in`.
- **Render** will automatically issue a free **Let's Encrypt SSL/TLS Certificate** for `api.tradoindia.in`.
- DNS propagation typically takes **5 to 15 minutes** (maximum 24-48 hours).

---

## ✅ Verification Checklist

Once DNS propagates:
1. Open [https://tradoindia.in](https://tradoindia.in) in your browser — your POS & ERP Web App should open securely over HTTPS with a valid padlock 🔒.
2. Open [https://api.tradoindia.in/swagger](https://api.tradoindia.in/swagger) — your Swagger Interactive Documentation should open cleanly.
3. Log in with `shopadmin@demo.com` / `ShopAdmin@123` on `https://tradoindia.in` to verify end-to-end operation!
