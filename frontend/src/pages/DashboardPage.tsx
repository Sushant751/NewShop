import { useState } from 'react';
import { useQuery } from 'react-query';
import { useNavigate } from 'react-router-dom';
import {
    Box,
    Card,
    CardContent,
    Grid,
    Typography,
    CircularProgress,
    Alert,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    TextField,
    Chip,
    Button,
    IconButton,
    Tooltip,
} from '@mui/material';
import {
    BarChart,
    Bar,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip as RechartsTooltip,
    ResponsiveContainer,
    AreaChart,
    Area,
    Legend,
} from 'recharts';
import AttachMoneyIcon from '@mui/icons-material/AttachMoney';
import ShoppingCartIcon from '@mui/icons-material/ShoppingCartOutlined';
import InventoryIcon from '@mui/icons-material/Inventory2Outlined';
import PeopleIcon from '@mui/icons-material/PeopleOutline';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import DownloadIcon from '@mui/icons-material/DownloadOutlined';
import StorefrontIcon from '@mui/icons-material/Storefront';
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong';
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import OpenInNewIcon from '@mui/icons-material/OpenInNew';
import AccountBalanceWalletIcon from '@mui/icons-material/AccountBalanceWalletOutlined';
import { dashboardApi } from '../api/endpoints';
import type { DashboardDto, ShopMetricsDto } from '../types';
import { formatCurrency, getErrorMessage } from '../utils/helpers';
import { useAppSelector, RootState } from '../store';
import { Roles } from '../types';

interface MantisStatCardProps {
    title: string;
    value: string;
    /** Pass null to show 'no comparison data' state */
    pctChange: number | null;
    icon: React.ReactNode;
    color: string;
    bgTint: string;
    subtitle?: string;
}

function MantisStatCard({ title, value, pctChange, icon, color, bgTint, subtitle }: MantisStatCardProps) {
    const hasData = pctChange !== null;
    const isPositive = hasData ? pctChange >= 0 : true;
    const trendLabel = hasData
        ? `${pctChange >= 0 ? '+' : ''}${pctChange.toFixed(1)}%`
        : '—';

    return (
        <Card
            sx={{
                borderRadius: 2.5,
                border: '1px solid #e6ebf1',
                boxShadow: '0px 2px 8px rgba(32, 40, 45, 0.04)',
                transition: 'transform 0.2s ease, box-shadow 0.2s ease',
                '&:hover': {
                    transform: 'translateY(-3px)',
                    boxShadow: '0px 8px 20px rgba(32, 40, 45, 0.08)',
                },
            }}
        >
            <CardContent sx={{ p: 2.5, '&:last-child': { pb: 2.5 } }}>
                <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', mb: 1.5 }}>
                    <Box>
                        <Typography variant="body2" sx={{ color: '#5b6b79', fontWeight: 500, mb: 0.5 }}>
                            {title}
                        </Typography>
                        <Typography variant="h4" sx={{ fontWeight: 700, color: '#1d2630' }}>
                            {value}
                        </Typography>
                        {subtitle && (
                            <Typography variant="caption" sx={{ color: '#8c8c8c', display: 'block', mt: 0.3 }}>
                                {subtitle}
                            </Typography>
                        )}
                    </Box>
                    <Box
                        sx={{
                            bgcolor: bgTint,
                            color: color,
                            borderRadius: 2,
                            p: 1.2,
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                        }}
                    >
                        {icon}
                    </Box>
                </Box>

                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.8, mt: 1 }}>
                    {hasData ? (
                        <Chip
                            icon={isPositive ? <ArrowUpwardIcon sx={{ fontSize: '12px !important' }} /> : <ArrowDownwardIcon sx={{ fontSize: '12px !important' }} />}
                            label={trendLabel}
                            size="small"
                            sx={{
                                height: 20,
                                fontSize: '0.6875rem',
                                fontWeight: 700,
                                bgcolor: isPositive ? '#f6ffed' : '#fff2f0',
                                color: isPositive ? '#52c41a' : '#ff4d4f',
                                border: `1px solid ${isPositive ? '#b7eb8f' : '#ffa39e'}`,
                            }}
                        />
                    ) : (
                        <Typography variant="caption" sx={{ color: '#8c8c8c', fontStyle: 'italic' }}>
                            {subtitle ? 'Current Period' : 'No prior period data'}
                        </Typography>
                    )}
                    {hasData && (
                        <Typography variant="caption" sx={{ color: '#8c8c8c' }}>vs last period</Typography>
                    )}
                </Box>
            </CardContent>
        </Card>
    );
}

// Computes % change from previous to current. Returns null when previous = 0 (no comparison possible).
function pctDiff(current: number, previous: number): number | null {
    if (previous === 0) return null;
    return ((current - previous) / previous) * 100;
}

function DashboardPage() {
    const navigate = useNavigate();
    const currentUser = useAppSelector((state: RootState) => state.auth.user);
    const isGlobalAdmin = currentUser?.roles?.includes(Roles.GlobalAdmin);

    const today = new Date();
    const monthAgo = new Date();
    monthAgo.setDate(today.getDate() - 30);

    const [from, setFrom] = useState(monthAgo.toISOString().split('T')[0]);
    const [to, setTo] = useState(today.toISOString().split('T')[0]);

    // Previous period: same duration, immediately before the selected range
    const prevFrom = new Date(from);
    const prevTo = new Date(from);
    const rangeDays = Math.max(1, Math.round((new Date(to).getTime() - new Date(from).getTime()) / 86400000));
    prevFrom.setDate(prevFrom.getDate() - rangeDays);

    const prevFromStr = prevFrom.toISOString().split('T')[0];
    const prevToStr = prevTo.toISOString().split('T')[0];

    const { data, isLoading, error } = useQuery<DashboardDto>(
        ['dashboard', from, to, isGlobalAdmin ? 'global' : currentUser?.tenantId],
        () => dashboardApi.getSummary(from, to),
        { keepPreviousData: true },
    );

    const { data: prevData } = useQuery<DashboardDto>(
        ['dashboard-prev', prevFromStr, prevToStr, isGlobalAdmin ? 'global' : currentUser?.tenantId],
        () => dashboardApi.getSummary(prevFromStr, prevToStr),
        { keepPreviousData: true },
    );

    if (isLoading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '60vh' }}>
                <CircularProgress />
            </Box>
        );
    }

    if (error) {
        return (
            <Alert severity="error" sx={{ my: 2 }}>
                {getErrorMessage(error, 'Failed to load dashboard data. Please try again.')}
            </Alert>
        );
    }

    const dailySalesData = (data?.dailySales || []).map((item) => ({
        date: new Date(item.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
        sales: item.totalSales,
        orders: item.salesCount,
    }));

    const topProductsData = data?.topProducts || [];
    const shopMetrics: ShopMetricsDto[] = data?.shopMetrics || [];

    const shopRevenueChartData = shopMetrics.map((shop) => ({
        name: shop.tenantName,
        revenue: shop.totalRevenue,
        bills: shop.totalBillsGenerated,
        paidBills: shop.paidBillsCount,
        cancelledBills: shop.cancelledBillsCount,
    }));

    const handleExportCsv = () => {
        if (!data) return;

        if (isGlobalAdmin && shopMetrics.length > 0) {
            // Export Shop-wise breakdown for Global Admin (Platform Administration)
            const headers = ['Shop Name', 'Slug', 'Plan', 'Status', 'Staff Count', 'Products Count'];
            const rows = shopMetrics.map((s) => [
                `"${s.tenantName}"`,
                s.tenantSlug,
                s.plan || 'Standard',
                s.status,
                s.userCount,
                s.productCount,
            ]);
            const csv = [headers.join(','), ...rows.map((r) => r.join(','))].join('\n');
            const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Shop_Network_Report_${from}_to_${to}.csv`;
            a.click();
            URL.revokeObjectURL(url);
            return;
        }

        // Export Daily Sales for Shop Admin
        const headers = ['Date', 'Sales Count', 'Total Revenue'];
        const rows = (data.dailySales || []).map((d) => [
            d.date.split('T')[0],
            d.salesCount,
            d.totalSales.toFixed(2),
        ]);
        const csv = [headers.join(','), ...rows.map((r) => r.join(','))].join('\n');
        const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Sales_Summary_${from}_to_${to}.csv`;
        a.click();
        URL.revokeObjectURL(url);
    };

    return (
        <Box sx={{ p: { xs: 2, md: 3 } }}>
            {/* Header with Title & Date Controls */}
            <Box
                sx={{
                    display: 'flex',
                    flexDirection: { xs: 'column', sm: 'row' },
                    justifyContent: 'space-between',
                    alignItems: { xs: 'flex-start', sm: 'center' },
                    gap: 2,
                    mb: 3,
                }}
            >
                <Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                        <Typography variant="h5" sx={{ fontWeight: 700, color: '#1d2630' }}>
                            {isGlobalAdmin ? 'Platform Executive Dashboard' : 'Store Dashboard'}
                        </Typography>
                        {isGlobalAdmin ? (
                            <Chip
                                label="Multi-Shop Network"
                                size="small"
                                sx={{ bgcolor: '#e8f0ff', color: '#4680ff', fontWeight: 700, fontSize: '0.75rem' }}
                            />
                        ) : (
                            <Chip
                                label={currentUser?.tenantName || 'Demo Shop'}
                                size="small"
                                sx={{ bgcolor: '#f6ffed', color: '#52c41a', fontWeight: 600, fontSize: '0.75rem' }}
                            />
                        )}
                    </Box>
                    <Typography variant="body2" sx={{ color: '#5b6b79', mt: 0.5 }}>
                        {isGlobalAdmin
                            ? 'Consolidated cross-shop metrics, staff allocations, and bill generation & cancellation tracking'
                            : 'Real-time store performance, revenue metrics, and inventory overview'}
                    </Typography>
                </Box>

                {/* Filters & Export */}
                <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center', flexWrap: 'wrap' }}>
                    <TextField
                        type="date"
                        label="From"
                        size="small"
                        value={from}
                        onChange={(e) => setFrom(e.target.value)}
                        InputLabelProps={{ shrink: true }}
                        sx={{ bgcolor: '#fff', borderRadius: 1.5, '& .MuiOutlinedInput-root': { borderRadius: 1.5 } }}
                    />
                    <TextField
                        type="date"
                        label="To"
                        size="small"
                        value={to}
                        onChange={(e) => setTo(e.target.value)}
                        InputLabelProps={{ shrink: true }}
                        sx={{ bgcolor: '#fff', borderRadius: 1.5, '& .MuiOutlinedInput-root': { borderRadius: 1.5 } }}
                    />
                    <Button
                        variant="outlined"
                        size="small"
                        startIcon={<DownloadIcon />}
                        onClick={handleExportCsv}
                        sx={{
                            borderRadius: 1.5,
                            textTransform: 'none',
                            fontWeight: 600,
                            height: 40,
                            borderColor: '#e6ebf1',
                            color: '#5b6b79',
                            bgcolor: '#fff',
                            '&:hover': { borderColor: '#4680ff', bgcolor: '#f8faff', color: '#4680ff' },
                        }}
                    >
                        Export CSV
                    </Button>
                </Box>
            </Box>

            {/* ========================================================================= */}
            {/* VIEW A: GLOBAL ADMIN MULTI-SHOP DASHBOARD                                  */}
            {/* ========================================================================= */}
            {isGlobalAdmin ? (
                <>
                    {/* Row 1: Platform Administrative Stat Cards */}
                    <Grid container spacing={2.5} sx={{ mb: 3.5 }}>
                        <Grid item xs={12} sm={6} md={4}>
                            <MantisStatCard
                                title="Active Shops / Tenants"
                                value={String(data?.totalShopsCount || shopMetrics.length || 0)}
                                pctChange={null}
                                subtitle="Registered System Tenants"
                                icon={<StorefrontIcon />}
                                color="#4680ff"
                                bgTint="#e8f0ff"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6} md={4}>
                            <MantisStatCard
                                title="Total Staff Users"
                                value={String(data?.totalUsersCount || 0)}
                                pctChange={null}
                                subtitle="Allocated Across All Shops"
                                icon={<PeopleIcon />}
                                color="#13c2c2"
                                bgTint="#e6fffb"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6} md={4}>
                            <MantisStatCard
                                title="Catalog Product SKUs"
                                value={String(data?.productCount || 0)}
                                pctChange={null}
                                subtitle="Total System Inventory SKUs"
                                icon={<InventoryIcon />}
                                color="#fa8c16"
                                bgTint="#fff7e6"
                            />
                        </Grid>
                    </Grid>

                    {/* Row 2: Comprehensive Shop Network & Staff Matrix Table */}
                    <Card sx={{ borderRadius: 2.5, border: '1px solid #e6ebf1' }}>
                        <CardContent sx={{ p: 3 }}>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2.5 }}>
                                <Box>
                                    <Typography variant="h6" sx={{ fontWeight: 700 }}>
                                        Platform Tenants & Staff Allocation Matrix
                                    </Typography>
                                    <Typography variant="body2" color="textSecondary">
                                        Registered shop branches, active staff allocations, product inventory counts, and status
                                    </Typography>
                                </Box>
                                <Button
                                    variant="outlined"
                                    size="small"
                                    startIcon={<PeopleIcon />}
                                    onClick={() => navigate('/staff')}
                                    sx={{ borderRadius: 2, textTransform: 'none', fontWeight: 600 }}
                                >
                                    Manage All Staff
                                </Button>
                            </Box>

                            <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 2, borderColor: '#e6ebf1' }}>
                                <Table>
                                    <TableHead>
                                        <TableRow sx={{ bgcolor: '#fafafa' }}>
                                            <TableCell sx={{ fontWeight: 700 }}>Shop / Branch</TableCell>
                                            <TableCell align="center" sx={{ fontWeight: 700 }}>Staff Users</TableCell>
                                            <TableCell align="center" sx={{ fontWeight: 700 }}>Product Catalog</TableCell>
                                            <TableCell align="center" sx={{ fontWeight: 700 }}>Subscription Plan</TableCell>
                                            <TableCell align="center" sx={{ fontWeight: 700 }}>Status</TableCell>
                                            <TableCell align="center" sx={{ fontWeight: 700 }}>Action</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {shopMetrics.length === 0 ? (
                                            <TableRow>
                                                <TableCell colSpan={6} align="center" sx={{ py: 3, color: '#8c8c8c' }}>
                                                    No shops registered yet.
                                                </TableCell>
                                            </TableRow>
                                        ) : (
                                            shopMetrics.map((shop) => (
                                                <TableRow key={shop.tenantId} hover>
                                                    <TableCell>
                                                        <Box>
                                                            <Typography variant="body2" sx={{ fontWeight: 700, color: '#1d2630' }}>
                                                                {shop.tenantName}
                                                            </Typography>
                                                            <Typography variant="caption" sx={{ color: '#8c8c8c' }}>
                                                                {shop.tenantSlug}
                                                            </Typography>
                                                        </Box>
                                                    </TableCell>

                                                    <TableCell align="center">
                                                        <Chip
                                                            icon={<PeopleIcon sx={{ fontSize: '14px !important' }} />}
                                                            label={`${shop.userCount} users`}
                                                            size="small"
                                                            sx={{ bgcolor: '#e8f0ff', color: '#4680ff', fontWeight: 600 }}
                                                        />
                                                    </TableCell>

                                                    <TableCell align="center" sx={{ fontWeight: 600 }}>
                                                        {shop.productCount} SKUs
                                                    </TableCell>

                                                    <TableCell align="center">
                                                        <Chip
                                                            label={shop.plan || 'Standard'}
                                                            size="small"
                                                            sx={{ height: 20, fontSize: '0.7rem', bgcolor: '#f0f5ff', color: '#2f54eb', fontWeight: 600 }}
                                                        />
                                                    </TableCell>

                                                    <TableCell align="center">
                                                        <Chip
                                                            label={shop.status}
                                                            size="small"
                                                            sx={{
                                                                bgcolor: shop.status === 'Active' ? '#f6ffed' : '#fff2f0',
                                                                color: shop.status === 'Active' ? '#52c41a' : '#ff4d4f',
                                                                fontWeight: 600,
                                                            }}
                                                        />
                                                    </TableCell>

                                                    <TableCell align="center">
                                                        <Tooltip title="View / Manage Shop Staff">
                                                            <IconButton
                                                                size="small"
                                                                onClick={() => navigate('/staff')}
                                                                sx={{ color: '#4680ff', '&:hover': { bgcolor: '#e8f0ff' } }}
                                                            >
                                                                <OpenInNewIcon fontSize="small" />
                                                            </IconButton>
                                                        </Tooltip>
                                                    </TableCell>
                                                </TableRow>
                                            ))
                                        )}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        </CardContent>
                    </Card>
                </>
            ) : (
                /* ========================================================================= */
                /* VIEW B: STORE ADMIN & CASHIER OPERATIONAL DASHBOARD                        */
                /* ========================================================================= */
                <>
                    {/* Top Row KPI Cards */}
                    <Grid container spacing={2.5} sx={{ mb: 3.5 }}>
                        <Grid item xs={12} sm={6} md={3}>
                            <MantisStatCard
                                title="Total Sales Revenue"
                                value={formatCurrency(data?.totalSales || 0)}
                                pctChange={pctDiff(data?.totalSales || 0, prevData?.totalSales || 0)}
                                icon={<AttachMoneyIcon />}
                                color="#4680ff"
                                bgTint="#e8f0ff"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                            <MantisStatCard
                                title="Total Invoices"
                                value={String(data?.salesCount || 0)}
                                pctChange={pctDiff(data?.salesCount || 0, prevData?.salesCount || 0)}
                                icon={<ReceiptLongIcon />}
                                color="#52c41a"
                                bgTint="#f6ffed"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                            <MantisStatCard
                                title="Catalog Products"
                                value={String(data?.productCount || 0)}
                                pctChange={pctDiff(data?.productCount || 0, prevData?.productCount || 0)}
                                icon={<InventoryIcon />}
                                color="#13c2c2"
                                bgTint="#e6fffb"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                            <MantisStatCard
                                title="Active Customers"
                                value={String(data?.customerCount || 0)}
                                pctChange={pctDiff(data?.customerCount || 0, prevData?.customerCount || 0)}
                                icon={<PeopleIcon />}
                                color="#722ed1"
                                bgTint="#f9f0ff"
                            />
                        </Grid>
                    </Grid>

                    {/* Secondary Metrics */}
                    <Grid container spacing={2.5} sx={{ mb: 3.5 }}>
                        <Grid item xs={12} sm={6} md={4}>
                            <MantisStatCard
                                title="Total Purchases"
                                value={formatCurrency(data?.totalPurchases || 0)}
                                pctChange={pctDiff(data?.totalPurchases || 0, prevData?.totalPurchases || 0)}
                                icon={<ShoppingCartIcon />}
                                color="#fa8c16"
                                bgTint="#fff7e6"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6} md={4}>
                            <MantisStatCard
                                title="Operating Expenses"
                                value={formatCurrency(data?.totalExpenses || 0)}
                                pctChange={pctDiff(data?.totalExpenses || 0, prevData?.totalExpenses || 0)}
                                icon={<AttachMoneyIcon />}
                                color="#ff4d4f"
                                bgTint="#fff2f0"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6} md={4}>
                            <MantisStatCard
                                title="Discount Amount"
                                value={formatCurrency(data?.totalDiscountAmount || 0)}
                                pctChange={pctDiff(data?.totalDiscountAmount || 0, prevData?.totalDiscountAmount || 0)}
                                icon={<AttachMoneyIcon />}
                                color="#faad14"
                                bgTint="#fffbe6"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6} md={4}>
                            { !isGlobalAdmin && (
                                <MantisStatCard
                                    title="Net Profit"
                                    value={formatCurrency(data?.totalProfit || 0)}
                                    pctChange={pctDiff(data?.totalProfit || 0, prevData?.totalProfit || 0)}
                                    icon={<AccountBalanceWalletIcon />}
                                    color="#52c41a"
                                    bgTint="#f6ffed"
                                />
                            ) }
                        </Grid>
                    </Grid>

                    {/* Charts Section */}
                    <Grid container spacing={2.5} sx={{ mb: 3.5 }}>
                        <Grid item xs={12} md={8}>
                            <Card sx={{ borderRadius: 2.5, border: '1px solid #e6ebf1', p: 1 }}>
                                <CardContent>
                                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                                        <Box>
                                            <Typography variant="h6" sx={{ fontWeight: 700 }}>
                                                Revenue Overview
                                            </Typography>
                                            <Typography variant="caption" color="textSecondary">
                                                Daily sales revenue trend (₹) over selected timeframe
                                            </Typography>
                                        </Box>
                                        <Chip label="Daily Trend" size="small" sx={{ bgcolor: '#e8f0ff', color: '#4680ff', fontWeight: 600 }} />
                                    </Box>

                                    <ResponsiveContainer width="100%" height={320}>
                                        <AreaChart data={dailySalesData}>
                                            <defs>
                                                <linearGradient id="salesGradient" x1="0" y1="0" x2="0" y2="1">
                                                    <stop offset="5%" stopColor="#4680ff" stopOpacity={0.4} />
                                                    <stop offset="95%" stopColor="#4680ff" stopOpacity={0.0} />
                                                </linearGradient>
                                            </defs>
                                            <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                                            <XAxis dataKey="date" fontSize={11} stroke="#8c8c8c" />
                                            <YAxis fontSize={11} stroke="#8c8c8c" />
                                            <RechartsTooltip
                                                contentStyle={{
                                                    backgroundColor: '#ffffff',
                                                    borderRadius: '8px',
                                                    border: '1px solid #e6ebf1',
                                                    boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
                                                }}
                                            />
                                            <Area
                                                type="monotone"
                                                dataKey="sales"
                                                stroke="#4680ff"
                                                strokeWidth={2.5}
                                                fillOpacity={1}
                                                fill="url(#salesGradient)"
                                                name="Revenue (₹)"
                                            />
                                        </AreaChart>
                                    </ResponsiveContainer>
                                </CardContent>
                            </Card>
                        </Grid>

                        <Grid item xs={12} md={4}>
                            <Card sx={{ borderRadius: 2.5, border: '1px solid #e6ebf1', p: 1 }}>
                                <CardContent>
                                    <Box sx={{ mb: 2 }}>
                                        <Typography variant="h6" sx={{ fontWeight: 700 }}>
                                            Top Product Revenue
                                        </Typography>
                                        <Typography variant="caption" color="textSecondary">
                                            Highest performing sales inventory
                                        </Typography>
                                    </Box>

                                    <ResponsiveContainer width="100%" height={320}>
                                        <BarChart data={topProductsData} layout="vertical">
                                            <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                                            <XAxis type="number" fontSize={11} stroke="#8c8c8c" />
                                            <YAxis
                                                type="category"
                                                dataKey="productName"
                                                fontSize={11}
                                                stroke="#8c8c8c"
                                                width={90}
                                            />
                                            <RechartsTooltip
                                                contentStyle={{
                                                    backgroundColor: '#ffffff',
                                                    borderRadius: '8px',
                                                    border: '1px solid #e6ebf1',
                                                }}
                                            />
                                            <Bar dataKey="revenue" fill="#4680ff" radius={[0, 6, 6, 0]} name="Revenue (₹)" />
                                        </BarChart>
                                    </ResponsiveContainer>
                                </CardContent>
                            </Card>
                        </Grid>
                    </Grid>

                    {/* Top Products Table */}
                    {topProductsData.length > 0 && (
                        <Card sx={{ borderRadius: 2.5, border: '1px solid #e6ebf1' }}>
                            <CardContent sx={{ p: 3 }}>
                                <Typography variant="h6" sx={{ fontWeight: 700, mb: 2 }}>
                                    Top Performing Products Summary
                                </Typography>
                                <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 2, borderColor: '#e6ebf1' }}>
                                    <Table>
                                        <TableHead>
                                            <TableRow sx={{ bgcolor: '#fafafa' }}>
                                                <TableCell sx={{ fontWeight: 700 }}>Product Name</TableCell>
                                                <TableCell align="right" sx={{ fontWeight: 700 }}>Quantity Sold</TableCell>
                                                <TableCell align="right" sx={{ fontWeight: 700 }}>Total Revenue</TableCell>
                                                <TableCell align="center" sx={{ fontWeight: 700 }}>Performance</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {topProductsData.map((product) => (
                                                <TableRow key={product.productId} hover>
                                                    <TableCell sx={{ fontWeight: 600, color: '#1d2630' }}>
                                                        {product.productName}
                                                    </TableCell>
                                                    <TableCell align="right" sx={{ fontWeight: 500 }}>{product.quantitySold}</TableCell>
                                                    <TableCell align="right" sx={{ fontWeight: 700, color: '#4680ff' }}>
                                                        {formatCurrency(product.revenue)}
                                                    </TableCell>
                                                    <TableCell align="center">
                                                        <Chip
                                                            label="High Demand"
                                                            size="small"
                                                            sx={{ bgcolor: '#f6ffed', color: '#52c41a', fontWeight: 600 }}
                                                        />
                                                    </TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            </CardContent>
                        </Card>
                    )}
                </>
            )}
        </Box>
    );
}

export default DashboardPage;
