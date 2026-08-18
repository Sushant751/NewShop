using Billing.Application.Abstractions;
using Billing.Application.Commands.Reports;
using Billing.Application.DTOs;
using Billing.Domain.Entities;
using Billing.Persistence.Repositories;
using Billing.Persistence.TenantContext;
using Billing.Shared.Constants;
using Billing.Shared.Enums;
using Billing.Shared.Results;
using MediatR;

namespace Billing.Application.Handlers.Reports;


public sealed class GetProfitLossReportHandler : IRequestHandler<GetProfitLossReportQuery, Result<ProfitLossDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetProfitLossReportHandler(IReportRepository reportRepository, ICacheService cache, ITenantContext tenantContext)
    {
        _reportRepository = reportRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProfitLossDto>> Handle(GetProfitLossReportQuery request, CancellationToken cancellationToken)
    {
        var to = request.To.HasValue ? request.To.Value.Date.AddDays(1) : DateTime.UtcNow;
        var from = request.From?.Date ?? to.Date.AddDays(-30);

        var prefix = request.IsGlobalAdmin ? "reports:global" : CacheKeys.Dashboard(_tenantContext.TenantId!.Value);
        var cacheKey = $"{prefix}:pl:{from:yyyyMMdd}:{to:yyyyMMdd}";
        var cached = await _cache.GetAsync<ProfitLossDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<ProfitLossDto>.Ok(cached);

        var row = await _reportRepository.GetProfitLossAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var dto = new ProfitLossDto(row.Revenue, row.CostOfGoods, row.Expenses, row.DiscountAmount, row.GrossProfit, row.NetProfit);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<ProfitLossDto>.Ok(dto);
    }
}

public sealed class GetSalesReportHandler : IRequestHandler<GetSalesReportQuery, Result<SalesReportSummaryDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetSalesReportHandler(IReportRepository reportRepository, ICacheService cache, ITenantContext tenantContext)
    {
        _reportRepository = reportRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<SalesReportSummaryDto>> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        var to = request.To.HasValue ? request.To.Value.Date.AddDays(1) : DateTime.UtcNow;
        var from = request.From?.Date ?? to.Date.AddDays(-30);

        var prefix = request.IsGlobalAdmin ? "reports:global" : CacheKeys.Dashboard(_tenantContext.TenantId!.Value);
        var cacheKey = $"{prefix}:sales:{from:yyyyMMdd}:{to:yyyyMMdd}";
        var cached = await _cache.GetAsync<SalesReportSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<SalesReportSummaryDto>.Ok(cached);

        var rows = await _reportRepository.GetSalesReportAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var sales = rows
            .Select(r => new SalesReportDto(r.SaleDate, r.InvoiceNumber, r.CustomerName, r.SubTotal, r.DiscountAmount, r.TaxAmount, r.GrandTotal, Enum.TryParse<SaleStatus>(r.Status, out var s) ? s.ToString() : r.Status, Enum.TryParse<PaymentStatus>(r.PaymentStatus, out var p) ? p.ToString() : r.PaymentStatus))
            .ToList();

        var dto = new SalesReportSummaryDto(
            sales,
            sales.Sum(s => s.SubTotal),
            sales.Sum(s => s.DiscountAmount),
            sales.Sum(s => s.TaxAmount),
            sales.Sum(s => s.GrandTotal),
            sales.Count);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<SalesReportSummaryDto>.Ok(dto);
    }
}

public sealed class GetGstReportHandler : IRequestHandler<GetGstReportQuery, Result<GstReportDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetGstReportHandler(IReportRepository reportRepository, ICacheService cache, ITenantContext tenantContext)
    {
        _reportRepository = reportRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<GstReportDto>> Handle(GetGstReportQuery request, CancellationToken cancellationToken)
    {
        var to = request.To.HasValue ? request.To.Value.Date.AddDays(1) : DateTime.UtcNow;
        var from = request.From?.Date ?? to.Date.AddDays(-30);

        var prefix = request.IsGlobalAdmin ? "reports:global" : CacheKeys.Dashboard(_tenantContext.TenantId!.Value);
        var cacheKey = $"{prefix}:gst:{from:yyyyMMdd}:{to:yyyyMMdd}";
        var cached = await _cache.GetAsync<GstReportDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<GstReportDto>.Ok(cached);

        var rows = await _reportRepository.GetGstReportAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var breakdown = rows
            .Select(r => new GstRateBreakdownDto(r.TaxRate, r.TaxableAmount, r.TaxAmount, r.InvoiceCount))
            .ToList();

        var dto = new GstReportDto(
            breakdown,
            breakdown.Sum(b => b.TaxableAmount),
            breakdown.Sum(b => b.TaxAmount),
            breakdown.Sum(b => b.InvoiceCount));

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<GstReportDto>.Ok(dto);
    }
}

public sealed class GetPaymentSummaryReportHandler : IRequestHandler<GetPaymentSummaryReportQuery, Result<PaymentSummaryDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetPaymentSummaryReportHandler(IReportRepository reportRepository, ICacheService cache, ITenantContext tenantContext)
    {
        _reportRepository = reportRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaymentSummaryDto>> Handle(GetPaymentSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var to = request.To.HasValue ? request.To.Value.Date.AddDays(1) : DateTime.UtcNow;
        var from = request.From?.Date ?? to.Date.AddDays(-30);

        var prefix = request.IsGlobalAdmin ? "reports:global" : CacheKeys.Dashboard(_tenantContext.TenantId!.Value);
        var cacheKey = $"{prefix}:pay:{from:yyyyMMdd}:{to:yyyyMMdd}";
        var cached = await _cache.GetAsync<PaymentSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<PaymentSummaryDto>.Ok(cached);

        var rows = await _reportRepository.GetPaymentMethodSummaryAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var methods = rows
            .Select(r => new PaymentMethodSummaryDto(int.TryParse(r.PaymentMethod, out var p) ? ((PaymentMethod)p).ToString() : r.PaymentMethod, r.TotalAmount, r.TransactionCount))
            .ToList();

        var dto = new PaymentSummaryDto(
            methods,
            methods.Sum(m => m.TotalAmount),
            methods.Sum(m => m.TransactionCount));

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<PaymentSummaryDto>.Ok(dto);
    }
}

public sealed class GetInventoryValuationReportHandler : IRequestHandler<GetInventoryValuationReportQuery, Result<InventoryValuationSummaryDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetInventoryValuationReportHandler(IReportRepository reportRepository, ICacheService cache, ITenantContext tenantContext)
    {
        _reportRepository = reportRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<InventoryValuationSummaryDto>> Handle(GetInventoryValuationReportQuery request, CancellationToken cancellationToken)
    {
        var prefix = request.IsGlobalAdmin ? "reports:global" : CacheKeys.Dashboard(_tenantContext.TenantId!.Value);
        var cacheKey = $"{prefix}:invval";
        var cached = await _cache.GetAsync<InventoryValuationSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<InventoryValuationSummaryDto>.Ok(cached);

        var rows = await _reportRepository.GetInventoryValuationAsync(request.IsGlobalAdmin, cancellationToken);
        var items = rows
            .Select(r => new InventoryValuationDto(r.ProductId, r.ProductName, r.Sku, r.CurrentStock, r.CostPrice, r.StockValue))
            .ToList();

        var dto = new InventoryValuationSummaryDto(
            items,
            items.Sum(i => i.StockValue),
            items.Count);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), cancellationToken);
        return Result<InventoryValuationSummaryDto>.Ok(dto);
    }
}

public sealed class GetTopProductsReportHandler : IRequestHandler<GetTopProductsReportQuery, Result<IReadOnlyList<TopProductDto>>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetTopProductsReportHandler(IReportRepository reportRepository, ICacheService cache, ITenantContext tenantContext)
    {
        _reportRepository = reportRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<TopProductDto>>> Handle(GetTopProductsReportQuery request, CancellationToken cancellationToken)
    {
        var to = request.To.HasValue ? request.To.Value.Date.AddDays(1) : DateTime.UtcNow;
        var from = request.From?.Date ?? to.Date.AddDays(-30);

        var prefix = request.IsGlobalAdmin ? "reports:global" : CacheKeys.Dashboard(_tenantContext.TenantId!.Value);
        var cacheKey = $"{prefix}:top:{from:yyyyMMdd}:{to:yyyyMMdd}:{request.Top}";
        var cached = await _cache.GetAsync<IReadOnlyList<TopProductDto>>(cacheKey, cancellationToken);
        if (cached is not null) return Result<IReadOnlyList<TopProductDto>>.Ok(cached);

        var rows = await _reportRepository.GetTopProductsAsync(from, to, request.Top, request.IsGlobalAdmin, cancellationToken);
        var dto = rows
            .Select(r => new TopProductDto(r.ProductId, r.ProductName, r.QuantitySold, r.Revenue))
            .ToList();

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<IReadOnlyList<TopProductDto>>.Ok(dto);
    }
}

public sealed class GetReportsDashboardHandler : IRequestHandler<GetReportsDashboardQuery, Result<ReportsDashboardDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ITenantContext _tenantContext;

    public GetReportsDashboardHandler(IReportRepository reportRepository, ITenantContext tenantContext)
    {
        _reportRepository = reportRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ReportsDashboardDto>> Handle(GetReportsDashboardQuery request, CancellationToken cancellationToken)
    {
        var to = request.To.HasValue ? request.To.Value.Date.AddDays(1) : DateTime.UtcNow;
        var from = request.From?.Date ?? to.Date.AddDays(-30);

        var plRow = await _reportRepository.GetProfitLossAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var profitLoss = new ProfitLossDto(plRow.Revenue, plRow.CostOfGoods, plRow.Expenses, plRow.DiscountAmount, plRow.GrossProfit, plRow.NetProfit);

        var salesRows = await _reportRepository.GetSalesReportAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var sales = salesRows
            .Select(r => new SalesReportDto(r.SaleDate, r.InvoiceNumber, r.CustomerName, r.SubTotal, r.DiscountAmount, r.TaxAmount, r.GrandTotal, Enum.TryParse<SaleStatus>(r.Status, out var s) ? s.ToString() : r.Status, Enum.TryParse<PaymentStatus>(r.PaymentStatus, out var p) ? p.ToString() : r.PaymentStatus))
            .ToList();
        var salesSummary = new SalesReportSummaryDto(
            sales, sales.Sum(s => s.SubTotal), sales.Sum(s => s.DiscountAmount), sales.Sum(s => s.TaxAmount), sales.Sum(s => s.GrandTotal), sales.Count);

        var payRows = await _reportRepository.GetPaymentMethodSummaryAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var methods = payRows.Select(r => new PaymentMethodSummaryDto(Enum.TryParse<PaymentMethod>(r.PaymentMethod, out var p) ? p.ToString() : r.PaymentMethod, r.TotalAmount, r.TransactionCount)).ToList();
        var paymentSummary = new PaymentSummaryDto(methods, methods.Sum(m => m.TotalAmount), methods.Sum(m => m.TransactionCount));

        var gstRows = await _reportRepository.GetGstReportAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var breakdown = gstRows.Select(r => new GstRateBreakdownDto(r.TaxRate, r.TaxableAmount, r.TaxAmount, r.InvoiceCount)).ToList();
        var gstReport = new GstReportDto(breakdown, breakdown.Sum(b => b.TaxableAmount), breakdown.Sum(b => b.TaxAmount), breakdown.Sum(b => b.InvoiceCount));

        var invRows = await _reportRepository.GetInventoryValuationAsync(request.IsGlobalAdmin, cancellationToken);
        var invItems = invRows.Select(r => new InventoryValuationDto(r.ProductId, r.ProductName, r.Sku, r.CurrentStock, r.CostPrice, r.StockValue)).ToList();
        var inventoryValuation = new InventoryValuationSummaryDto(invItems, invItems.Sum(i => i.StockValue), invItems.Count);

        var dto = new ReportsDashboardDto(profitLoss, salesSummary, paymentSummary, gstReport, inventoryValuation);
        return Result<ReportsDashboardDto>.Ok(dto);
    }
}
