import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import {
    Box,
    Grid,
    Card,
    CardContent,
    Typography,
    TextField,
    InputAdornment,
    Button,
    IconButton,
    List,
    ListItem,
    ListItemText,
    ListItemSecondaryAction,
    Divider,
    Alert,
    CircularProgress,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    RadioGroup,
    Radio,
    FormControlLabel,
    Chip,
    Snackbar,
    Autocomplete,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import AddIcon from '@mui/icons-material/Add';
import RemoveIcon from '@mui/icons-material/Remove';
import DeleteIcon from '@mui/icons-material/Delete';
import ShoppingCartIcon from '@mui/icons-material/ShoppingCart';
import { productsApi, salesApi, customersApi } from '../api/endpoints';
import { PaymentMethod } from '../types';
import type { ProductDto, CustomerDto } from '../types';

interface CartItem {
    product: ProductDto;
    quantity: number;
}

function POSPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();

    const [searchTerm, setSearchTerm] = useState('');
    const [cart, setCart] = useState<CartItem[]>([]);
    const [discountAmount, setDiscountAmount] = useState(0);
    const [customerId, setCustomerId] = useState<string | null>(null);
    const [notes, setNotes] = useState('');
    const [checkoutOpen, setCheckoutOpen] = useState(false);
    const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.Cash);
    const [paymentAmount, setPaymentAmount] = useState(0);
    const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
        open: false,
        message: '',
        severity: 'success',
    });

    // Product search
    const { data: searchResults, isLoading: searching } = useQuery(
        ['product-search', searchTerm],
        () => productsApi.search(searchTerm, 20),
        { enabled: searchTerm.length >= 2 },
    );

    // Customer search
    const { data: customers } = useQuery(
        ['customers-search', ''],
        () => customersApi.list({ page: 1, pageSize: 50 }),
    );

    const createSaleMutation = useMutation(
        () =>
            salesApi.create({
                customerId,
                shopId: null, // backend resolves from token
                items: cart.map((item) => ({
                    productId: item.product.id,
                    quantity: item.quantity,
                    unitPrice: item.product.sellingPrice,
                })),
                payments: [
                    {
                        method: PaymentMethod[paymentMethod],
                        amount: paymentAmount || subtotal,
                    },
                ],
                discountAmount,
                notes: notes || null,
            }),
        {
            onSuccess: (sale) => {
                queryClient.invalidateQueries('sales');
                queryClient.invalidateQueries('dashboard');
                queryClient.invalidateQueries('products');
                setSnackbar({ open: true, message: `Sale completed: ${sale.invoiceNumber}`, severity: 'success' });
                setCart([]);
                setDiscountAmount(0);
                setCustomerId(null);
                setNotes('');
                setCheckoutOpen(false);
                setPaymentAmount(0);
                navigate(`/sales/${sale.id}`);
            },
            onError: (err: Error) => {
                setSnackbar({ open: true, message: err.message, severity: 'error' });
                setCheckoutOpen(false);
            },
        },
    );

    const addToCart = useCallback((product: ProductDto) => {
        setCart((prev) => {
            const existing = prev.find((item) => item.product.id === product.id);
            if (existing) {
                return prev.map((item) =>
                    item.product.id === product.id
                        ? { ...item, quantity: item.quantity + 1 }
                        : item,
                );
            }
            return [...prev, { product, quantity: 1 }];
        });
    }, []);

    const updateQuantity = (productId: string, delta: number) => {
        setCart((prev) =>
            prev
                .map((item) =>
                    item.product.id === productId
                        ? { ...item, quantity: Math.max(0, item.quantity + delta) }
                        : item,
                )
                .filter((item) => item.quantity > 0),
        );
    };

    const setQuantity = (productId: string, qty: number) => {
        if (qty <= 0) {
            setCart((prev) => prev.filter((item) => item.product.id !== productId));
        } else {
            setCart((prev) =>
                prev.map((item) =>
                    item.product.id === productId ? { ...item, quantity: qty } : item,
                ),
            );
        }
    };

    const removeFromCart = (productId: string) => {
        setCart((prev) => prev.filter((item) => item.product.id !== productId));
    };

    const subtotal = cart.reduce(
        (sum, item) => sum + item.product.sellingPrice * item.quantity,
        0,
    );
    const taxAmount = cart.reduce(
        (sum, item) =>
            sum +
            (item.product.isTaxable
                ? item.product.sellingPrice * item.quantity * (item.product.taxRate / 100)
                : 0),
        0,
    );
    const total = subtotal - discountAmount + taxAmount;
    const change = paymentAmount - total;

    return (
        <Box>
            <Typography variant="h4" gutterBottom>
                Point of Sale
            </Typography>

            <Grid container spacing={3}>
                {/* Product Search & Results */}
                <Grid item xs={12} md={7}>
                    <Card>
                        <CardContent>
                            <TextField
                                placeholder="Search products by name, SKU, or barcode..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter') {
                                        if (searchResults?.length === 1) {
                                            addToCart(searchResults[0]);
                                            setSearchTerm('');
                                        } else if (searchResults && searchResults.length > 1) {
                                            const exact = searchResults.find(p => p.barcode === searchTerm || p.sku === searchTerm);
                                            if (exact) {
                                                addToCart(exact);
                                                setSearchTerm('');
                                            }
                                        }
                                    }
                                }}
                                fullWidth
                                sx={{ mb: 2 }}
                                InputProps={{
                                    startAdornment: (
                                        <InputAdornment position="start">
                                            <SearchIcon />
                                        </InputAdornment>
                                    ),
                                }}
                            />

                            {searching && (
                                <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                                    <CircularProgress size={32} />
                                </Box>
                            )}

                            {searchResults && searchResults.length === 0 && searchTerm.length >= 2 && (
                                <Typography color="textSecondary" align="center" sx={{ py: 3 }}>
                                    No products found
                                </Typography>
                            )}

                            <Grid container spacing={1}>
                                {searchResults?.map((product) => (
                                    <Grid item xs={12} sm={6} md={4} key={product.id}>
                                        <Card
                                            variant="outlined"
                                            sx={{
                                                cursor: 'pointer',
                                                transition: 'all 0.2s',
                                                '&:hover': { borderColor: 'primary.main', boxShadow: 2 },
                                                opacity: product.isActive ? 1 : 0.5,
                                            }}
                                            onClick={() => product.isActive && addToCart(product)}
                                        >
                                            <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
                                                <Typography variant="body2" fontWeight={600} noWrap>
                                                    {product.name}
                                                </Typography>
                                                <Typography variant="caption" color="textSecondary">
                                                    {product.sku || 'No SKU'}
                                                </Typography>
                                                <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 0.5 }}>
                                                    <Typography variant="body2" color="primary" fontWeight={700}>
                                                        ₹{product.sellingPrice.toFixed(2)}
                                                        {product.isTaxable && ` (+${product.taxRate}% GST)`}
                                                    </Typography>
                                                    <Chip
                                                        label={`Stock: ${product.currentStock}`}
                                                        size="small"
                                                        color={product.currentStock <= product.reorderLevel ? 'error' : 'success'}
                                                        variant="outlined"
                                                    />
                                                </Box>
                                            </CardContent>
                                        </Card>
                                    </Grid>
                                ))}
                            </Grid>
                        </CardContent>
                    </Card>
                </Grid>

                {/* Cart */}
                <Grid item xs={12} md={5}>
                    <Card sx={{ position: 'sticky', top: 80 }}>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                                <ShoppingCartIcon sx={{ mr: 1 }} />
                                <Typography variant="h6">Cart ({cart.length})</Typography>
                            </Box>

                            {cart.length === 0 ? (
                                <Typography color="textSecondary" align="center" sx={{ py: 4 }}>
                                    Cart is empty. Search and add products.
                                </Typography>
                            ) : (
                                <>
                                    <List sx={{ maxHeight: 300, overflow: 'auto' }}>
                                        {cart.map((item) => (
                                            <ListItem key={item.product.id} divider>
                                                <ListItemText
                                                    primary={item.product.name}
                                                    secondary={`₹${item.product.sellingPrice.toFixed(2)} × ${item.quantity} = ₹${(item.product.sellingPrice * item.quantity).toFixed(2)}${item.product.isTaxable ? ` (Tax: ₹${(item.product.sellingPrice * item.quantity * (item.product.taxRate / 100)).toFixed(2)})` : ''}`}
                                                    primaryTypographyProps={{ fontWeight: 600, noWrap: true }}
                                                    secondaryTypographyProps={{ variant: 'caption' }}
                                                />
                                                <ListItemSecondaryAction>
                                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                                                        <IconButton size="small" onClick={() => updateQuantity(item.product.id, -1)}>
                                                            <RemoveIcon fontSize="small" />
                                                        </IconButton>
                                                        <TextField
                                                            value={item.quantity}
                                                            onChange={(e) => setQuantity(item.product.id, parseInt(e.target.value) || 0)}
                                                            sx={{ width: 50 }}
                                                            inputProps={{ style: { textAlign: 'center' } }}
                                                            size="small"
                                                        />
                                                        <IconButton size="small" onClick={() => updateQuantity(item.product.id, 1)}>
                                                            <AddIcon fontSize="small" />
                                                        </IconButton>
                                                        <IconButton size="small" onClick={() => removeFromCart(item.product.id)} color="error">
                                                            <DeleteIcon fontSize="small" />
                                                        </IconButton>
                                                    </Box>
                                                </ListItemSecondaryAction>
                                            </ListItem>
                                        ))}
                                    </List>

                                    <Divider sx={{ my: 2 }} />

                                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <Typography>Subtotal:</Typography>
                                            <Typography>₹{subtotal.toFixed(2)}</Typography>
                                        </Box>
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                            <Typography>Discount:</Typography>
                                            <TextField
                                                type="number"
                                                value={discountAmount}
                                                onChange={(e) => setDiscountAmount(Math.max(0, parseFloat(e.target.value) || 0))}
                                                sx={{ width: 100 }}
                                                size="small"
                                                InputProps={{ startAdornment: '₹' }}
                                            />
                                        </Box>
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <Typography>Tax:</Typography>
                                            <Typography>₹{taxAmount.toFixed(2)}</Typography>
                                        </Box>
                                        <Divider />
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <Typography variant="h6">Total:</Typography>
                                            <Typography variant="h6" color="primary">₹{total.toFixed(2)}</Typography>
                                        </Box>
                                    </Box>

                                    <Button
                                        fullWidth
                                        variant="contained"
                                        size="large"
                                        sx={{ mt: 2 }}
                                        onClick={() => setCheckoutOpen(true)}
                                        disabled={cart.length === 0}
                                    >
                                        Checkout
                                    </Button>
                                </>
                            )}
                        </CardContent>
                    </Card>
                </Grid>
            </Grid>

            {/* Checkout Dialog */}
            <Dialog open={checkoutOpen} onClose={() => setCheckoutOpen(false)} maxWidth="sm" fullWidth>
                <DialogTitle>Checkout - Total: ₹{total.toFixed(2)}</DialogTitle>
                <DialogContent>
                    <Box sx={{ mt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
                        <Autocomplete
                            options={customers?.items || []}
                            getOptionLabel={(option: CustomerDto) => `${option.name} ${option.phone ? `(${option.phone})` : ''}`}
                            value={customers?.items?.find((c: CustomerDto) => c.id === customerId) || null}
                            onChange={(_, newValue) => setCustomerId(newValue ? (newValue as CustomerDto).id : null)}
                            isOptionEqualToValue={(option, value) => option.id === value.id}
                            renderInput={(params) => (
                                <TextField {...params} label="Customer (optional)" placeholder="Search customer... or Walk-in" />
                            )}
                            fullWidth
                        />

                        <Typography variant="subtitle1">Payment Method</Typography>
                        <RadioGroup
                            row
                            value={paymentMethod}
                            onChange={(e) => setPaymentMethod(Number(e.target.value) as PaymentMethod)}
                        >
                            <FormControlLabel value={PaymentMethod.Cash} control={<Radio />} label="Cash" />
                            <FormControlLabel value={PaymentMethod.Card} control={<Radio />} label="Card" />
                            <FormControlLabel value={PaymentMethod.UPI} control={<Radio />} label="UPI" />
                            <FormControlLabel value={PaymentMethod.Wallet} control={<Radio />} label="Wallet" />
                            <FormControlLabel value={PaymentMethod.Credit} control={<Radio />} label="Credit" />
                        </RadioGroup>

                        <TextField
                            label="Amount Received"
                            type="number"
                            value={paymentAmount}
                            onChange={(e) => setPaymentAmount(parseFloat(e.target.value) || 0)}
                            InputProps={{ startAdornment: '₹' }}
                            fullWidth
                            helperText={paymentAmount > 0 ? `Change: ₹${Math.max(0, change).toFixed(2)}` : ''}
                        />

                        <TextField
                            label="Notes (optional)"
                            value={notes}
                            onChange={(e) => setNotes(e.target.value)}
                            multiline
                            rows={2}
                            fullWidth
                        />
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setCheckoutOpen(false)}>Cancel</Button>
                    <Button
                        variant="contained"
                        onClick={() => createSaleMutation.mutate()}
                        disabled={createSaleMutation.isLoading}
                        startIcon={createSaleMutation.isLoading ? <CircularProgress size={20} /> : undefined}
                    >
                        Complete Sale
                    </Button>
                </DialogActions>
            </Dialog>

            <Snackbar
                open={snackbar.open}
                autoHideDuration={4000}
                onClose={() => setSnackbar({ ...snackbar, open: false })}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                <Alert severity={snackbar.severity} onClose={() => setSnackbar({ ...snackbar, open: false })}>
                    {snackbar.message}
                </Alert>
            </Snackbar>
        </Box>
    );
}

export default POSPage;
