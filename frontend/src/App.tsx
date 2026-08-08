import { Routes, Route, Navigate } from 'react-router-dom';
import { useAppSelector } from './store';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import ResetPasswordPage from './pages/ResetPasswordPage';
import DashboardPage from './pages/DashboardPage';
import ProductsPage from './pages/ProductsPage';
import ProductFormPage from './pages/ProductFormPage';
import POSPage from './pages/POSPage';
import SalesPage from './pages/SalesPage';
import SaleDetailPage from './pages/SaleDetailPage';
import CustomersPage from './pages/CustomersPage';
import SuppliersPage from './pages/SuppliersPage';
import PurchasesPage from './pages/PurchasesPage';
import ReportsPage from './pages/ReportsPage';
import SettingsPage from './pages/SettingsPage';
import StaffPage from './pages/StaffPage';
import NotFoundPage from './pages/NotFoundPage';

function App() {
    const isAuthenticated = useAppSelector((state) => state.auth.isAuthenticated);

    return (
        <Routes>
            {/* Public routes */}
            <Route
                path="/login"
                element={isAuthenticated ? <Navigate to="/dashboard" replace /> : <LoginPage />}
            />
            <Route
                path="/register"
                element={isAuthenticated ? <Navigate to="/dashboard" replace /> : <RegisterPage />}
            />
            <Route
                path="/forgot-password"
                element={isAuthenticated ? <Navigate to="/dashboard" replace /> : <ForgotPasswordPage />}
            />
            <Route
                path="/reset-password"
                element={isAuthenticated ? <Navigate to="/dashboard" replace /> : <ResetPasswordPage />}
            />

            {/* Protected routes with layout */}
            <Route
                element={
                    <ProtectedRoute>
                        <Layout />
                    </ProtectedRoute>
                }
            >
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/products" element={<ProductsPage />} />
                <Route path="/products/new" element={<ProductFormPage />} />
                <Route path="/products/:id/edit" element={<ProductFormPage />} />
                <Route path="/pos" element={<POSPage />} />
                <Route path="/sales" element={<SalesPage />} />
                <Route path="/sales/:id" element={<SaleDetailPage />} />
                <Route path="/customers" element={<CustomersPage />} />
                <Route path="/suppliers" element={<SuppliersPage />} />
                <Route path="/purchases" element={<PurchasesPage />} />
                <Route
                    path="/reports"
                    element={
                        <ProtectedRoute permission="Reports.View">
                            <ReportsPage />
                        </ProtectedRoute>
                    }
                />
                <Route path="/settings" element={<SettingsPage />} />
                <Route path="/staff" element={<StaffPage />} />
            </Route>

            {/* Default redirect */}
            <Route path="/" element={<Navigate to="/dashboard" replace />} />

            {/* 404 */}
            <Route path="*" element={<NotFoundPage />} />
        </Routes>
    );
}

export default App;
