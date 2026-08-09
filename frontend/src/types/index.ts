// ============================================================================
// API Response Envelope (matches Billing.Shared.Results.Result<T>)
// ============================================================================

export interface ApiResult<T> {
    success: boolean;
    message: string;
    data: T | null;
    errors: string[] | null;
}

// ============================================================================
// Pagination
// ============================================================================

export interface PagedResult<T> {
    items: T[];
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
}

export interface PagedQuery {
    page?: number;
    pageSize?: number;
    search?: string;
    orderBy?: string;
    ascending?: boolean;
}

// ============================================================================
// Auth DTOs (matches Billing.Application.DTOs.Auth)
// ============================================================================

export interface LoginRequest {
    email: string;
    password: string;
    tenantSlug?: string | null;
}

export interface LoginResponse {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    userId: string;
    tenantId: string;
    tenantName: string;
    userName: string;
    email: string;
    fullName: string;
    roles: string[];
    permissions: string[];
}

export interface RegisterRequest {
    fullName: string;
    email: string;
    password: string;
    phoneNumber?: string | null;
    tenantName?: string | null;
}

export interface RefreshRequest {
    accessToken: string;
    refreshToken: string;
}

export interface RefreshResponse {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export interface ForgotPasswordRequest {
    email: string;
}

export interface ResetPasswordRequest {
    email: string;
    token: string;
    newPassword: string;
}

export interface UserDto {
    id: string;
    fullName: string;
    email: string;
    phoneNumber: string | null;
    isActive: boolean;
    roles: string[];
}

// ============================================================================
// Product DTOs (matches Billing.Application.DTOs.Products)
// ============================================================================

export interface ProductDto {
    id: string;
    name: string;
    description: string | null;
    sku: string | null;
    barcode: string | null;
    categoryId: string | null;
    categoryName: string | null;
    brandId: string | null;
    brandName: string | null;
    unitId: string | null;
    unitName: string | null;
    costPrice: number;
    sellingPrice: number;
    taxRate: number;
    isTaxable: boolean;
    reorderLevel: number;
    currentStock: number;
    imageUrl: string | null;
    isActive: boolean;
    trackInventory: boolean;
}

export interface CreateProductRequest {
    name: string;
    description?: string | null;
    sku?: string | null;
    barcode?: string | null;
    categoryId?: string | null;
    brandId?: string | null;
    unitId?: string | null;
    costPrice: number;
    sellingPrice: number;
    taxRate: number;
    isTaxable: boolean;
    reorderLevel: number;
    openingStock: number;
    imageUrl?: string | null;
    trackInventory: boolean;
    allowSaleWithoutStock: boolean;
}

export interface UpdateProductRequest {
    name: string;
    description?: string | null;
    sku?: string | null;
    barcode?: string | null;
    categoryId?: string | null;
    brandId?: string | null;
    unitId?: string | null;
    costPrice: number;
    sellingPrice: number;
    taxRate: number;
    isTaxable: boolean;
    reorderLevel: number;
    imageUrl?: string | null;
    trackInventory: boolean;
    isActive: boolean;
}

// ============================================================================
// Sale DTOs (matches Billing.Application.DTOs.Sales)
// ============================================================================

export interface SaleItemDto {
    id: string;
    productId: string;
    productName: string;
    quantity: number;
    unitPrice: number;
    discountAmount: number;
    taxRate: number;
    taxAmount: number;
    lineTotal: number;
}

export interface PaymentDto {
    method: string;
    amount: number;
    reference: string | null;
    notes: string | null;
}

export interface SaleDto {
    id: string;
    invoiceNumber: string;
    shopId: string | null;
    customerId: string | null;
    customerName: string | null;
    cashierId: string;
    saleDate: string;
    status: string;
    paymentStatus: string;
    subTotal: number;
    discountAmount: number;
    taxAmount: number;
    roundOff: number;
    grandTotal: number;
    paidAmount: number;
    balanceDue: number;
    notes: string | null;
    items: SaleItemDto[];
    payments: PaymentDto[];
}

export interface SaleItemRequest {
    productId: string;
    quantity: number;
    unitPrice: number;
    discountAmount?: number;
}

export interface PaymentRequest {
    method: string;
    amount: number;
    reference?: string | null;
    notes?: string | null;
}

export interface CreateSaleRequest {
    customerId?: string | null;
    shopId?: string | null;
    items: SaleItemRequest[];
    payments: PaymentRequest[];
    discountAmount: number;
    notes?: string | null;
    couponCode?: string | null;
}

export interface CancelSaleRequest {
    reason: string;
}

// ============================================================================
// Customer DTOs (matches Billing.Application.DTOs)
// ============================================================================

export interface CustomerDto {
    id: string;
    name: string;
    email: string | null;
    phone: string | null;
    address: string | null;
    city: string | null;
    state: string | null;
    country: string | null;
    postalCode: string | null;
    taxNumber: string | null;
    currentBalance: number;
    creditLimit: number;
    isActive: boolean;
}

export interface CreateCustomerRequest {
    name: string;
    email?: string | null;
    phone?: string | null;
    address?: string | null;
    creditLimit?: number;
}

export interface UpdateCustomerRequest {
    name: string;
    email?: string | null;
    phone?: string | null;
    address?: string | null;
    creditLimit?: number;
    isActive?: boolean;
}

// ============================================================================
// Supplier DTOs
// ============================================================================

export interface SupplierDto {
    id: string;
    name: string;
    email: string | null;
    phone: string | null;
    address: string | null;
    contactPerson: string | null;
    isActive: boolean;
}

export interface CreateSupplierRequest {
    name: string;
    email?: string | null;
    phone?: string | null;
    address?: string | null;
    contactPerson?: string | null;
}

// ============================================================================
// Purchase DTOs
// ============================================================================

export interface PurchaseItemDto {
    id: string;
    productId: string;
    productName: string;
    quantity: number;
    unitCost: number;
    taxRate: number;
    taxAmount: number;
    lineTotal: number;
}

export interface PurchaseDto {
    id: string;
    purchaseNumber: string;
    shopId: string | null;
    supplierId: string;
    supplierName: string | null;
    purchaseDate: string;
    status: string;
    subTotal: number;
    discountAmount: number;
    taxAmount: number;
    grandTotal: number;
    paidAmount: number;
    balanceDue: number;
    notes: string | null;
    items: PurchaseItemDto[];
}

export interface PurchaseItemRequest {
    productId: string;
    quantity: number;
    unitCost: number;
    taxRate?: number;
}

export interface CreatePurchaseRequest {
    supplierId: string;
    shopId?: string | null;
    items: PurchaseItemRequest[];
    discountAmount?: number;
    paidAmount?: number;
    notes?: string | null;
}

// ============================================================================
// Dashboard DTOs
// ============================================================================

export interface TopProductDto {
    productId: string;
    productName: string;
    quantitySold: number;
    revenue: number;
}

export interface DailySalesDto {
    date: string;
    totalSales: number;
    salesCount: number;
}

export interface DashboardDto {
    totalSales: number;
    totalPurchases: number;
    totalExpenses: number;
    totalProfit: number;
    salesCount: number;
    productCount: number;
    customerCount: number;
    lowStockCount: number;
    topProducts: TopProductDto[];
    dailySales: DailySalesDto[];
}

// ============================================================================
// Report DTOs (matches Billing.Application.DTOs report records)
// ============================================================================

export interface ProfitLossDto {
    revenue: number;
    costOfGoods: number;
    expenses: number;
    grossProfit: number;
    netProfit: number;
}

export interface SalesReportDto {
    saleDate: string;
    invoiceNumber: string;
    customerName: string | null;
    subTotal: number;
    taxAmount: number;
    grandTotal: number;
    status: string;
    paymentStatus: string;
}

export interface SalesReportSummaryDto {
    sales: SalesReportDto[];
    totalSubTotal: number;
    totalTax: number;
    totalGrandTotal: number;
    totalCount: number;
}

export interface GstRateBreakdownDto {
    taxRate: number;
    taxableAmount: number;
    taxAmount: number;
    invoiceCount: number;
}

export interface GstReportDto {
    rateBreakdown: GstRateBreakdownDto[];
    totalTaxableAmount: number;
    totalTaxAmount: number;
    totalInvoices: number;
}

export interface PaymentMethodSummaryDto {
    paymentMethod: string;
    totalAmount: number;
    transactionCount: number;
}

export interface PaymentSummaryDto {
    methods: PaymentMethodSummaryDto[];
    totalAmount: number;
    totalTransactions: number;
}

export interface InventoryValuationDto {
    productId: string;
    productName: string;
    sku: string | null;
    currentStock: number;
    costPrice: number;
    stockValue: number;
}

export interface InventoryValuationSummaryDto {
    items: InventoryValuationDto[];
    totalStockValue: number;
    productCount: number;
}

export interface ReportsDashboardDto {
    profitLoss: ProfitLossDto;
    salesSummary: SalesReportSummaryDto;
    paymentSummary: PaymentSummaryDto;
    gstReport: GstReportDto;
    inventoryValuation: InventoryValuationSummaryDto;
}

// ============================================================================
// Enums (matches Billing.Shared.Enums)
// ============================================================================

export enum SaleStatus {
    Draft = 1,
    Held = 2,
    Completed = 3,
    Cancelled = 4,
    Returned = 5,
}

export enum PaymentMethod {
    Cash = 1,
    Card = 2,
    UPI = 3,
    Wallet = 4,
    Credit = 5,
    Split = 6,
}

export enum PaymentStatus {
    Unpaid = 1,
    Partial = 2,
    Paid = 3,
    Refunded = 4,
}

export enum StockMovementType {
    PurchaseIn = 1,
    SaleOut = 2,
    TransferIn = 3,
    TransferOut = 4,
    AdjustmentIn = 5,
    AdjustmentOut = 6,
    ReturnIn = 7,
    InitialStock = 8,
}

export enum PurchaseStatus {
    Draft = 1,
    Ordered = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5,
}

export enum TenantStatus {
    Active = 1,
    Suspended = 2,
    Terminated = 3,
    Trial = 4,
}

// ============================================================================
// Roles & Permissions (matches Billing.Shared.Enums.Roles)
// ============================================================================

export const Roles = {
    GlobalAdmin: 'GlobalAdmin',
    ShopAdmin: 'ShopAdmin',
    Manager: 'Manager',
    Cashier: 'Cashier',
    Staff: 'Staff',
    Clerk: 'Clerk',
} as const;

export const Permissions = {
    ProductsView: 'Products.View',
    ProductsCreate: 'Products.Create',
    ProductsEdit: 'Products.Edit',
    ProductsDelete: 'Products.Delete',
    SalesView: 'Sales.View',
    SalesCreate: 'Sales.Create',
    SalesCancel: 'Sales.Cancel',
    CustomersView: 'Customers.View',
    CustomersCreate: 'Customers.Create',
    CustomersEdit: 'Customers.Edit',
    CustomersDelete: 'Customers.Delete',
    PurchasesView: 'Purchases.View',
    PurchasesCreate: 'Purchases.Create',
    InventoryView: 'Inventory.View',
    InventoryAdjust: 'Inventory.Adjust',
    ReportsView: 'Reports.View',
    ExpensesView: 'Expenses.View',
    ExpensesCreate: 'Expenses.Create',
    SettingsView: 'Settings.View',
    SettingsEdit: 'Settings.Edit',
    StaffView: 'Staff.View',
    StaffManage: 'Staff.Manage',
} as const;

export type RoleName = (typeof Roles)[keyof typeof Roles];
export type PermissionName = (typeof Permissions)[keyof typeof Permissions];
