import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { authApi } from '../../api/endpoints';
import { tokenStorage } from '../../api/client';
import type {
    LoginRequest,
    LoginResponse,
    RegisterRequest,
    ChangePasswordRequest,
} from '../../types';

// ============================================================================
// Auth State
// ============================================================================

export interface AuthState {
    user: {
        userId: string;
        tenantId: string;
        tenantName: string;
        userName: string;
        email: string;
        fullName: string;
        roles: string[];
        permissions: string[];
    } | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    error: string | null;
}

const storedUser = tokenStorage.getUser() as AuthState['user'];

const initialState: AuthState = {
    user: storedUser,
    isAuthenticated: !!tokenStorage.getAccessToken(),
    isLoading: false,
    error: null,
};

// ============================================================================
// Async Thunks
// ============================================================================

export const loginUser = createAsyncThunk<
    LoginResponse,
    LoginRequest,
    { rejectValue: string }
>('auth/login', async (credentials, { rejectWithValue }) => {
    try {
        const response = await authApi.login(credentials);
        tokenStorage.setTokens(response.accessToken, response.refreshToken);
        tokenStorage.setUser({
            userId: response.userId,
            tenantId: response.tenantId,
            tenantName: response.tenantName,
            userName: response.userName,
            email: response.email,
            fullName: response.fullName,
            roles: response.roles,
            permissions: response.permissions,
        });
        return response;
    } catch (err) {
        const message = err instanceof Error ? err.message : 'Login failed';
        return rejectWithValue(message);
    }
});

export const registerUser = createAsyncThunk<
    LoginResponse,
    RegisterRequest,
    { rejectValue: string }
>('auth/register', async (data, { rejectWithValue }) => {
    try {
        const response = await authApi.register(data);
        tokenStorage.setTokens(response.accessToken, response.refreshToken);
        tokenStorage.setUser({
            userId: response.userId,
            tenantId: response.tenantId,
            tenantName: response.tenantName,
            userName: response.userName,
            email: response.email,
            fullName: response.fullName,
            roles: response.roles,
            permissions: response.permissions,
        });
        return response;
    } catch (err) {
        const message = err instanceof Error ? err.message : 'Registration failed';
        return rejectWithValue(message);
    }
});

export const logoutUser = createAsyncThunk<
    void,
    void,
    { rejectValue: string }
>('auth/logout', async () => {
    try {
        const refreshToken = tokenStorage.getRefreshToken();
        const accessToken = tokenStorage.getAccessToken();
        if (refreshToken && accessToken) {
            await authApi.logout({ accessToken, refreshToken });
        }
    } catch {
        // Ignore logout API errors - we clear local state anyway
    } finally {
        tokenStorage.clear();
    }
});

export const changePassword = createAsyncThunk<
    void,
    ChangePasswordRequest,
    { rejectValue: string }
>('auth/changePassword', async (data, { rejectWithValue }) => {
    try {
        await authApi.changePassword(data);
    } catch (err) {
        const message = err instanceof Error ? err.message : 'Password change failed';
        return rejectWithValue(message);
    }
});

// ============================================================================
// Auth Slice
// ============================================================================

const authSlice = createSlice({
    name: 'auth',
    initialState,
    reducers: {
        clearError: (state) => {
            state.error = null;
        },
        clearAuth: (state) => {
            state.user = null;
            state.isAuthenticated = false;
            state.error = null;
            tokenStorage.clear();
        },
        setUser: (
            state,
            action: PayloadAction<NonNullable<AuthState['user']>>,
        ) => {
            state.user = action.payload;
            state.isAuthenticated = true;
        },
    },
    extraReducers: (builder) => {
        builder
            // Login
            .addCase(loginUser.pending, (state) => {
                state.isLoading = true;
                state.error = null;
            })
            .addCase(loginUser.fulfilled, (state, action) => {
                state.isLoading = false;
                state.isAuthenticated = true;
                state.user = {
                    userId: action.payload.userId,
                    tenantId: action.payload.tenantId,
                    tenantName: action.payload.tenantName,
                    userName: action.payload.userName,
                    email: action.payload.email,
                    fullName: action.payload.fullName,
                    roles: action.payload.roles,
                    permissions: action.payload.permissions,
                };
            })
            .addCase(loginUser.rejected, (state, action) => {
                state.isLoading = false;
                state.error = action.payload || 'Login failed';
                state.isAuthenticated = false;
            })
            // Register
            .addCase(registerUser.pending, (state) => {
                state.isLoading = true;
                state.error = null;
            })
            .addCase(registerUser.fulfilled, (state, action) => {
                state.isLoading = false;
                state.isAuthenticated = true;
                state.user = {
                    userId: action.payload.userId,
                    tenantId: action.payload.tenantId,
                    tenantName: action.payload.tenantName,
                    userName: action.payload.userName,
                    email: action.payload.email,
                    fullName: action.payload.fullName,
                    roles: action.payload.roles,
                    permissions: action.payload.permissions,
                };
            })
            .addCase(registerUser.rejected, (state, action) => {
                state.isLoading = false;
                state.error = action.payload || 'Registration failed';
            })
            // Logout
            .addCase(logoutUser.fulfilled, (state) => {
                state.user = null;
                state.isAuthenticated = false;
                state.error = null;
            })
            // Change password
            .addCase(changePassword.pending, (state) => {
                state.isLoading = true;
                state.error = null;
            })
            .addCase(changePassword.fulfilled, (state) => {
                state.isLoading = false;
            })
            .addCase(changePassword.rejected, (state, action) => {
                state.isLoading = false;
                state.error = action.payload || 'Password change failed';
            });
    },
});

export const { clearError, clearAuth, setUser } = authSlice.actions;
export default authSlice.reducer;
