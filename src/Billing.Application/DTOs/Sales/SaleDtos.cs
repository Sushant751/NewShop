using Billing.Shared.Enums;

namespace Billing.Application.DTOs.Sales;

public sealed record SaleItemDto(
    Guid Id, Guid ProductId, string ProductName, decimal Quantity,
    decimal UnitPrice, decimal DiscountAmount, decimal TaxRate,
    decimal TaxAmount, decimal LineTotal);

public sealed record PaymentDto(
    Guid Id, PaymentMethod Method, decimal Amount, string? Reference, DateTime PaidAt);

public sealed record SaleDto(
    Guid Id, string InvoiceNumber, Guid? ShopId, Guid? CustomerId, string? CustomerName,
    Guid CashierId, DateTime SaleDate, SaleStatus Status, PaymentStatus PaymentStatus,
    decimal SubTotal, decimal DiscountAmount, decimal TaxAmount, decimal RoundOff,
    decimal GrandTotal, decimal PaidAmount, decimal BalanceDue, string? Notes,
    IReadOnlyList<SaleItemDto> Items, IReadOnlyList<PaymentDto> Payments);

public sealed record SaleItemRequest(Guid ProductId, decimal Quantity, decimal UnitPrice, decimal DiscountAmount = 0);

public sealed record PaymentRequest(PaymentMethod Method, decimal Amount, string? Reference, string? Notes);

public sealed record CreateSaleRequest(
    Guid? CustomerId, Guid? ShopId, List<SaleItemRequest> Items,
    List<PaymentRequest> Payments, decimal DiscountAmount, string? Notes, string? CouponCode);

public sealed record CancelSaleRequest(string? Reason);
