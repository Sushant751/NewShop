namespace Billing.Shared.Enums;

/// <summary>
/// Status of a tenant subscription / shop lifecycle.
/// </summary>
public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Terminated = 3,
    Trial = 4
}

/// <summary>
/// Lifecycle status of a sale.
/// </summary>
public enum SaleStatus
{
    Draft = 1,
    Held = 2,
    Completed = 3,
    Cancelled = 4,
    Returned = 5
}

/// <summary>
/// Payment methods supported by the POS terminal.
/// </summary>
public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    UPI = 3,
    Wallet = 4,
    Credit = 5,
    Split = 6
}

/// <summary>
/// Payment status for a sale.
/// </summary>
public enum PaymentStatus
{
    Unpaid = 1,
    Partial = 2,
    Paid = 3,
    Refunded = 4
}

/// <summary>
/// Direction of a stock movement.
/// </summary>
public enum StockMovementType
{
    PurchaseIn = 1,
    SaleOut = 2,
    TransferIn = 3,
    TransferOut = 4,
    AdjustmentIn = 5,
    AdjustmentOut = 6,
    ReturnIn = 7,
    InitialStock = 8
}

/// <summary>
/// Lifecycle status of a purchase order.
/// </summary>
public enum PurchaseStatus
{
    Draft = 1,
    Ordered = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5
}
