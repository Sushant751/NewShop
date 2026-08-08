import { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAppSelector } from '../store';
import { Roles } from '../types';

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
    if (permission && !user?.permissions?.includes(permission)) {
        return <Navigate to="/dashboard" replace />;
    }

    return <>{children}</>;
}

export default ProtectedRoute;

// Convenience helper: check if user has a given permission (GlobalAdmin bypasses)
export function hasPermission(
    user: { permissions: string[]; roles: string[] } | null,
    permission: string,
): boolean {
    if (!user) return false;
    if (user.roles.includes(Roles.GlobalAdmin)) return true;
    if (user.roles.includes(Roles.ShopAdmin)) return true;
    return user.permissions.includes(permission);
}
