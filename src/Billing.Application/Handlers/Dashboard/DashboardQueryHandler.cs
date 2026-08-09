using AutoMapper;
using Billing.Application.Abstractions;
using Billing.Application.Commands.Sales;
using Billing.Application.Common;
using Billing.Application.DTOs;
using Billing.Persistence.Repositories;
using Billing.Persistence.TenantContext;
using Billing.Shared.Constants;
using Billing.Shared.Results;
using MediatR;

namespace Billing.Application.Handlers.Dashboard;

public sealed class GetDashboardHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetDashboardHandler(IReportRepository reportRepository, ICacheService cache, ITenantContext tenantContext)
    {
        _reportRepository = reportRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<DashboardDto>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var to = request.To.HasValue ? request.To.Value.Date.AddDays(1) : DateTime.UtcNow;
        var from = request.From?.Date ?? to.Date.AddDays(-30);

        var prefix = request.IsGlobalAdmin ? "dashboard:global" : CacheKeys.Dashboard(_tenantContext.TenantId!.Value);
        var cacheKey = $"{prefix}:{from:yyyyMMdd}:{to:yyyyMMdd}";
        var cached = await _cache.GetAsync<DashboardDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<DashboardDto>.Ok(cached);

        var summary = await _reportRepository.GetDashboardSummaryAsync(from, to, request.IsGlobalAdmin, cancellationToken);
        var topProducts = await _reportRepository.GetTopProductsAsync(from, to, 10, request.IsGlobalAdmin, cancellationToken);
        var dailySales = await _reportRepository.GetDailySalesAsync(from, to, request.IsGlobalAdmin, cancellationToken);

        IReadOnlyList<ShopMetricsDto>? shopMetrics = null;
        if (request.IsGlobalAdmin)
        {
            var shopRows = await _reportRepository.GetShopMetricsAsync(from, to, cancellationToken);
            shopMetrics = shopRows.Select(s => new ShopMetricsDto(
                s.TenantId, s.TenantName, s.TenantSlug, s.Plan, s.Status,
                s.UserCount, s.ProductCount, s.TotalBillsGenerated, s.PaidBillsCount,
                s.CancelledBillsCount, s.TotalRevenue, s.CancelledAmount,
                s.OutstandingAmount, s.CreatedDate)).ToList();
        }

        var dto = new DashboardDto(
            summary.TotalSales, summary.TotalPurchases, summary.TotalExpenses, summary.TotalProfit,
            summary.SalesCount, summary.ProductCount, summary.CustomerCount, summary.LowStockCount,
            topProducts.Select(t => new TopProductDto(t.ProductId, t.ProductName, t.QuantitySold, t.Revenue)).ToList(),
            dailySales.Select(d => new DailySalesDto(d.Date, d.TotalSales, d.SalesCount)).ToList(),
            summary.TotalShopsCount,
            summary.TotalUsersCount,
            summary.TotalCancelledBillsCount,
            summary.TotalCancelledAmount,
            shopMetrics);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<DashboardDto>.Ok(dto);
    }
}
