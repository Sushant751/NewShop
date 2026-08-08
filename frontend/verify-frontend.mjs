// Headless browser verification of all frontend pages.
// Run with: node database/verify-frontend.mjs
import { chromium } from 'playwright';

const BASE = 'http://localhost:5173';
const EMAIL = 'admin@billingsystem.com';
const PASSWORD = 'Admin@123';

// Routes to verify (protected pages). Each entry: { path, name, expectText (optional substring) }
const routes = [
    { path: '/dashboard', name: 'Dashboard' },
    { path: '/products', name: 'Products' },
    { path: '/products/new', name: 'ProductForm-New' },
    { path: '/pos', name: 'POS' },
    { path: '/sales', name: 'Sales' },
    { path: '/customers', name: 'Customers' },
    { path: '/suppliers', name: 'Suppliers' },
    { path: '/purchases', name: 'Purchases' },
    { path: '/reports', name: 'Reports' },
    { path: '/settings', name: 'Settings' },
];

const results = [];

async function main() {
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await context.newPage();

    // Collect console errors, uncaught page errors, and failed network requests globally
    const consoleErrors = [];
    const pageErrors = [];
    const networkFails = [];
    page.on('console', (msg) => {
        if (msg.type() === 'error') consoleErrors.push(msg.text());
    });
    page.on('pageerror', (err) => {
        pageErrors.push(`${err.name}: ${err.message}`);
    });
    page.on('requestfailed', (req) => {
        const url = req.url();
        // Ignore dev-server HMR / sourcemap noise
        if (url.includes('/@') || url.includes('.map')) return;
        networkFails.push(`${req.method()} ${url} :: ${req.failure()?.errorText || ''}`);
    });

    // 1. Load login page
    console.log('=== Step 1: Load login page ===');
    await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(800);

    // 2. Fill login form and submit
    console.log('=== Step 2: Login ===');
    await page.fill('input[type="email"]', EMAIL);
    await page.fill('input[type="password"]', PASSWORD);
    // Click the Sign In button (type=submit inside the form)
    await Promise.all([
        page.waitForURL('**/dashboard', { timeout: 15000 }),
        page.click('button[type="submit"]'),
    ]);
    await page.waitForTimeout(1500);
    console.log('  Logged in, now on:', page.url());

    // 3. Visit each route
    for (const r of routes) {
        console.log(`=== Visiting ${r.name} (${r.path}) ===`);
        const beforeErrCount = consoleErrors.length;
        const beforePageErrCount = pageErrors.length;
        const beforeFailCount = networkFails.length;
        try {
            await page.goto(`${BASE}${r.path}`, { waitUntil: 'networkidle', timeout: 20000 });
            await page.waitForTimeout(1200);
            const title = await page.title();
            const bodyText = (await page.textContent('body')) || '';
            const bodyLen = bodyText.length;
            // Detect MUI error alert on page
            const hasAlert = await page.locator('.MuiAlert-message').count();
            const newErrors = consoleErrors.slice(beforeErrCount);
            const newPageErrors = pageErrors.slice(beforePageErrCount);
            const newFails = networkFails.slice(beforeFailCount);
            const status =
                newPageErrors.length > 0 || newFails.length === 0 && !newErrors.some((e) => e.includes('500') || e.includes('401'))
                    ? (newPageErrors.length > 0 ? 'CRASH' : (bodyLen > 50 ? 'OK' : 'EMPTY'))
                    : 'ERROR';
            results.push({
                route: r.path,
                name: r.name,
                status,
                title,
                bodyLen,
                alerts: hasAlert,
                consoleErrors: newErrors,
                pageErrors: newPageErrors,
                networkFails: newFails,
            });
            console.log(`  status=${status} title="${title}" bodyLen=${bodyLen} alerts=${hasAlert} fails=${newFails.length} errs=${newErrors.length} pageErrs=${newPageErrors.length}`);
        } catch (e) {
            results.push({ route: r.path, name: r.name, status: 'NAV_FAIL', error: e.message });
            console.log(`  NAV_FAIL: ${e.message}`);
        }
    }

    // 4. Visit SaleDetail for an existing sale (need a sale id)
    console.log('=== Visiting SaleDetail (first sale) ===');
    const beforePageErrCount = pageErrors.length;
    try {
        // Navigate to sales list and click the first "View" button (DataGrid uses Button, not anchor)
        await page.goto(`${BASE}/sales`, { waitUntil: 'networkidle', timeout: 20000 });
        await page.waitForTimeout(1200);
        const viewBtn = page.locator('button:has-text("View")').first();
        if (await viewBtn.count()) {
            await Promise.all([
                page.waitForURL('**/sales/**', { timeout: 15000 }),
                viewBtn.click(),
            ]);
            await page.waitForTimeout(1200);
            const bodyLen = ((await page.textContent('body')) || '').length;
            const newPageErrors = pageErrors.slice(beforePageErrCount);
            const status = newPageErrors.length > 0 ? 'CRASH' : bodyLen > 50 ? 'OK' : 'EMPTY';
            results.push({ route: page.url().replace(BASE, ''), name: 'SaleDetail', status, bodyLen, pageErrors: newPageErrors });
            console.log(`  SaleDetail status=${status} bodyLen=${bodyLen} pageErrs=${newPageErrors.length}`);
        } else {
            results.push({ route: '/sales/:id', name: 'SaleDetail', status: 'SKIPPED', note: 'No View button found' });
            console.log('  No View button found');
        }
    } catch (e) {
        results.push({ route: '/sales/:id', name: 'SaleDetail', status: 'NAV_FAIL', error: e.message });
        console.log(`  SaleDetail NAV_FAIL: ${e.message}`);
    }

    await browser.close();

    // 5. Summary
    console.log('\n========== FRONTEND VERIFICATION SUMMARY ==========');
    const ok = results.filter((r) => r.status === 'OK');
    const empty = results.filter((r) => r.status === 'EMPTY');
    const err = results.filter((r) => r.status === 'ERROR' || r.status === 'NAV_FAIL');
    const skipped = results.filter((r) => r.status === 'SKIPPED');
    console.log(`Total routes: ${results.length}`);
    console.log(`OK: ${ok.length} | EMPTY: ${empty.length} | ERROR/FAIL: ${err.length} | SKIPPED: ${skipped.length}`);
    console.log('\n--- Per-route detail ---');
    for (const r of results) {
        const flag = r.status === 'OK' ? '[OK]    ' : r.status === 'EMPTY' ? '[EMPTY] ' : r.status === 'CRASH' ? '[CRASH] ' : r.status === 'SKIPPED' ? '[SKIP]  ' : '[ERROR] ';
        console.log(`${flag} ${r.name.padEnd(16)} ${r.route}`);
        if (r.networkFails && r.networkFails.length) {
            for (const f of r.networkFails) console.log(`          NET-FAIL: ${f}`);
        }
        if (r.pageErrors && r.pageErrors.length) {
            for (const e of r.pageErrors) console.log(`          PAGE-ERR: ${e}`);
        }
        if (r.consoleErrors && r.consoleErrors.length) {
            for (const e of r.consoleErrors) console.log(`          CONSOLE-ERR: ${e}`);
        }
        if (r.error) console.log(`          ERROR: ${r.error}`);
    }
    console.log('\n--- All network failures (global) ---');
    for (const f of networkFails) console.log(`  ${f}`);
    console.log('\n--- All uncaught page errors (global) ---');
    for (const e of pageErrors) console.log(`  ${e}`);
    console.log('\n--- All console errors (global) ---');
    for (const e of consoleErrors) console.log(`  ${e}`);
    console.log('==================================================');
}

main().catch((e) => {
    console.error('FATAL:', e);
    process.exit(1);
});
