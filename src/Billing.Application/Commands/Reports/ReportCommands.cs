using Billing.Application.DTOs;
using Billing.Shared.Results;
using MediatR;

namespace Billing.Application.Commands.Reports;

public sealed record GetProfitLossReportQuery(DateTime? From, DateTime? To, bool IsGlobalAdmin = false) : IRequest<Result<ProfitLossDto>>;
public sealed record GetSalesReportQuery(DateTime? From, DateTime? To, bool IsGlobalAdmin = false) : IRequest<Result<SalesReportSummaryDto>>;
public sealed record GetGstReportQuery(DateTime? From, DateTime? To, bool IsGlobalAdmin = false) : IRequest<Result<GstReportDto>>;
public sealed record GetPaymentSummaryReportQuery(DateTime? From, DateTime? To, bool IsGlobalAdmin = false) : IRequest<Result<PaymentSummaryDto>>;
public sealed record GetInventoryValuationReportQuery(bool IsGlobalAdmin = false) : IRequest<Result<InventoryValuationSummaryDto>>;
public sealed record GetTopProductsReportQuery(DateTime? From, DateTime? To, int Top = 20, bool IsGlobalAdmin = false) : IRequest<Result<IReadOnlyList<TopProductDto>>>;
public sealed record GetReportsDashboardQuery(DateTime? From, DateTime? To, bool IsGlobalAdmin = false) : IRequest<Result<ReportsDashboardDto>>;
