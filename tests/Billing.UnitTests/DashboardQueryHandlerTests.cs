using Billing.Application.Abstractions;
using Billing.Application.Commands.Sales;
using Billing.Application.DTOs;
using Billing.Application.Handlers.Dashboard;
using Billing.Persistence.Repositories;
using Billing.Persistence.TenantContext;
using FluentAssertions;
using Moq;
using Xunit;

namespace Billing.UnitTests;

public class DashboardQueryHandlerTests
{
    private readonly Mock<IReportRepository> _reportRepo = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<ITenantContext> _tenantContext = new();

    private readonly GetDashboardHandler _handler;
    private static readonly Guid TestTenantId = Guid.NewGuid();

    public DashboardQueryHandlerTests()
    {
        _tenantContext.SetupGet(t => t.TenantId).Returns(TestTenantId);
        _tenantContext.SetupGet(t => t.IsAvailable).Returns(true);

        _handler = new GetDashboardHandler(_reportRepo.Object, _cache.Object, _tenantContext.Object);
    }

    [Fact]
    public async Task Handle_ShopUser_Should_Return_DashboardDto_Successfully()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var query = new GetDashboardQuery(from, to, IsGlobalAdmin: false);

        var summary = new DashboardSummary(
            TotalSales: 1000, TotalDiscountAmount: 50, TotalPurchases: 400, TotalExpenses: 100, TotalProfit: 500,
            SalesCount: 10, ProductCount: 20, CustomerCount: 5, LowStockCount: 2,
            TotalShopsCount: 1, TotalUsersCount: 3, TotalCancelledBillsCount: 0, TotalCancelledAmount: 0);

        _reportRepo.Setup(r => r.GetDashboardSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _reportRepo.Setup(r => r.GetTopProductsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopProductRow>());
        _reportRepo.Setup(r => r.GetDailySalesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailySalesRow>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalSales.Should().Be(1000);
        result.Data.TotalPurchases.Should().Be(400);
    }

    [Fact]
    public async Task Handle_GlobalAdmin_Should_Return_DashboardDto_With_ShopMetrics()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var query = new GetDashboardQuery(from, to, IsGlobalAdmin: true);

        var summary = new DashboardSummary(
            TotalSales: 5000, TotalDiscountAmount: 200, TotalPurchases: 2000, TotalExpenses: 500, TotalProfit: 2500,
            SalesCount: 50, ProductCount: 100, CustomerCount: 25, LowStockCount: 5,
            TotalShopsCount: 3, TotalUsersCount: 12, TotalCancelledBillsCount: 1, TotalCancelledAmount: 50);

        var shopMetrics = new List<ShopMetricsRow>
        {
            new(TestTenantId, "Shop 1", "shop-1", "Standard", "Active", 4, 10, 20, 19, 1, 3000, 50, 0, DateTime.UtcNow)
        };

        _reportRepo.Setup(r => r.GetDashboardSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _reportRepo.Setup(r => r.GetTopProductsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopProductRow>());
        _reportRepo.Setup(r => r.GetDailySalesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailySalesRow>());
        _reportRepo.Setup(r => r.GetShopMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shopMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ShopMetrics.Should().NotBeNull();
        result.Data.ShopMetrics!.Count.Should().Be(1);
    }
}
