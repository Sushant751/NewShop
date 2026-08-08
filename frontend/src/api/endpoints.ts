import apiClient from './client';
import type {
    ApiResult,
    PagedResult,
    PagedQuery,
    LoginRequest,
    LoginResponse,
    RegisterRequest,
    RefreshRequest,
    RefreshResponse,
    ChangePasswordRequest,
    ForgotPasswordRequest,
    ResetPasswordRequest,
    ProductDto,
    CreateProductRequest,
    UpdateProductRequest,
    SaleDto,
    CreateSaleRequest,
    CancelSaleRequest,
    CustomerDto,
    CreateCustomerRequest,
    UpdateCustomerRequest,
    SupplierDto,
    CreateSupplierRequest,
    PurchaseDto,
    CreatePurchaseRequest,
    DashboardDto,
    ProfitLossDto,
    SalesReportSummaryDto,
    GstReportDto,
    PaymentSummaryDto,
    InventoryValuationSummaryDto,
    TopProductDto,
    ReportsDashboardDto,
} from '../types';

// ============================================================================
// Helper: unwrap ApiResult<T> → T (throws on failure)
// ============================================================================

async function unwrap<T>(promise: Promise<{ data: ApiResult<T> }>): Promise<T> {
    try {
        const res = await promise;
        if (!res.data.success || res.data.data === null) {
            const msg = res.data.message || 'Request failed';
            const errs = res.data.errors?.join(', ') || '';
            throw new Error(errs ? `${msg}: ${errs}` : msg);
        }
        return res.data.data;
    } catch (err: any) {
        if (err.response?.data?.errors && typeof err.response.data.errors === 'object') {
            const errs = Object.values(err.response.data.errors).flat().join(', ');
            throw new Error(errs);
        }
        if (err.response?.data?.title) {
            throw new Error(err.response.data.title);
        }
        if (err.response?.data?.message) {
            throw new Error(err.response.data.message);
        }
        throw err;
    }
}

// Helper: unwrap ApiResult<void> (no data)
async function unwrapVoid(promise: Promise<{ data: ApiResult<unknown> }>): Promise<void> {
    try {
        const res = await promise;
        if (!res.data.success) {
            const msg = res.data.message || 'Request failed';
            const errs = res.data.errors?.join(', ') || '';
            throw new Error(errs ? `${msg}: ${errs}` : msg);
        }
    } catch (err: any) {
        if (err.response?.data?.errors && typeof err.response.data.errors === 'object') {
            const errs = Object.values(err.response.data.errors).flat().join(', ');
            throw new Error(errs);
        }
        if (err.response?.data?.title) {
            throw new Error(err.response.data.title);
        }
        if (err.response?.data?.message) {
            throw new Error(err.response.data.message);
        }
        throw err;
    }
}

// ============================================================================
// Auth API
// ============================================================================

export const authApi = {
    login: (data: LoginRequest) =>
        unwrap<LoginResponse>(apiClient.post('/auth/login', data)),
    register: (data: RegisterRequest) =>
        unwrap<LoginResponse>(apiClient.post('/auth/register', data)),
    refresh: (data: RefreshRequest) =>
        unwrap<RefreshResponse>(apiClient.post('/auth/refresh', data)),
    revoke: (data: RefreshRequest) =>
        unwrapVoid(apiClient.post('/auth/revoke', data)),
    logout: (data: RefreshRequest) =>
        unwrapVoid(apiClient.post('/auth/logout', data)),
    changePassword: (data: ChangePasswordRequest) =>
        unwrapVoid(apiClient.post('/auth/change-password', data)),
    forgotPassword: (data: ForgotPasswordRequest) =>
        unwrap<string>(apiClient.post('/auth/forgot-password', data)),
    resetPassword: (data: ResetPasswordRequest) =>
        unwrapVoid(apiClient.post('/auth/reset-password', data)),
};

// ============================================================================
// Products API
// ============================================================================

export const productsApi = {
    list: (params: PagedQuery) =>
        unwrap<PagedResult<ProductDto>>(apiClient.get('/products', { params })),
    getById: (id: string) =>
        unwrap<ProductDto>(apiClient.get(`/products/${id}`)),
    create: (data: CreateProductRequest) =>
        unwrap<ProductDto>(apiClient.post('/products', data)),
    update: (id: string, data: UpdateProductRequest) =>
        unwrapVoid(apiClient.put(`/products/${id}`, data)),
    delete: (id: string) =>
        unwrapVoid(apiClient.delete(`/products/${id}`)),
    search: (term: string, limit = 10) =>
        unwrap<ProductDto[]>(apiClient.get('/products/search', { params: { term, limit } })),
    lowStock: () =>
        unwrap<ProductDto[]>(apiClient.get('/products/low-stock')),
};

// ============================================================================
// Sales API
// ============================================================================

export const salesApi = {
    list: (params: { from?: string; to?: string; page?: number; pageSize?: number }) =>
        unwrap<PagedResult<SaleDto>>(apiClient.get('/sales', { params })),
    getById: (id: string) =>
        unwrap<SaleDto>(apiClient.get(`/sales/${id}`)),
    create: (data: CreateSaleRequest) =>
        unwrap<SaleDto>(apiClient.post('/sales', data)),
    cancel: (id: string, data: CancelSaleRequest) =>
        unwrapVoid(apiClient.post(`/sales/${id}/cancel`, data)),
};

// ============================================================================
// Customers API
// ============================================================================

export const customersApi = {
    list: (params: PagedQuery) =>
        unwrap<PagedResult<CustomerDto>>(apiClient.get('/customers', { params })),
    getById: (id: string) =>
        unwrap<CustomerDto>(apiClient.get(`/customers/${id}`)),
    create: (data: CreateCustomerRequest) =>
        unwrap<CustomerDto>(apiClient.post('/customers', data)),
    update: (id: string, data: UpdateCustomerRequest) =>
        unwrapVoid(apiClient.put(`/customers/${id}`, data)),
    delete: (id: string) =>
        unwrapVoid(apiClient.delete(`/customers/${id}`)),
    search: (term: string, limit = 10) =>
        unwrap<CustomerDto[]>(apiClient.get('/customers/search', { params: { term, limit } })),
};

// ============================================================================
// Suppliers API
// ============================================================================

export const suppliersApi = {
    list: (params: PagedQuery) =>
        unwrap<PagedResult<SupplierDto>>(apiClient.get('/suppliers', { params })),
    getById: (id: string) =>
        unwrap<SupplierDto>(apiClient.get(`/suppliers/${id}`)),
    create: (data: CreateSupplierRequest) =>
        unwrap<SupplierDto>(apiClient.post('/suppliers', data)),
};

// ============================================================================
// Purchases API
// ============================================================================

export const purchasesApi = {
    list: (params: PagedQuery) =>
        unwrap<PagedResult<PurchaseDto>>(apiClient.get('/purchases', { params })),
    getById: (id: string) =>
        unwrap<PurchaseDto>(apiClient.get(`/purchases/${id}`)),
    create: (data: CreatePurchaseRequest) =>
        unwrap<PurchaseDto>(apiClient.post('/purchases', data)),
};

// ============================================================================
// Dashboard API
// ============================================================================

export const dashboardApi = {
    get: (params: { from?: string; to?: string }) =>
        unwrap<DashboardDto>(apiClient.get('/dashboard', { params })),
};

// ============================================================================
// Reports API
// ============================================================================

export const reportsApi = {
    profitLoss: (params: { from: string; to: string }) =>
        unwrap<ProfitLossDto>(apiClient.get('/reports/profit-loss', { params })),
    sales: (params: { from: string; to: string }) =>
        unwrap<SalesReportSummaryDto>(apiClient.get('/reports/sales', { params })),
    salesExport: (params: { from: string; to: string }) =>
        apiClient.get('/reports/sales/export', { params, responseType: 'blob' }),
    gst: (params: { from: string; to: string }) =>
        unwrap<GstReportDto>(apiClient.get('/reports/gst', { params })),
    gstExport: (params: { from: string; to: string }) =>
        apiClient.get('/reports/gst/export', { params, responseType: 'blob' }),
    payments: (params: { from: string; to: string }) =>
        unwrap<PaymentSummaryDto>(apiClient.get('/reports/payments', { params })),
    inventoryValuation: () =>
        unwrap<InventoryValuationSummaryDto>(apiClient.get('/reports/inventory-valuation')),
    inventoryValuationExport: () =>
        apiClient.get('/reports/inventory-valuation/export', { responseType: 'blob' }),
    topProducts: (params: { from: string; to: string; top?: number }) =>
        unwrap<TopProductDto[]>(apiClient.get('/reports/top-products', { params })),
    dashboard: (params: { from: string; to: string }) =>
        unwrap<ReportsDashboardDto>(apiClient.get('/reports/dashboard', { params })),
};

// ============================================================================
// Users API
// ============================================================================

export interface UserDto {
    id: string;
    userName: string;
    email: string;
    fullName: string;
    phoneNumber?: string;
    isActive: boolean;
    lastLoginAt?: string;
    roles: string[];
}

export interface CreateUserRequest {
    fullName: string;
    email: string;
    phoneNumber?: string;
    password?: string;
    role: string;
}

export interface UpdateUserRequest {
    isActive: boolean;
    role: string;
}

export const usersApi = {
    list: () =>
        unwrap<UserDto[]>(apiClient.get('/users')),
    create: (data: CreateUserRequest) =>
        unwrap<UserDto>(apiClient.post('/users', data)),
    update: (id: string, data: UpdateUserRequest) =>
        unwrap<void>(apiClient.put(`/users/${id}`, data)),
};
