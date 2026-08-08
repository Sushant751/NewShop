using Billing.Application.DTOs;
using Billing.Shared.Results;
using MediatR;

namespace Billing.Application.Commands.Reports;

public sealed record GetProfitLossReportQuery(DateTime? From, DateTime? To) : IRequest<Result<ProfitLossDto>>;
public sealed record GetSalesReportQuery(DateTime? From, DateTime? To) : IRequest<Result<SalesReportSummaryDto>>;
public sealed record GetGstReportQuery(DateTime? From, DateTime? To) : IRequest<Result<GstReportDto>>;
public sealed record GetPaymentSummaryReportQuery(DateTime? From, DateTime? To) : IRequest<Result<PaymentSummaryDto>>;
public sealed record GetInventoryValuationReportQuery() : IRequest<Result<InventoryValuationSummaryDto>>;
public sealed record GetTopProductsReportQuery(DateTime? From, DateTime? To, int Top = 20) : IRequest<Result<IReadOnlyList<TopProductDto>>>;
public sealed record GetReportsDashboardQuery(DateTime? From, DateTime? To) : IRequest<Result<ReportsDashboardDto>>;
