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
    Tabs,
    Tab,
    Button,
    Chip,
} from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import AssessmentIcon from '@mui/icons-material/Assessment';
import { reportsApi } from '../api/endpoints';
import { formatCurrency, getErrorMessage } from '../utils/helpers';
import type {
    ProfitLossDto,
    SalesReportSummaryDto,
    GstReportDto,
    PaymentSummaryDto,
    InventoryValuationSummaryDto,
    TopProductDto,
} from '../types';

type ReportTab = 'profitLoss' | 'sales' | 'gst' | 'payments' | 'inventory' | 'topProducts';

function ReportsPage() {
    const today = new Date();
    const monthAgo = new Date();
    monthAgo.setDate(today.getDate() - 30);

    const [from, setFrom] = useState(monthAgo.toISOString().split('T')[0]);
    const [to, setTo] = useState(today.toISOString().split('T')[0]);
    const [tab, setTab] = useState<ReportTab>('profitLoss');
    const [exporting, setExporting] = useState(false);

    const dateRange = { from, to };

    // ---- Queries (lazy per tab) ----
    const profitLossQuery = useQuery<ProfitLossDto>(
        ['report-profit-loss', from, to],
        () => reportsApi.profitLoss(dateRange),
        { enabled: tab === 'profitLoss' && !!from && !!to },
    );

    const salesQuery = useQuery<SalesReportSummaryDto>(
        ['report-sales', from, to],
        () => reportsApi.sales(dateRange),
        { enabled: tab === 'sales' && !!from && !!to },
    );

    const gstQuery = useQuery<GstReportDto>(
        ['report-gst', from, to],
        () => reportsApi.gst(dateRange),
        { enabled: tab === 'gst' && !!from && !!to },
    );

    const paymentsQuery = useQuery<PaymentSummaryDto>(
        ['report-payments', from, to],
        () => reportsApi.payments(dateRange),
        { enabled: tab === 'payments' && !!from && !!to },
    );

    const inventoryQuery = useQuery<InventoryValuationSummaryDto>(
        ['report-inventory-valuation'],
        () => reportsApi.inventoryValuation(),
        { enabled: tab === 'inventory' },
    );

    const topProductsQuery = useQuery<TopProductDto[]>(
        ['report-top-products', from, to],
        () => reportsApi.topProducts(dateRange),
        { enabled: tab === 'topProducts' && !!from && !!to },
    );

    // ---- CSV download helper ----
    const downloadBlob = (data: unknown, filename: string) => {
        const blob = new Blob([data as BlobPart], { type: 'text/csv;charset=utf-8;' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', filename);
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    };

    const handleExport = async (type: 'sales' | 'gst' | 'inventory') => {
        setExporting(true);
        try {
            let response;
            let filename;
            if (type === 'sales') {
                response = await reportsApi.salesExport(dateRange);
                filename = `sales-report_${from}_to_${to}.csv`;
            } else if (type === 'gst') {
                response = await reportsApi.gstExport(dateRange);
                filename = `gst-report_${from}_to_${to}.csv`;
            } else {
                response = await reportsApi.inventoryValuationExport();
                filename = `inventory-valuation_${today.toISOString().split('T')[0]}.csv`;
            }
            downloadBlob(response.data, filename);
        } catch (err) {
            // eslint-disable-next-line no-console
            console.error('Export failed:', getErrorMessage(err));
        } finally {
            setExporting(false);
        }
    };

    const getStatusChip = (status: string) => {
        const color: 'success' | 'error' | 'warning' | 'default' =
            status === 'Completed' ? 'success' :
                status === 'Cancelled' ? 'error' :
                    status === 'Pending' ? 'warning' : 'default';
        return <Chip label={status} size="small" color={color} />;
    };

    const getPaymentStatusChip = (status: string) => {
        const color: 'success' | 'error' | 'warning' | 'default' =
            status === 'Paid' ? 'success' :
                status === 'Unpaid' ? 'error' :
                    status === 'PartiallyPaid' ? 'warning' : 'default';
        return <Chip label={status} size="small" color={color} />;
    };

    const renderDateFilters = () => (
        <Box sx={{ display: 'flex', gap: 2, mb: 2, alignItems: 'center', flexWrap: 'wrap' }}>
            <TextField
                label="From Date"
                type="date"
                size="small"
                value={from}
                onChange={(e) => setFrom(e.target.value)}
                InputLabelProps={{ shrink: true }}
                sx={{ width: 180 }}
            />
            <TextField
                label="To Date"
                type="date"
                size="small"
                value={to}
                onChange={(e) => setTo(e.target.value)}
                InputLabelProps={{ shrink: true }}
                sx={{ width: 180 }}
            />
        </Box>
    );

    const renderLoading = () => (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress />
        </Box>
    );

    const renderError = (msg: string) => (
        <Alert severity="error" sx={{ mt: 2 }}>{msg}</Alert>
    );

    // ---- Tab panels ----
    const renderProfitLoss = () => {
        if (profitLossQuery.isLoading) return renderLoading();
        if (profitLossQuery.error) return renderError(getErrorMessage(profitLossQuery.error, 'Failed to load P&L report'));
        const d = profitLossQuery.data;
        if (!d) return null;
        return (
            <Grid container spacing={3}>
                <Grid item xs={12} sm={6} md={4}>
                    <Card><CardContent>
                        <Typography color="textSecondary" variant="body2">Revenue</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700, color: 'success.main' }}>{formatCurrency(d.revenue)}</Typography>
                    </CardContent></Card>
                </Grid>
                <Grid item xs={12} sm={6} md={4}>
                    <Card><CardContent>
                        <Typography color="textSecondary" variant="body2">Cost of Goods Sold</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700, color: 'error.main' }}>{formatCurrency(d.costOfGoods)}</Typography>
                    </CardContent></Card>
                </Grid>
                <Grid item xs={12} sm={6} md={4}>
                    <Card><CardContent>
                        <Typography color="textSecondary" variant="body2">Expenses</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700, color: 'error.main' }}>{formatCurrency(d.expenses)}</Typography>
                    </CardContent></Card>
                </Grid>
                <Grid item xs={12} sm={6} md={4}>
                    <Card><CardContent>
                        <Typography color="textSecondary" variant="body2">Discount</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700, color: 'warning.main' }}>{formatCurrency(d.discountAmount)}</Typography>
                    </CardContent></Card>
                </Grid>
                <Grid item xs={12} sm={6} md={4}>
                    <Card><CardContent>
                        <Typography color="textSecondary" variant="body2">Gross Profit</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700 }}>{formatCurrency(d.grossProfit)}</Typography>
                    </CardContent></Card>
                </Grid>
                <Grid item xs={12} sm={6} md={4}>
                    <Card><CardContent>
                        <Typography color="textSecondary" variant="body2">Net Profit</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700, color: d.netProfit >= 0 ? 'success.main' : 'error.main' }}>
                            {formatCurrency(d.netProfit)}
                        </Typography>
                    </CardContent></Card>
                </Grid>
            </Grid>
        );
    };

    const renderSales = () => {
        if (salesQuery.isLoading) return renderLoading();
        if (salesQuery.error) return renderError(getErrorMessage(salesQuery.error, 'Failed to load sales report'));
        const d = salesQuery.data;
        if (!d) return null;
        return (
            <Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <Chip label={`Total: ${formatCurrency(d.totalGrandTotal)}`} color="primary" />
                        <Chip label={`Count: ${d.totalCount}`} variant="outlined" />
                        <Chip label={`Tax: ${formatCurrency(d.totalTax)}`} variant="outlined" />
                    </Box>
                    <Button
                        variant="outlined"
                        startIcon={<DownloadIcon />}
                        disabled={exporting}
                        onClick={() => handleExport('sales')}
                    >
                        Export CSV
                    </Button>
                </Box>
                <TableContainer component={Paper} variant="outlined">
                    <Table size="small">
                        <TableHead>
                            <TableRow>
                                <TableCell>Date</TableCell>
                                <TableCell>Invoice #</TableCell>
                                <TableCell>Customer</TableCell>
                                <TableCell align="right">Subtotal</TableCell>
                                <TableCell align="right">Tax</TableCell>
                                <TableCell align="right">Total</TableCell>
                                <TableCell>Status</TableCell>
                                <TableCell>Payment</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {d.sales.map((s, idx) => (
                                <TableRow key={idx}>
                                    <TableCell>{new Date(s.saleDate).toLocaleDateString()}</TableCell>
                                    <TableCell>{s.invoiceNumber}</TableCell>
                                    <TableCell>{s.customerName || 'Walk-in'}</TableCell>
                                    <TableCell align="right">{formatCurrency(s.subTotal)}</TableCell>
                                    <TableCell align="right">{formatCurrency(s.taxAmount)}</TableCell>
                                    <TableCell align="right">{formatCurrency(s.grandTotal)}</TableCell>
                                    <TableCell>{getStatusChip(s.status)}</TableCell>
                                    <TableCell>{getPaymentStatusChip(s.paymentStatus)}</TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>
            </Box>
        );
    };

    const renderGst = () => {
        if (gstQuery.isLoading) return renderLoading();
        if (gstQuery.error) return renderError(getErrorMessage(gstQuery.error, 'Failed to load GST report'));
        const d = gstQuery.data;
        if (!d) return null;
        return (
            <Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <Chip label={`Total Taxable: ${formatCurrency(d.totalTaxableAmount)}`} color="primary" />
                        <Chip label={`Total Tax: ${formatCurrency(d.totalTaxAmount)}`} color="secondary" />
                        <Chip label={`Invoices: ${d.totalInvoices}`} variant="outlined" />
                    </Box>
                    <Button
                        variant="outlined"
                        startIcon={<DownloadIcon />}
                        disabled={exporting}
                        onClick={() => handleExport('gst')}
                    >
                        Export CSV
                    </Button>
                </Box>
                <TableContainer component={Paper} variant="outlined">
                    <Table size="small">
                        <TableHead>
                            <TableRow>
                                <TableCell>Tax Rate (%)</TableCell>
                                <TableCell align="right">Taxable Amount</TableCell>
                                <TableCell align="right">Tax Amount</TableCell>
                                <TableCell align="right">Invoice Count</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {d.rateBreakdown.map((r, idx) => (
                                <TableRow key={idx}>
                                    <TableCell>{r.taxRate}%</TableCell>
                                    <TableCell align="right">{formatCurrency(r.taxableAmount)}</TableCell>
                                    <TableCell align="right">{formatCurrency(r.taxAmount)}</TableCell>
                                    <TableCell align="right">{r.invoiceCount}</TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>
            </Box>
        );
    };

    const renderPayments = () => {
        if (paymentsQuery.isLoading) return renderLoading();
        if (paymentsQuery.error) return renderError(getErrorMessage(paymentsQuery.error, 'Failed to load payment summary'));
        const d = paymentsQuery.data;
        if (!d) return null;
        return (
            <Box>
                <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                    <Chip label={`Total: ${formatCurrency(d.totalAmount)}`} color="primary" />
                    <Chip label={`Transactions: ${d.totalTransactions}`} variant="outlined" />
                </Box>
                <TableContainer component={Paper} variant="outlined">
                    <Table size="small">
                        <TableHead>
                            <TableRow>
                                <TableCell>Payment Method</TableCell>
                                <TableCell align="right">Total Amount</TableCell>
                                <TableCell align="right">Transaction Count</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {d.methods.map((m, idx) => (
                                <TableRow key={idx}>
                                    <TableCell>{m.paymentMethod}</TableCell>
                                    <TableCell align="right">{formatCurrency(m.totalAmount)}</TableCell>
                                    <TableCell align="right">{m.transactionCount}</TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>
            </Box>
        );
    };

    const renderInventory = () => {
        if (inventoryQuery.isLoading) return renderLoading();
        if (inventoryQuery.error) return renderError(getErrorMessage(inventoryQuery.error, 'Failed to load inventory valuation'));
        const d = inventoryQuery.data;
        if (!d) return null;
        return (
            <Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <Chip label={`Total Stock Value: ${formatCurrency(d.totalStockValue)}`} color="primary" />
                        <Chip label={`Products: ${d.productCount}`} variant="outlined" />
                    </Box>
                    <Button
                        variant="outlined"
                        startIcon={<DownloadIcon />}
                        disabled={exporting}
                        onClick={() => handleExport('inventory')}
                    >
                        Export CSV
                    </Button>
                </Box>
                <TableContainer component={Paper} variant="outlined">
                    <Table size="small">
                        <TableHead>
                            <TableRow>
                                <TableCell>Product</TableCell>
                                <TableCell>SKU</TableCell>
                                <TableCell align="right">Current Stock</TableCell>
                                <TableCell align="right">Cost Price</TableCell>
                                <TableCell align="right">Stock Value</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {d.items.map((item) => (
                                <TableRow key={item.productId}>
                                    <TableCell>{item.productName}</TableCell>
                                    <TableCell>{item.sku || '-'}</TableCell>
                                    <TableCell align="right">{item.currentStock}</TableCell>
                                    <TableCell align="right">{formatCurrency(item.costPrice)}</TableCell>
                                    <TableCell align="right">{formatCurrency(item.stockValue)}</TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>
            </Box>
        );
    };

    const renderTopProducts = () => {
        if (topProductsQuery.isLoading) return renderLoading();
        if (topProductsQuery.error) return renderError(getErrorMessage(topProductsQuery.error, 'Failed to load top products'));
        const d = topProductsQuery.data;
        if (!d || d.length === 0) return <Alert severity="info">No sales data for this period.</Alert>;
        return (
            <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                    <TableHead>
                        <TableRow>
                            <TableCell>Rank</TableCell>
                            <TableCell>Product</TableCell>
                            <TableCell align="right">Qty Sold</TableCell>
                            <TableCell align="right">Revenue</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {d.map((p, idx) => (
                            <TableRow key={p.productId}>
                                <TableCell>{idx + 1}</TableCell>
                                <TableCell>{p.productName}</TableCell>
                                <TableCell align="right">{p.quantitySold}</TableCell>
                                <TableCell align="right">{formatCurrency(p.revenue)}</TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        );
    };

    const showDateFilters = tab !== 'inventory';

    return (
        <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                <AssessmentIcon sx={{ mr: 1, fontSize: 32, color: 'primary.main' }} />
                <Typography variant="h4" component="h1">Reports</Typography>
            </Box>

            {showDateFilters && renderDateFilters()}

            <Card sx={{ mb: 2 }}>
                <CardContent>
                    <Tabs
                        value={tab}
                        onChange={(_, v) => setTab(v as ReportTab)}
                        variant="scrollable"
                        scrollButtons="auto"
                    >
                        <Tab label="Profit & Loss" value="profitLoss" />
                        <Tab label="Sales" value="sales" />
                        <Tab label="GST" value="gst" />
                        <Tab label="Payments" value="payments" />
                        <Tab label="Inventory Valuation" value="inventory" />
                        <Tab label="Top Products" value="topProducts" />
                    </Tabs>
                </CardContent>
            </Card>

            <Box sx={{ mt: 2 }}>
                {tab === 'profitLoss' && renderProfitLoss()}
                {tab === 'sales' && renderSales()}
                {tab === 'gst' && renderGst()}
                {tab === 'payments' && renderPayments()}
                {tab === 'inventory' && renderInventory()}
                {tab === 'topProducts' && renderTopProducts()}
            </Box>
        </Box>
    );
}

export default ReportsPage;
