import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import type { ApiResult, RefreshRequest, RefreshResponse } from '../types';

// ============================================================================
// Axios instance with JWT auth + refresh token rotation interceptors
// ============================================================================

const TOKEN_KEY = 'billing_access_token';
const REFRESH_TOKEN_KEY = 'billing_refresh_token';
const USER_KEY = 'billing_user';

// --- Token storage helpers (localStorage) ---

export const tokenStorage = {
    getAccessToken(): string | null {
        return localStorage.getItem(TOKEN_KEY);
    },
    getRefreshToken(): string | null {
        return localStorage.getItem(REFRESH_TOKEN_KEY);
    },
    setTokens(accessToken: string, refreshToken: string): void {
        localStorage.setItem(TOKEN_KEY, accessToken);
        localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
    },
    getUser(): unknown | null {
        const raw = localStorage.getItem(USER_KEY);
        return raw ? JSON.parse(raw) : null;
    },
    setUser(user: unknown): void {
        localStorage.setItem(USER_KEY, JSON.stringify(user));
    },
    clear(): void {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
    },
};

// --- Axios instance ---

const apiClient: AxiosInstance = axios.create({
    baseURL: '/api',
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 30000,
});

// --- Request interceptor: inject Bearer token ---

apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = tokenStorage.getAccessToken();
        if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error: AxiosError) => Promise.reject(error),
);

// --- Refresh token state (prevent concurrent refresh) ---

let isRefreshing = false;
let refreshPromise: Promise<string> | null = null;
let failedQueue: Array<{
    resolve: (token: string) => void;
    reject: (error: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null): void {
    failedQueue.forEach((prom) => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token!);
        }
    });
    failedQueue = [];
}

// --- Response interceptor: handle 401 with refresh token rotation ---

apiClient.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & {
            _retry?: boolean;
        };

        // If not a 401 or already retried, reject
        if (error.response?.status !== 401 || originalRequest._retry) {
            return Promise.reject(error);
        }

        // Don't attempt refresh on auth endpoints
        if (
            originalRequest.url?.includes('/auth/login') ||
            originalRequest.url?.includes('/auth/refresh') ||
            originalRequest.url?.includes('/auth/register') ||
            originalRequest.url?.includes('/auth/forgot-password') ||
            originalRequest.url?.includes('/auth/reset-password')
        ) {
            return Promise.reject(error);
        }

        const refreshToken = tokenStorage.getRefreshToken();
        const accessToken = tokenStorage.getAccessToken();

        if (!refreshToken || !accessToken) {
            tokenStorage.clear();
            window.location.href = '/login';
            return Promise.reject(error);
        }

        // If already refreshing, queue this request
        if (isRefreshing) {
            return new Promise((resolve, reject) => {
                failedQueue.push({ resolve, reject });
            })
                .then((token) => {
                    if (originalRequest.headers) {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                    }
                    return apiClient(originalRequest);
                })
                .catch((err) => Promise.reject(err));
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
            refreshPromise = apiClient
                .post<ApiResult<RefreshResponse>>('/auth/refresh', {
                    accessToken,
                    refreshToken,
                } as RefreshRequest)
                .then((res) => {
                    const data = res.data.data;
                    if (!data) throw new Error('No refresh data');
                    tokenStorage.setTokens(data.accessToken, data.refreshToken);
                    return data.accessToken;
                });

            const newToken = await refreshPromise;
            processQueue(null, newToken);

            if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${newToken}`;
            }
            return apiClient(originalRequest);
        } catch (refreshError) {
            processQueue(refreshError, null);
            tokenStorage.clear();
            window.location.href = '/login';
            return Promise.reject(refreshError);
        } finally {
            isRefreshing = false;
            refreshPromise = null;
        }
    },
);

export default apiClient;
