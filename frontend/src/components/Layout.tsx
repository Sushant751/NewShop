import { useState } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
    AppBar,
    Box,
    CssBaseline,
    Divider,
    Drawer,
    IconButton,
    List,
    ListItem,
    ListItemButton,
    ListItemIcon,
    ListItemText,
    ListSubheader,
    Toolbar,
    Typography,
    Avatar,
    Menu,
    MenuItem,
    Tooltip,
    Badge,
    InputBase,
    Chip,
    Paper,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import DashboardIcon from '@mui/icons-material/DashboardOutlined';
import InventoryIcon from '@mui/icons-material/Inventory2Outlined';
import PointOfSaleIcon from '@mui/icons-material/PointOfSaleOutlined';
import ReceiptIcon from '@mui/icons-material/ReceiptLongOutlined';
import PeopleIcon from '@mui/icons-material/PeopleOutline';
import LocalShippingIcon from '@mui/icons-material/LocalShippingOutlined';
import ShoppingBagIcon from '@mui/icons-material/ShoppingBagOutlined';
import AssessmentIcon from '@mui/icons-material/AssessmentOutlined';
import SettingsIcon from '@mui/icons-material/SettingsOutlined';
import LogoutIcon from '@mui/icons-material/LogoutOutlined';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import SearchIcon from '@mui/icons-material/Search';
import StorefrontIcon from '@mui/icons-material/Storefront';
import { useQueryClient } from 'react-query';
import { useAppDispatch, useAppSelector } from '../store';
import { logoutUser } from '../store/slices/authSlice';
import { Permissions, Roles } from '../types';

import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';

const drawerWidth = 260;

interface NavGroup {
    category: string;
    items: NavItem[];
}

interface NavItem {
    label: string;
    path: string;
    icon: React.ReactNode;
    permission?: string;
    badge?: string;
}

// Global Admin: Dedicated administrative suite — only staff management, system analytics, and platform settings.
const globalAdminNavGroups: NavGroup[] = [
    {
        category: 'PLATFORM MANAGEMENT',
        items: [
            { label: 'Dashboard', path: '/dashboard', icon: <DashboardIcon /> },
            { label: 'All Users & Staff', path: '/staff', icon: <PeopleIcon /> },
        ],
    },
    {
        category: 'SYSTEM & ANALYTICS',
        items: [
            { label: 'Platform Reports', path: '/reports', icon: <AssessmentIcon /> },
            { label: 'Settings', path: '/settings', icon: <SettingsIcon /> },
        ],
    },
];

// Shop Admins & Staff: Operational POS, store inventory, suppliers, customers, and sales.
const shopNavGroups: NavGroup[] = [
    {
        category: 'NAVIGATION',
        items: [
            { label: 'Dashboard', path: '/dashboard', icon: <DashboardIcon /> },
            { label: 'Point of Sale (POS)', path: '/pos', icon: <PointOfSaleIcon />, permission: Permissions.SalesCreate, badge: 'HOT' },
            { label: 'Sales History', path: '/sales', icon: <ReceiptIcon />, permission: Permissions.SalesView },
        ],
    },
    {
        category: 'MANAGEMENT',
        items: [
            { label: 'Products', path: '/products', icon: <InventoryIcon />, permission: Permissions.ProductsView },
            { label: 'Customers', path: '/customers', icon: <PeopleIcon />, permission: Permissions.CustomersView },
            { label: 'Suppliers', path: '/suppliers', icon: <LocalShippingIcon />, permission: Permissions.PurchasesView },
            { label: 'Purchases', path: '/purchases', icon: <ShoppingBagIcon />, permission: Permissions.PurchasesView },
            { label: 'Staff', path: '/staff', icon: <PeopleIcon />, permission: Permissions.StaffManage },
        ],
    },
    {
        category: 'ANALYTICS & SYSTEM',
        items: [
            { label: 'Reports', path: '/reports', icon: <AssessmentIcon />, permission: Permissions.ReportsView },
            { label: 'Settings', path: '/settings', icon: <SettingsIcon />, permission: Permissions.SettingsView },
        ],
    },
];

function Layout() {
    const [open, setOpen] = useState(true);
    const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
    const navigate = useNavigate();
    const location = useLocation();
    const dispatch = useAppDispatch();
    const user = useAppSelector((state) => state.auth.user);

    const isGlobalAdmin = user?.roles?.includes(Roles.GlobalAdmin);
    const activeNavGroups = isGlobalAdmin ? globalAdminNavGroups : shopNavGroups;

    const canAccess = (item: NavItem): boolean => {
        if (isGlobalAdmin) return true;
        if (!item.permission) return true;
        if (user?.roles?.includes(Roles.ShopAdmin)) return true; // Shop admin can manage store staff
        return user?.permissions?.includes(item.permission) ?? false;
    };

    const toggleDrawer = () => setOpen(!open);

    const handleProfileMenu = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleCloseMenu = () => setAnchorEl(null);

    const queryClient = useQueryClient();

    const handleLogout = async () => {
        await dispatch(logoutUser());
        // Clear ALL cached query data so the next user never sees stale data
        // from the previous session (e.g. dashboard, reports, users).
        queryClient.clear();
        navigate('/login');
    };

    const initials = user?.fullName
        ? user.fullName
            .split(' ')
            .map((n) => n[0])
            .slice(0, 2)
            .join('')
            .toUpperCase()
        : 'A';

    return (
        <Box sx={{ display: 'flex' }}>
            <CssBaseline />
            
            {/* Top Navigation Bar - Mantis Style */}
            <AppBar
                position="fixed"
                sx={{
                    zIndex: (theme) => theme.zIndex.drawer + 1,
                    transition: (theme) =>
                        theme.transitions.create(['width', 'margin'], {
                            easing: theme.transitions.easing.sharp,
                            duration: theme.transitions.duration.leavingScreen,
                        }),
                    marginLeft: open ? `${drawerWidth}px` : 0,
                    width: open ? `calc(100% - ${drawerWidth}px)` : '100%',
                    bgcolor: '#ffffff',
                    borderBottom: '1px solid #e6ebf1',
                    boxShadow: 'none',
                }}
            >
                <Toolbar sx={{ px: 3 }}>
                    <IconButton
                        color="inherit"
                        edge="start"
                        onClick={toggleDrawer}
                        sx={{ mr: 2, color: '#5b6b79' }}
                    >
                        {open ? <ChevronLeftIcon /> : <MenuIcon />}
                    </IconButton>

                    {/* Mantis Header Search Bar */}
                    <Paper
                        component="form"
                        onSubmit={(e) => e.preventDefault()}
                        sx={{
                            p: '2px 12px',
                            display: { xs: 'none', sm: 'flex' },
                            alignItems: 'center',
                            width: 320,
                            bgcolor: '#f8fafc',
                            border: '1px solid #e6ebf1',
                            borderRadius: 2,
                            boxShadow: 'none',
                        }}
                    >
                        <SearchIcon sx={{ color: '#8c8c8c', mr: 1, fontSize: 20 }} />
                        <InputBase
                            placeholder="Search (Ctrl + K)..."
                            sx={{ fontSize: '0.8125rem', width: '100%' }}
                        />
                    </Paper>

                    <Box sx={{ flexGrow: 1 }} />

                    {/* Live Tenant Info Badge */}
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mr: 2 }}>
                        <Chip
                            label={isGlobalAdmin ? 'APP ADMIN' : (user?.tenantName || user?.roles?.[0] || 'STAFF').toUpperCase()}
                            size="small"
                            sx={{
                                bgcolor: isGlobalAdmin ? '#f6ffed' : '#e8f0ff',
                                color: isGlobalAdmin ? '#52c41a' : '#4680ff',
                                fontWeight: 700,
                                fontSize: '0.7rem',
                                borderRadius: 1.5,
                                border: isGlobalAdmin ? '1px solid #b7eb8f' : 'none',
                            }}
                        />
                    </Box>

                    {/* User Profile Dropdown */}
                    <Tooltip title={user?.fullName || 'User Profile'}>
                        <IconButton onClick={handleProfileMenu} sx={{ p: 0.5 }}>
                            <Badge
                                overlap="circular"
                                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                                variant="dot"
                                color="success"
                            >
                                <Avatar
                                    sx={{
                                        bgcolor: '#4680ff',
                                        color: '#ffffff',
                                        width: 38,
                                        height: 38,
                                        fontWeight: 600,
                                        fontSize: '0.9375rem',
                                    }}
                                >
                                    {initials}
                                </Avatar>
                            </Badge>
                        </IconButton>
                    </Tooltip>

                    <Menu
                        anchorEl={anchorEl}
                        open={Boolean(anchorEl)}
                        onClose={handleCloseMenu}
                        keepMounted
                        PaperProps={{
                            elevation: 0,
                            sx: {
                                overflow: 'visible',
                                filter: 'drop-shadow(0px 2px 10px rgba(0,0,0,0.08))',
                                mt: 1.5,
                                borderRadius: 2.5,
                                border: '1px solid #e6ebf1',
                                minWidth: 200,
                                '& .MuiAvatar-root': {
                                    width: 32,
                                    height: 32,
                                    ml: -0.5,
                                    mr: 1,
                                },
                            },
                        }}
                        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                    >
                        <Box sx={{ px: 2, py: 1.5 }}>
                            <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#1d2630' }}>
                                {user?.fullName || 'Administrator'}
                            </Typography>
                            <Typography variant="caption" color="textSecondary">
                                {user?.email || 'admin@billingsystem.com'}
                            </Typography>
                        </Box>
                        <Divider sx={{ my: 0.5 }} />
                        <MenuItem onClick={handleLogout} sx={{ color: '#ff4d4f', py: 1 }}>
                            <ListItemIcon sx={{ color: '#ff4d4f' }}>
                                <LogoutIcon fontSize="small" />
                            </ListItemIcon>
                            Sign Out
                        </MenuItem>
                    </Menu>
                </Toolbar>
            </AppBar>

            {/* Sidebar Navigation Drawer - Mantis Style */}
            <Drawer
                variant="persistent"
                open={open}
                sx={{
                    width: drawerWidth,
                    flexShrink: 0,
                    '& .MuiDrawer-paper': {
                        width: drawerWidth,
                        boxSizing: 'border-box',
                        bgcolor: '#ffffff',
                        borderRight: '1px solid #e6ebf1',
                    },
                }}
            >
                {/* Brand Header */}
                <Box
                    sx={{
                        display: 'flex',
                        alignItems: 'center',
                        px: 3,
                        py: 2.2,
                        gap: 1.5,
                        borderBottom: '1px solid #e6ebf1',
                    }}
                >
                    <Box
                        sx={{
                            bgcolor: isGlobalAdmin ? '#1890ff' : '#4680ff',
                            color: '#ffffff',
                            borderRadius: 2,
                            p: 1,
                            display: 'flex',
                            boxShadow: isGlobalAdmin ? '0 2px 8px rgba(24, 144, 255, 0.35)' : '0 2px 8px rgba(70, 128, 255, 0.35)',
                        }}
                    >
                        {isGlobalAdmin ? <AdminPanelSettingsIcon /> : <StorefrontIcon />}
                    </Box>
                    <Box>
                        <Typography variant="h6" sx={{ fontWeight: 700, lineHeight: 1.2, color: '#1d2630' }}>
                            {isGlobalAdmin ? 'App Admin' : (user?.tenantName || 'My Shop')}
                        </Typography>
                        <Typography variant="caption" sx={{ color: isGlobalAdmin ? '#52c41a' : '#8c8c8c', fontWeight: 600 }}>
                            {isGlobalAdmin ? 'GLOBAL ADMIN' : (user?.roles?.[0] || 'STAFF').toUpperCase()}
                        </Typography>
                    </Box>
                </Box>

                <Box sx={{ overflow: 'auto', py: 1 }}>
                    {activeNavGroups.map((group) => {
                        const accessibleItems = group.items.filter(canAccess);
                        if (accessibleItems.length === 0) return null;

                        return (
                            <List
                                key={group.category}
                                subheader={
                                    <ListSubheader
                                        disableSticky
                                        sx={{
                                            bgcolor: 'transparent',
                                            color: '#8c8c8c',
                                            fontSize: '0.6875rem',
                                            fontWeight: 700,
                                            letterSpacing: '0.8px',
                                            lineHeight: '32px',
                                            px: 3,
                                            mt: 1,
                                        }}
                                    >
                                        {group.category}
                                    </ListSubheader>
                                }
                            >
                                {accessibleItems.map((item) => {
                                    const isActive =
                                        location.pathname === item.path ||
                                        (item.path !== '/dashboard' && location.pathname.startsWith(item.path));

                                    return (
                                        <ListItem key={item.path} disablePadding sx={{ px: 1.5, py: 0.3 }}>
                                            <ListItemButton
                                                onClick={() => navigate(item.path)}
                                                selected={isActive}
                                                sx={{
                                                    borderRadius: 2,
                                                    py: 1,
                                                    px: 2,
                                                    transition: 'all 0.2s ease',
                                                    bgcolor: isActive ? '#e8f0ff !important' : 'transparent',
                                                    color: isActive ? '#4680ff' : '#5b6b79',
                                                    '&:hover': {
                                                        bgcolor: isActive ? '#e8f0ff' : '#f8fafc',
                                                        color: isActive ? '#4680ff' : '#1d2630',
                                                    },
                                                }}
                                            >
                                                <ListItemIcon
                                                    sx={{
                                                        color: isActive ? '#4680ff' : '#8c8c8c',
                                                        minWidth: 36,
                                                    }}
                                                >
                                                    {item.icon}
                                                </ListItemIcon>
                                                <ListItemText
                                                    primary={item.label}
                                                    primaryTypographyProps={{
                                                        fontSize: '0.875rem',
                                                        fontWeight: isActive ? 600 : 500,
                                                    }}
                                                />
                                                {item.badge && (
                                                    <Chip
                                                        label={item.badge}
                                                        size="small"
                                                        sx={{
                                                            height: 18,
                                                            fontSize: '0.625rem',
                                                            bgcolor: '#ff4d4f',
                                                            color: '#ffffff',
                                                            fontWeight: 700,
                                                        }}
                                                    />
                                                )}
                                            </ListItemButton>
                                        </ListItem>
                                    );
                                })}
                            </List>
                        );
                    })}
                </Box>
            </Drawer>

            {/* Main Content Area */}
            <Box
                component="main"
                sx={{
                    flexGrow: 1,
                    p: 3.5,
                    width: `calc(100% - ${open ? drawerWidth : 0}px)`,
                    minHeight: '100vh',
                    bgcolor: '#f4f6f8',
                    transition: (theme) => theme.transitions.create('width', {
                        easing: theme.transitions.easing.sharp,
                        duration: theme.transitions.duration.leavingScreen,
                    }),
                    ...(open && {
                        transition: (theme) => theme.transitions.create('width', {
                            easing: theme.transitions.easing.easeOut,
                            duration: theme.transitions.duration.enteringScreen,
                        }),
                    }),
                }}
            >
                <Toolbar />
                <Outlet />
            </Box>
        </Box>
    );
}

export default Layout;
