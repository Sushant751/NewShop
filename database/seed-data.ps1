$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5000'
$t = Get-Content "$env:TEMP\bs_token.txt" -Raw
$h = @{ Authorization = "Bearer $t"; 'Content-Type' = 'application/json' }

function Post($path, $body) {
    $json = $body | ConvertTo-Json -Depth 10 -Compress
    try {
        $r = Invoke-RestMethod -Uri "$base$path" -Headers $h -Method Post -Body $json
        return $r
    }
    catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $errBody = $sr.ReadToEnd()
            Write-Host "  ERROR $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "  BODY: $errBody" -ForegroundColor Red
        }
        else {
            Write-Host "  ERROR $($_.Exception.Message)" -ForegroundColor Red
        }
        return $null
    }
}

# ---- Fetch existing products & customers & suppliers ----
$prodRes = Invoke-RestMethod -Uri "$base/api/products?pageSize=100" -Headers $h
$products = $prodRes.data.Items
Write-Host "Existing products: $($products.Count)"
$prodRes.data.Items | ForEach-Object { Write-Host "  - $($_.Id) | $($_.Name) | stock=$($_.CurrentStock) | price=$($_.SellingPrice)" }

$custRes = Invoke-RestMethod -Uri "$base/api/customers?pageSize=100" -Headers $h
$customers = $custRes.data.Items
Write-Host "Existing customers: $($customers.Count)"

$supRes = Invoke-RestMethod -Uri "$base/api/suppliers?pageSize=100" -Headers $h
$suppliers = $supRes.data.Items
Write-Host "Existing suppliers: $($suppliers.Count)"

# ---- Create extra customers ----
Write-Host "`n=== Creating customers ===" -ForegroundColor Cyan
$newCustomers = @(
    @{ Name = 'Rahul Sharma'; Email = 'rahul@example.com'; Phone = '9876543210'; Address = 'MG Road'; City = 'Bengaluru'; State = 'Karnataka'; Country = 'India'; PostalCode = '560001'; TaxNumber = $null; OpeningBalance = 0; CreditLimit = 50000 },
    @{ Name = 'Priya Patel'; Email = 'priya@example.com'; Phone = '9876543211'; Address = 'Satellite Road'; City = 'Ahmedabad'; State = 'Gujarat'; Country = 'India'; PostalCode = '380015'; TaxNumber = $null; OpeningBalance = 0; CreditLimit = 25000 },
    @{ Name = 'Amit Kumar'; Email = 'amit@example.com'; Phone = '9876543212'; Address = 'Park Street'; City = 'Kolkata'; State = 'West Bengal'; Country = 'India'; PostalCode = '700016'; TaxNumber = $null; OpeningBalance = 1000; CreditLimit = 10000 }
)
foreach ($c in $newCustomers) {
    $r = Post '/api/customers' $c
    if ($r -and $r.success) { Write-Host "  OK customer: $($r.data.Name) (id=$($r.data.Id))"; $customers += $r.data }
}

# ---- Create extra suppliers ----
Write-Host "`n=== Creating suppliers ===" -ForegroundColor Cyan
$newSuppliers = @(
    @{ Name = 'Global Distributors'; ContactPerson = 'Vikram Singh'; Email = 'sales@globaldist.com'; Phone = '9000011122'; Address = 'Industrial Area'; City = 'Pune'; State = 'Maharashtra'; Country = 'India'; PostalCode = '411001'; TaxNumber = $null; OpeningBalance = 0 },
    @{ Name = 'TechSource Supplies'; ContactPerson = 'Neha Gupta'; Email = 'orders@techsource.com'; Phone = '9000011133'; Address = 'Cyber City'; City = 'Gurugram'; State = 'Haryana'; Country = 'India'; PostalCode = '122002'; TaxNumber = $null; OpeningBalance = 0 }
)
foreach ($s in $newSuppliers) {
    $r = Post '/api/suppliers' $s
    if ($r -and $r.success) { Write-Host "  OK supplier: $($r.data.Name) (id=$($r.data.Id))"; $suppliers += $r.data }
}

# ---- Create a purchase order (adds stock) ----
Write-Host "`n=== Creating purchase order ===" -ForegroundColor Cyan
if ($products.Count -ge 2 -and $suppliers.Count -ge 1) {
    $supplierId = $suppliers[0].Id
    $items = @(
        @{ ProductId = $products[0].Id; Quantity = 50; UnitCost = $products[0].CostPrice; TaxRate = 18 },
        @{ ProductId = $products[1].Id; Quantity = 30; UnitCost = $products[1].CostPrice; TaxRate = 18 }
    )
    $purchaseBody = @{ SupplierId = $supplierId; ShopId = $null; Items = $items; DiscountAmount = 0; PaidAmount = ($items[0].Quantity * $items[0].UnitCost + $items[1].Quantity * $items[1].UnitCost); Notes = 'Seed purchase order' }
    $r = Post '/api/purchases' $purchaseBody
    if ($r -and $r.success) { Write-Host "  OK purchase: $($r.data.PurchaseNumber) grandTotal=$($r.data.GrandTotal)" }
}

# ---- Create sales (POS checkout) ----
Write-Host "`n=== Creating sales ===" -ForegroundColor Cyan
if ($products.Count -ge 2 -and $customers.Count -ge 1) {
    for ($i = 0; $i -lt 3 -and $i -lt $customers.Count; $i++) {
        $cust = $customers[$i]
        $p1 = $products[0]
        $p2 = $products[$($products.Count - 1)]
        $items = @(
            @{ ProductId = $p1.Id; Quantity = 2; UnitPrice = $p1.SellingPrice; DiscountAmount = 0 },
            @{ ProductId = $p2.Id; Quantity = 1; UnitPrice = $p2.SellingPrice; DiscountAmount = 0 }
        )
        $sub = 2 * $p1.SellingPrice + 1 * $p2.SellingPrice
        $payments = @(
            @{ Method = 1; Amount = $sub; Reference = $null; Notes = $null }
        )
        $saleBody = @{ CustomerId = $cust.Id; ShopId = $null; Items = $items; Payments = $payments; DiscountAmount = 0; Notes = "Seed sale #$($i+1)"; CouponCode = $null }
        $r = Post '/api/sales' $saleBody
        if ($r -and $r.success) { Write-Host "  OK sale: $($r.data.InvoiceNumber) grandTotal=$($r.data.GrandTotal) status=$($r.data.Status)" }
    }
}

Write-Host "`n=== Seeding complete ===" -ForegroundColor Green
