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

        var cacheKey = CacheKeys.Dashboard(_tenantContext.TenantId!.Value);
        var cached = await _cache.GetAsync<DashboardDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<DashboardDto>.Ok(cached);

        var summary = await _reportRepository.GetDashboardSummaryAsync(from, to, cancellationToken);
        var topProducts = await _reportRepository.GetTopProductsAsync(from, to, 10, cancellationToken);
        var dailySales = await _reportRepository.GetDailySalesAsync(from, to, cancellationToken);

        var dto = new DashboardDto(
            summary.TotalSales, summary.TotalPurchases, summary.TotalExpenses, summary.TotalProfit,
            summary.SalesCount, summary.ProductCount, summary.CustomerCount, summary.LowStockCount,
            topProducts.Select(t => new TopProductDto(t.ProductId, t.ProductName, t.QuantitySold, t.Revenue)).ToList(),
            dailySales.Select(d => new DailySalesDto(d.Date, d.TotalSales, d.SalesCount)).ToList());

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<DashboardDto>.Ok(dto);
    }
}
