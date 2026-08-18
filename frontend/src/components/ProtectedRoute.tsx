import { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAppSelector } from '../store';
import { Permissions, Roles } from '../types';

interface ProtectedRouteProps {
    children: ReactNode;
    permission?: string;
    role?: string;
}

// ============================================================================
// ProtectedRoute - guards routes by authentication + optional permission/role
// ============================================================================

function ProtectedRoute({ children, permission, role }: ProtectedRouteProps) {
    const isAuthenticated = useAppSelector((state) => state.auth.isAuthenticated);
    const user = useAppSelector((state) => state.auth.user);
    const location = useLocation();

    if (!isAuthenticated) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    // GlobalAdmin and ShopAdmin bypass all permission/role checks
    if (user?.roles?.includes(Roles.GlobalAdmin) || user?.roles?.includes(Roles.ShopAdmin)) {
        return <>{children}</>;
    }

    // Check role requirement
    if (role && !user?.roles?.includes(role)) {
        return <Navigate to="/dashboard" replace />;
    }

    // Check permission requirement
    if (permission && !hasPermission(user, permission)) {
        return <Navigate to="/dashboard" replace />;
    }

    return <>{children}</>;
}

export default ProtectedRoute;

// Convenience helper: check if user has a given permission (GlobalAdmin and ShopAdmin bypass)
export function hasPermission(
    user: { permissions: string[]; roles: string[] } | null,
    permission: string,
): boolean {
    if (!user) return false;
    const userRoles = user.roles || [];
    if (userRoles.includes(Roles.GlobalAdmin) || userRoles.includes(Roles.ShopAdmin)) return true;
    if (user.permissions?.includes(permission)) return true;

    // Role-based fallbacks for Cashier, Clerk, Staff, and Manager
    const isCashierOrClerk = userRoles.includes(Roles.Cashier) || userRoles.includes(Roles.Staff) || userRoles.includes('Clerk') || userRoles.includes('Cashier');
    const isManager = userRoles.includes(Roles.Manager);

    if (isCashierOrClerk) {
        if ([Permissions.SalesCreate, Permissions.ProductsView, Permissions.CustomersView, Permissions.SalesView, Permissions.InventoryView].includes(permission as any)) {
            return true;
        }
    }
    if (isManager) {
        if (permission !== Permissions.SettingsManage && permission !== Permissions.StaffManage) {
            return true;
        }
    }

    return false;
}
