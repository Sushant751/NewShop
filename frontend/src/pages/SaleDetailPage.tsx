import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import {
    Box,
    Button,
    Card,
    CardContent,
    CircularProgress,
    Alert,
    Typography,
    Chip,
    Grid,
    Divider,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Paper,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    TextField,
    IconButton,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import CancelIcon from '@mui/icons-material/Cancel';
import PrintIcon from '@mui/icons-material/Print';
import { salesApi } from '../api/endpoints';
import { Permissions } from '../types';
import type { SaleDto } from '../types';
import { hasPermission } from '../components/ProtectedRoute';
import { useAppSelector } from '../store';
import { getErrorMessage } from '../utils/helpers';

function SaleDetailPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const user = useAppSelector((state) => state.auth.user);
    const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
    const [cancelReason, setCancelReason] = useState('');

    const { data: sale, isLoading, error } = useQuery<SaleDto>(
        ['sale', id],
        () => salesApi.getById(id!),
        { enabled: !!id },
    );

    const cancelMutation = useMutation(
        (reason: string) => salesApi.cancel(id!, { reason }),
        {
            onSuccess: () => {
                queryClient.invalidateQueries(['sale', id]);
                queryClient.invalidateQueries(['sales']);
                setCancelDialogOpen(false);
                setCancelReason('');
            },
        },
    );

    const formatCurrency = (val: number) => `₹${val.toFixed(2)}`;

    const getStatusChip = (status: string) => {
        const statusMap: Record<string, { label: string; color: 'success' | 'error' | 'warning' | 'default' }> = {
            Completed: { label: 'Completed', color: 'success' },
            Held: { label: 'Held', color: 'warning' },
            Draft: { label: 'Draft', color: 'default' },
            Cancelled: { label: 'Cancelled', color: 'error' },
            Returned: { label: 'Returned', color: 'default' },
        };
        const info = statusMap[status] || { label: status, color: 'default' as const };
        return <Chip label={info.label} size="small" color={info.color} variant="outlined" />;
    };

    const getPaymentChip = (status: string) => {
        const statusMap: Record<string, { label: string; color: 'success' | 'warning' | 'error' | 'default' }> = {
            Paid: { label: 'Paid', color: 'success' },
            Partial: { label: 'Partial', color: 'warning' },
            Unpaid: { label: 'Unpaid', color: 'error' },
            Refunded: { label: 'Refunded', color: 'default' },
        };
        const info = statusMap[status] || { label: status, color: 'default' as const };
        return <Chip label={info.label} size="small" color={info.color} variant="outlined" />;
    };

    const getPaymentMethodLabel = (method: string) => {
        const methodMap: Record<string, string> = {
            Cash: 'Cash',
            Card: 'Card',
            UPI: 'UPI',
            Wallet: 'Wallet',
            Credit: 'Credit',
            Split: 'Split',
        };
        return methodMap[method] || method;
    };

    const canCancel = hasPermission(user, Permissions.SalesCancel);
    const isCancellable = sale && sale.status !== 'Cancelled' && sale.status !== 'Returned';

    if (isLoading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
                <CircularProgress />
            </Box>
        );
    }

    if (error || !sale) {
        return (
            <Box>
                <Alert severity="error" sx={{ mb: 2 }}>
                    {getErrorMessage(error, 'Sale not found')}
                </Alert>
                <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/sales')}>
                    Back to Sales
                </Button>
            </Box>
        );
    }

    return (
        <>
        <style>
            {`
            @media print {
                body * {
                    visibility: hidden;
                }
                #printable-receipt, #printable-receipt * {
                    visibility: visible;
                }
                #printable-receipt {
                    position: absolute;
                    left: 0;
                    top: 0;
                    width: 80mm;
                    margin: 0;
                    padding: 10px;
                    font-family: monospace;
                    font-size: 12px;
                }
                @page { margin: 0; }
            }
            `}
        </style>
        
        {/* Hidden Printable Receipt */}
        <Box id="printable-receipt" sx={{ display: 'none', displayPrint: 'block' }}>
            <Box sx={{ textAlign: 'center', mb: 2 }}>
                <Typography variant="h6" fontWeight="bold">NEW SHOP</Typography>
                <Typography variant="body2">Invoice: {sale.invoiceNumber}</Typography>
                <Typography variant="body2">Date: {new Date(sale.saleDate).toLocaleString()}</Typography>
                <Typography variant="body2">Customer: {sale.customerName || 'Walk-in'}</Typography>
            </Box>
            <Divider sx={{ borderStyle: 'dashed', mb: 1 }} />
            
            <table style={{ width: '100%', marginBottom: '10px' }}>
                <thead>
                    <tr style={{ textAlign: 'left' }}>
                        <th>Item</th>
                        <th style={{ textAlign: 'center' }}>Qty</th>
                        <th style={{ textAlign: 'right' }}>Total</th>
                    </tr>
                </thead>
                <tbody>
                    {sale.items.map((item, idx) => (
                        <tr key={idx}>
                            <td>{item.productName}</td>
                            <td style={{ textAlign: 'center' }}>{item.quantity}</td>
                            <td style={{ textAlign: 'right' }}>{formatCurrency(item.lineTotal)}</td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <Divider sx={{ borderStyle: 'dashed', my: 1 }} />
            
            <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Typography variant="body2">Subtotal:</Typography>
                <Typography variant="body2">{formatCurrency(sale.subTotal)}</Typography>
            </Box>
            {sale.discountAmount > 0 && (
                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="body2">Discount:</Typography>
                    <Typography variant="body2">-{formatCurrency(sale.discountAmount)}</Typography>
                </Box>
            )}
            <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Typography variant="body2">Tax:</Typography>
                <Typography variant="body2">{formatCurrency(sale.taxAmount)}</Typography>
            </Box>
            <Divider sx={{ borderStyle: 'dashed', my: 1 }} />
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                <Typography variant="body2" fontWeight="bold">GRAND TOTAL:</Typography>
                <Typography variant="body2" fontWeight="bold">{formatCurrency(sale.grandTotal)}</Typography>
            </Box>
            
            <Box sx={{ textAlign: 'center', mt: 3 }}>
                <Typography variant="body2">Thank you for your business!</Typography>
            </Box>
        </Box>

        {/* Main UI */}
        <Box sx={{ displayPrint: 'none' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                <IconButton onClick={() => navigate('/sales')}>
                    <ArrowBackIcon />
                </IconButton>
                <Typography variant="h4" sx={{ flexGrow: 1 }}>
                    Sale {sale.invoiceNumber}
                </Typography>
                <Button
                    variant="outlined"
                    startIcon={<PrintIcon />}
                    onClick={() => window.print()}
                >
                    Print
                </Button>
                {canCancel && isCancellable && (
                    <Button
                        variant="contained"
                        color="error"
                        startIcon={<CancelIcon />}
                        onClick={() => setCancelDialogOpen(true)}
                    >
                        Cancel Sale
                    </Button>
                )}
            </Box>

            {cancelMutation.isError && (
                <Alert severity="error" sx={{ mb: 2 }}>
                    {getErrorMessage(cancelMutation.error, 'Failed to cancel sale')}
                </Alert>
            )}

            <Grid container spacing={3}>
                {/* Sale Info */}
                <Grid item xs={12} md={6}>
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                Sale Information
                            </Typography>
                            <Divider sx={{ mb: 2 }} />
                            <Grid container spacing={2}>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Invoice Number
                                    </Typography>
                                    <Typography variant="body1" gutterBottom>
                                        {sale.invoiceNumber}
                                    </Typography>
                                </Grid>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Date
                                    </Typography>
                                    <Typography variant="body1" gutterBottom>
                                        {new Date(sale.saleDate).toLocaleString()}
                                    </Typography>
                                </Grid>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Customer
                                    </Typography>
                                    <Typography variant="body1" gutterBottom>
                                        {sale.customerName || 'Walk-in Customer'}
                                    </Typography>
                                </Grid>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Status
                                    </Typography>
                                    <Box sx={{ mt: 0.5 }}>{getStatusChip(sale.status)}</Box>
                                </Grid>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Payment Status
                                    </Typography>
                                    <Box sx={{ mt: 0.5 }}>{getPaymentChip(sale.paymentStatus)}</Box>
                                </Grid>
                                {sale.notes && (
                                    <Grid item xs={12}>
                                        <Typography variant="body2" color="text.secondary">
                                            Notes
                                        </Typography>
                                        <Typography variant="body1">{sale.notes}</Typography>
                                    </Grid>
                                )}
                            </Grid>
                        </CardContent>
                    </Card>
                </Grid>

                {/* Payment Summary */}
                <Grid item xs={12} md={6}>
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                Payment Summary
                            </Typography>
                            <Divider sx={{ mb: 2 }} />
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                                <Typography variant="body1">Subtotal</Typography>
                                <Typography variant="body1">{formatCurrency(sale.subTotal)}</Typography>
                            </Box>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                                <Typography variant="body1">Discount</Typography>
                                <Typography variant="body1" color="error">
                                    -{formatCurrency(sale.discountAmount)}
                                </Typography>
                            </Box>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                                <Typography variant="body1">Tax</Typography>
                                <Typography variant="body1">{formatCurrency(sale.taxAmount)}</Typography>
                            </Box>
                            <Divider sx={{ my: 1 }} />
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                                <Typography variant="h6">Total</Typography>
                                <Typography variant="h6">{formatCurrency(sale.grandTotal)}</Typography>
                            </Box>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                                <Typography variant="body1">Paid</Typography>
                                <Typography variant="body1" color="success.main">
                                    {formatCurrency(sale.paidAmount)}
                                </Typography>
                            </Box>
                            {sale.balanceDue > 0 && (
                                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                                    <Typography variant="body1">Balance Due</Typography>
                                    <Typography variant="body1">{formatCurrency(sale.balanceDue)}</Typography>
                                </Box>
                            )}
                        </CardContent>
                    </Card>
                </Grid>

                {/* Sale Items */}
                <Grid item xs={12}>
                    <Card>
                        <CardContent>
                            <Typography variant="h6" gutterBottom>
                                Items
                            </Typography>
                            <TableContainer component={Paper} variant="outlined">
                                <Table size="small">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Product</TableCell>
                                            <TableCell align="right">Qty</TableCell>
                                            <TableCell align="right">Unit Price</TableCell>
                                            <TableCell align="right">Discount</TableCell>
                                            <TableCell align="right">Tax</TableCell>
                                            <TableCell align="right">Total</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {sale.items.map((item, idx) => (
                                            <TableRow key={idx}>
                                                <TableCell>{item.productName}</TableCell>
                                                <TableCell align="right">{item.quantity}</TableCell>
                                                <TableCell align="right">{formatCurrency(item.unitPrice)}</TableCell>
                                                <TableCell align="right">{formatCurrency(item.discountAmount)}</TableCell>
                                                <TableCell align="right">{formatCurrency(item.taxAmount)}</TableCell>
                                                <TableCell align="right">{formatCurrency(item.lineTotal)}</TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        </CardContent>
                    </Card>
                </Grid>

                {/* Payments */}
                {sale.payments.length > 0 && (
                    <Grid item xs={12}>
                        <Card>
                            <CardContent>
                                <Typography variant="h6" gutterBottom>
                                    Payments
                                </Typography>
                                <TableContainer component={Paper} variant="outlined">
                                    <Table size="small">
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>Method</TableCell>
                                                <TableCell align="right">Amount</TableCell>
                                                <TableCell>Reference</TableCell>
                                                <TableCell>Notes</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {sale.payments.map((payment, idx) => (
                                                <TableRow key={idx}>
                                                    <TableCell>{getPaymentMethodLabel(payment.method)}</TableCell>
                                                    <TableCell align="right">{formatCurrency(payment.amount)}</TableCell>
                                                    <TableCell>{payment.reference || '—'}</TableCell>
                                                    <TableCell>{payment.notes || '—'}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            </CardContent>
                        </Card>
                    </Grid>
                )}
            </Grid>

            {/* Cancel Sale Dialog */}
            <Dialog open={cancelDialogOpen} onClose={() => setCancelDialogOpen(false)} maxWidth="sm" fullWidth>
                <DialogTitle>Cancel Sale {sale.invoiceNumber}</DialogTitle>
                <DialogContent>
                    <Alert severity="warning" sx={{ mb: 2 }}>
                        Cancelling this sale will restore inventory. This action cannot be undone.
                    </Alert>
                    <TextField
                        autoFocus
                        margin="dense"
                        label="Reason for cancellation"
                        fullWidth
                        multiline
                        rows={3}
                        value={cancelReason}
                        onChange={(e) => setCancelReason(e.target.value)}
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setCancelDialogOpen(false)}>Close</Button>
                    <Button
                        color="error"
                        variant="contained"
                        disabled={!cancelReason.trim() || cancelMutation.isLoading}
                        onClick={() => cancelMutation.mutate(cancelReason.trim())}
                    >
                        {cancelMutation.isLoading ? <CircularProgress size={24} /> : 'Confirm Cancel'}
                    </Button>
                </DialogActions>
            </Dialog>
        </Box>
        </>
    );
}

export default SaleDetailPage;
