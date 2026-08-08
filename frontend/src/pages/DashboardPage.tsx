import { useState } from 'react';
import { useQuery } from 'react-query';
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
} from 'recharts';
import AttachMoneyIcon from '@mui/icons-material/AttachMoney';
import ShoppingCartIcon from '@mui/icons-material/ShoppingCartOutlined';
import InventoryIcon from '@mui/icons-material/Inventory2Outlined';
import PeopleIcon from '@mui/icons-material/PeopleOutline';
import WarningIcon from '@mui/icons-material/WarningAmberOutlined';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import DownloadIcon from '@mui/icons-material/DownloadOutlined';
import { dashboardApi } from '../api/endpoints';
import type { DashboardDto } from '../types';

interface MantisStatCardProps {
    title: string;
    value: string;
    /** Pass null to show 'no comparison data' state */
    pctChange: number | null;
    icon: React.ReactNode;
    color: string;
    bgTint: string;
}

function MantisStatCard({ title, value, pctChange, icon, color, bgTint }: MantisStatCardProps) {
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
                        <Typography variant="caption" sx={{ color: '#8c8c8c', fontStyle: 'italic' }}>No prior period data</Typography>
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
    const today = new Date();
    const monthAgo = new Date();
    monthAgo.setDate(today.getDate() - 30);

    const [from, setFrom] = useState(monthAgo.toISOString().split('T')[0]);
    const [to, setTo] = useState(today.toISOString().split('T')[0]);

    // Previous period: same duration, immediately before the selected range
    const prevFrom = new Date(from);
    const prevTo = new Date(from); // previous period ends where current starts
    const rangeDays = Math.max(1, Math.round((new Date(to).getTime() - new Date(from).getTime()) / 86400000));
    prevFrom.setDate(prevFrom.getDate() - rangeDays);
    const prevFromStr = prevFrom.toISOString().split('T')[0];
    const prevToStr = prevTo.toISOString().split('T')[0];

    const { data, isLoading, error } = useQuery<DashboardDto>(
        ['dashboard', from, to],
        () => dashboardApi.get({ from, to }),
        { enabled: !!from && !!to },
    );

    // Fetch previous period silently (no loading spinner)
    const { data: prevData } = useQuery<DashboardDto>(
        ['dashboard', prevFromStr, prevToStr],
        () => dashboardApi.get({ from: prevFromStr, to: prevToStr }),
        { enabled: !!from && !!to },
    );

    if (isLoading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 400 }}>
                <CircularProgress sx={{ color: '#4680ff' }} />
            </Box>
        );
    }

    if (error) {
        return (
            <Alert severity="error" sx={{ mt: 2, borderRadius: 2 }}>
                {error instanceof Error ? error.message : 'Failed to load dashboard metrics'}
            </Alert>
        );
    }

    const formatCurrency = (val: number) =>
        new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(val);

    const dailySalesData =
        data?.dailySales?.map((d) => ({
            date: new Date(d.date).toLocaleDateString('en-US', { day: '2-digit', month: 'short' }),
            sales: d.totalSales,
            count: d.salesCount,
        })) || [];

    const topProductsData = data?.topProducts || [];

    return (
        <Box>
            {/* Mantis Header Row */}
            <Box
                sx={{
                    display: 'flex',
                    flexDirection: { xs: 'column', sm: 'row' },
                    justify: 'space-between',
                    alignItems: { xs: 'flex-start', sm: 'center' },
                    gap: 2,
                    mb: 3.5,
                }}
            >
                <Box>
                    <Typography variant="h4" sx={{ fontWeight: 700, color: '#1d2630' }}>
                        Dashboard Analytics
                    </Typography>
                    <Typography variant="body2" sx={{ color: '#5b6b79' }}>
                        Real-time overview of sales performance, inventory status, and revenue trends.
                    </Typography>
                </Box>

                <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center' }}>
                    <TextField
                        type="date"
                        label="From"
                        value={from}
                        onChange={(e) => setFrom(e.target.value)}
                        InputLabelProps={{ shrink: true }}
                        sx={{ width: 155 }}
                    />
                    <TextField
                        type="date"
                        label="To"
                        value={to}
                        onChange={(e) => setTo(e.target.value)}
                        InputLabelProps={{ shrink: true }}
                        sx={{ width: 155 }}
                    />
                    <Button
                        variant="outlined"
                        startIcon={<DownloadIcon />}
                        sx={{
                            borderColor: '#e6ebf1',
                            color: '#5b6b79',
                            bgcolor: '#ffffff',
                            '&:hover': { bgcolor: '#f8fafc', borderColor: '#4680ff' },
                        }}
                    >
                        Export
                    </Button>
                </Box>
            </Box>

            {/* Mantis Stat Cards Grid */}
            <Grid container spacing={2.5} sx={{ mb: 3.5 }}>
                <Grid item xs={12} sm={6} md={3}>
                    <MantisStatCard
                        title="Total Revenue"
                        value={formatCurrency(data?.totalSales || 0)}
                        pctChange={pctDiff(data?.totalSales || 0, prevData?.totalSales || 0)}
                        icon={<AttachMoneyIcon />}
                        color="#4680ff"
                        bgTint="#e8f0ff"
                    />
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                    <MantisStatCard
                        title="Net Profit"
                        value={formatCurrency(data?.totalProfit || 0)}
                        pctChange={pctDiff(data?.totalProfit || 0, prevData?.totalProfit || 0)}
                        icon={<TrendingUpIcon />}
                        color="#52c41a"
                        bgTint="#f6ffed"
                    />
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                    <MantisStatCard
                        title="Sales Count"
                        value={String(data?.salesCount || 0)}
                        pctChange={pctDiff(data?.salesCount || 0, prevData?.salesCount || 0)}
                        icon={<ShoppingCartIcon />}
                        color="#faad14"
                        bgTint="#fffbe6"
                    />
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                    <MantisStatCard
                        title="Low Stock Warning"
                        value={String(data?.lowStockCount || 0)}
                        pctChange={pctDiff(data?.lowStockCount || 0, prevData?.lowStockCount || 0)}
                        icon={<WarningIcon />}
                        color="#ff4d4f"
                        bgTint="#fff2f0"
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
                        color="#4680ff"
                        bgTint="#e8f0ff"
                    />
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                    <MantisStatCard
                        title="Total Purchases"
                        value={formatCurrency(data?.totalPurchases || 0)}
                        pctChange={pctDiff(data?.totalPurchases || 0, prevData?.totalPurchases || 0)}
                        icon={<ShoppingCartIcon />}
                        color="#13c2c2"
                        bgTint="#e6fffb"
                    />
                </Grid>
                <Grid item xs={12} sm={6} md={3}>
                    <MantisStatCard
                        title="Operating Expenses"
                        value={formatCurrency(data?.totalExpenses || 0)}
                        pctChange={pctDiff(data?.totalExpenses || 0, prevData?.totalExpenses || 0)}
                        icon={<AttachMoneyIcon />}
                        color="#ff4d4f"
                        bgTint="#fff2f0"
                    />
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

            {/* Top Products Table - Mantis Style */}
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
        </Box>
    );
}

export default DashboardPage;
