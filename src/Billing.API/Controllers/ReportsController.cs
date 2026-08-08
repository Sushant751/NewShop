using Billing.Application.Commands.Reports;
using Billing.Application.DTOs;
using Billing.Identity.Authorization;
using Billing.Shared.Enums;
using Billing.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Billing.API.Controllers;

/// <summary>
/// Reporting endpoints: profit & loss, sales report, GST/tax breakdown,
/// payment method summary, inventory valuation, top products, and a
/// combined reports dashboard. Supports CSV export for tabular reports.
/// </summary>
[Authorize]
public class ReportsController : BaseApiController
{
    [HttpGet("profit-loss")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Get the profit & loss report for a date range.")]
    [ProducesResponseType(typeof(Result<ProfitLossDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfitLoss(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetProfitLossReportQuery(from, to), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("sales")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Get the sales report (line-item list + totals) for a date range.")]
    [ProducesResponseType(typeof(Result<SalesReportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetSalesReportQuery(from, to), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("sales/export")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Export the sales report as a CSV file.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetSalesReportQuery(from, to), cancellationToken);
        if (!result.Success || result.Data is null)
            return ToActionResult(result);

        var csv = BuildSalesCsv(result.Data);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"sales-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("gst")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Get the GST/tax breakdown report for a date range.")]
    [ProducesResponseType(typeof(Result<GstReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGstReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetGstReportQuery(from, to), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("gst/export")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Export the GST report as a CSV file.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportGstReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetGstReportQuery(from, to), cancellationToken);
        if (!result.Success || result.Data is null)
            return ToActionResult(result);

        var csv = BuildGstCsv(result.Data);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"gst-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("payments")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Get the payment method summary report for a date range.")]
    [ProducesResponseType(typeof(Result<PaymentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetPaymentSummaryReportQuery(from, to), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("inventory-valuation")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Get the inventory valuation report (stock-on-hand × cost price).")]
    [ProducesResponseType(typeof(Result<InventoryValuationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryValuation(CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetInventoryValuationReportQuery(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("inventory-valuation/export")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Export the inventory valuation report as a CSV file.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInventoryValuation(CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetInventoryValuationReportQuery(), cancellationToken);
        if (!result.Success || result.Data is null)
            return ToActionResult(result);

        var csv = BuildInventoryValuationCsv(result.Data);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"inventory-valuation-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("top-products")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Get the top-selling products report for a date range.")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<TopProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int top = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetTopProductsReportQuery(from, to, top), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Get the combined reports dashboard (P&L, sales, payments, GST, inventory).")]
    [ProducesResponseType(typeof(Result<ReportsDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportsDashboard(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetReportsDashboardQuery(from, to), cancellationToken);
        return ToActionResult(result);
    }

    // ========================================================================
    // CSV builders
    // ========================================================================

    private static string BuildSalesCsv(SalesReportSummaryDto data)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Date,Invoice Number,Customer,SubTotal,Tax,Grand Total,Status,Payment Status");
        foreach (var s in data.Sales)
        {
            sb.AppendLine(string.Join(',',
                s.SaleDate.ToString("yyyy-MM-dd HH:mm"),
                EscapeCsv(s.InvoiceNumber),
                EscapeCsv(s.CustomerName ?? "Walk-in"),
                s.SubTotal,
                s.TaxAmount,
                s.GrandTotal,
                s.Status,
                s.PaymentStatus));
        }
        sb.AppendLine();
        sb.AppendLine($"Totals,,,,{data.TotalSubTotal},{data.TotalTax},{data.TotalGrandTotal},");
        return sb.ToString();
    }

    private static string BuildGstCsv(GstReportDto data)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Tax Rate (%),Taxable Amount,Tax Amount,Invoice Count");
        foreach (var r in data.RateBreakdown)
        {
            sb.AppendLine(string.Join(',',
                r.TaxRate,
                r.TaxableAmount,
                r.TaxAmount,
                r.InvoiceCount));
        }
        sb.AppendLine();
        sb.AppendLine($"Totals,{data.TotalTaxableAmount},{data.TotalTaxAmount},{data.TotalInvoices}");
        return sb.ToString();
    }

    private static string BuildInventoryValuationCsv(InventoryValuationSummaryDto data)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Product Name,SKU,Current Stock,Cost Price,Stock Value");
        foreach (var i in data.Items)
        {
            sb.AppendLine(string.Join(',',
                EscapeCsv(i.ProductName),
                EscapeCsv(i.Sku ?? ""),
                i.CurrentStock,
                i.CostPrice,
                i.StockValue));
        }
        sb.AppendLine();
        sb.AppendLine($"Totals,,{data.ProductCount} products,,{data.TotalStockValue}");
        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
