$ErrorActionPreference = 'Stop'
$t = Get-Content "$env:TEMP\bs_token.txt" -Raw
$h = @{ Authorization = "Bearer $t" }

$endpoints = @(
    '/api/dashboard',
    '/api/products',
    '/api/customers',
    '/api/suppliers',
    '/api/purchases',
    '/api/sales',
    '/api/reports/sales',
    '/api/reports/payments',
    '/api/reports/gst',
    '/api/reports/inventory-valuation',
    '/api/reports/top-products',
    '/api/reports/profit-loss?from=2026-01-01&to=2026-12-31',
    '/api/products/low-stock',
    '/api/products/search?q=test'
)

foreach ($e in $endpoints) {
    try {
        $r = Invoke-RestMethod -Uri "http://localhost:5000$e" -Headers $h -Method Get
        $data = $r.data
        if ($null -eq $data) {
            $summary = 'NULL'
        }
        elseif ($null -ne $data.Items) {
            $summary = "Items=$($data.Items.Count); Total=$($data.Total)"
        }
        elseif ($data -is [array]) {
            $summary = "ArrayCount=$($data.Count)"
        }
        else {
            $keys = ($data | Get-Member -MemberType NoteProperty).Name -join ','
            $summary = "Object keys: $keys"
        }
        Write-Host "$e => $summary"
    }
    catch {
        Write-Host "$e => ERROR: $($_.Exception.Message)"
    }
}
