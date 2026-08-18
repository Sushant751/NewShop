import { useState, useCallback, useEffect } from 'react';
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
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner';
import VolumeUpIcon from '@mui/icons-material/VolumeUp';
import VolumeOffIcon from '@mui/icons-material/VolumeOff';
import { productsApi, salesApi, customersApi } from '../api/endpoints';
import { PaymentMethod } from '../types';
import type { ProductDto, CustomerDto } from '../types';

interface CartItem {
    product: ProductDto;
    quantity: number;
}

// Web Audio API Beep Synthesizer for supermarket scanner feedback
const playBeep = (type: 'success' | 'error' = 'success') => {
    try {
        const AudioContext = window.AudioContext || (window as any).webkitAudioContext;
        if (!AudioContext) return;
        const ctx = new AudioContext();
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();

        osc.connect(gain);
        gain.connect(ctx.destination);

        if (type === 'success') {
            osc.type = 'sine';
            osc.frequency.setValueAtTime(1400, ctx.currentTime);
            osc.frequency.exponentialRampToValueAtTime(1800, ctx.currentTime + 0.08);
            gain.gain.setValueAtTime(0.3, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.08);
            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + 0.08);
        } else {
            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(400, ctx.currentTime);
            osc.frequency.setValueAtTime(250, ctx.currentTime + 0.12);
            gain.gain.setValueAtTime(0.3, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.12);
            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + 0.12);
        }
    } catch {
        // Fallback if browser audio policy prevents sound before user interaction
    }
};

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
    const [soundEnabled, setSoundEnabled] = useState(true);
    const [quickAddOpen, setQuickAddOpen] = useState(false);
    const [quickAddData, setQuickAddData] = useState({
        name: '',
        barcode: '',
        sellingPrice: 0,
        costPrice: 0,
        openingStock: 10,
    });
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

    // Quick Add Product mutation when scanning an unlisted barcode
    const quickAddMutation = useMutation(
        (data: CreateProductRequest) => productsApi.create(data),
        {
            onSuccess: (newProduct) => {
                queryClient.invalidateQueries('products');
                addToCart(newProduct);
                setQuickAddOpen(false);
                if (soundEnabled) playBeep('success');
                setSnackbar({
                    open: true,
                    message: `Product "${newProduct.name}" created and added to sale!`,
                    severity: 'success',
                });
            },
            onError: (err: Error) => {
                setSnackbar({ open: true, message: err.message, severity: 'error' });
            },
        },
    );

    // Customer search
    const { data: customers } = useQuery(
        ['customers-search', ''],
        () => customersApi.list({ page: 1, pageSize: 50 }),
    );

    const createSaleMutation = useMutation(
        () => {
            const subtotalValue = cart.reduce((sum, item) => sum + item.product.sellingPrice * item.quantity, 0);
            const itemDiscounts = subtotalValue > 0
                ? cart.map((item) => {
                    const itemValue = item.product.sellingPrice * item.quantity;
                    const allocated = discountAmount > 0 ? Number((discountAmount * (itemValue / subtotalValue)).toFixed(2)) : 0;
                    return {
                        productId: item.product.id,
                        quantity: item.quantity,
                        unitPrice: item.product.sellingPrice,
                        discountAmount: allocated,
                    };
                })
                : cart.map((item) => ({
                    productId: item.product.id,
                    quantity: item.quantity,
                    unitPrice: item.product.sellingPrice,
                    discountAmount: 0,
                }));

            return salesApi.create({
                customerId,
                shopId: null,
                items: itemDiscounts,
                payments: [
                    {
                        method: PaymentMethod[paymentMethod],
                        amount: paymentAmount || subtotal,
                    },
                ],
                discountAmount,
                notes: notes || null,
            });
        },
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

    // Axevia 2D In-Counter Barcode Scanner Processor
    const handleScannedCode = useCallback(async (code: string) => {
        const cleanCode = code.trim();
        if (!cleanCode) return;

        try {
            const results = await productsApi.search(cleanCode, 10);
            if (results && results.length > 0) {
                const exactMatch = results.find(
                    (p) => p.barcode?.toLowerCase() === cleanCode.toLowerCase() || p.sku?.toLowerCase() === cleanCode.toLowerCase()
                ) || results[0];

                addToCart(exactMatch);
                if (soundEnabled) playBeep('success');
                setSnackbar({
                    open: true,
                    message: `Scanned: ${exactMatch.name} (₹${exactMatch.sellingPrice.toFixed(2)})`,
                    severity: 'success',
                });
                setSearchTerm('');
            } else {
                if (soundEnabled) playBeep('error');
                setQuickAddData({
                    name: '',
                    barcode: cleanCode,
                    sellingPrice: 0,
                    costPrice: 0,
                    openingStock: 10,
                });
                setQuickAddOpen(true);
                setSnackbar({
                    open: true,
                    message: `Barcode "${cleanCode}" not found in inventory! Quick add enabled.`,
                    severity: 'error',
                });
            }
        } catch {
            if (soundEnabled) playBeep('error');
            setSnackbar({
                open: true,
                message: `Failed to query barcode: ${cleanCode}`,
                severity: 'error',
            });
        }
    }, [addToCart, soundEnabled]);

    // Global Key Listener for Axevia 2D Camera / In-Counter Barcode Scanner
    useEffect(() => {
        let buffer = '';
        let lastKeyTime = Date.now();

        const handleKeyDown = (e: KeyboardEvent) => {
            const target = e.target as HTMLElement;
            const isInput = target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable;

            if (e.key === 'Enter') {
                if (buffer.length >= 2) {
                    e.preventDefault();
                    const scanned = buffer;
                    buffer = '';
                    handleScannedCode(scanned);
                }
                return;
            }

            if (e.key.length === 1) {
                const now = Date.now();
                const timeDiff = now - lastKeyTime;
                lastKeyTime = now;

                // Fast burst typing detection (< 50ms per key) characteristic of Axevia 2D scanner
                if (timeDiff < 50 || !isInput) {
                    buffer += e.key;
                } else {
                    buffer = e.key;
                }
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [handleScannedCode]);

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
    const total = subtotal + taxAmount - discountAmount;
    const change = paymentAmount - total;

    return (
        <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h4">
                    Point of Sale
                </Typography>

                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                    <Chip
                        icon={<QrCodeScannerIcon sx={{ color: '#2e7d32 !important' }} />}
                        label="Axevia 2D Scanner Ready"
                        color="success"
                        variant="outlined"
                        size="medium"
                        sx={{
                            fontWeight: 600,
                            bgcolor: 'rgba(46, 125, 50, 0.08)',
                            borderColor: '#2e7d32',
                            px: 1,
                        }}
                    />
                    <IconButton
                        color={soundEnabled ? "primary" : "default"}
                        onClick={() => setSoundEnabled(!soundEnabled)}
                        title={soundEnabled ? "Scanner Beep Sound Enabled" : "Scanner Beep Sound Muted"}
                        sx={{ border: '1px solid #e0e0e0' }}
                    >
                        {soundEnabled ? <VolumeUpIcon /> : <VolumeOffIcon />}
                    </IconButton>
                </Box>
            </Box>

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

            {/* Quick Add Product Dialog when scanned barcode is missing */}
            <Dialog open={quickAddOpen} onClose={() => setQuickAddOpen(false)} maxWidth="xs" fullWidth>
                <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <QrCodeScannerIcon color="primary" /> Quick Add Product
                </DialogTitle>
                <DialogContent>
                    <Box sx={{ mt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
                        <Alert severity="info" size="small">
                            Barcode <strong>{quickAddData.barcode}</strong> is not in inventory. Enter details below to save and add to current sale!
                        </Alert>
                        <TextField
                            label="Barcode"
                            value={quickAddData.barcode}
                            disabled
                            fullWidth
                            size="small"
                        />
                        <TextField
                            label="Product Name *"
                            value={quickAddData.name}
                            onChange={(e) => setQuickAddData({ ...quickAddData, name: e.target.value })}
                            required
                            fullWidth
                            autoFocus
                        />
                        <TextField
                            label="Selling Price (₹) *"
                            type="number"
                            value={quickAddData.sellingPrice || ''}
                            onChange={(e) => setQuickAddData({ ...quickAddData, sellingPrice: parseFloat(e.target.value) || 0 })}
                            InputProps={{ startAdornment: '₹' }}
                            fullWidth
                        />
                        <TextField
                            label="Cost Price (₹)"
                            type="number"
                            value={quickAddData.costPrice || ''}
                            onChange={(e) => setQuickAddData({ ...quickAddData, costPrice: parseFloat(e.target.value) || 0 })}
                            InputProps={{ startAdornment: '₹' }}
                            fullWidth
                        />
                        <TextField
                            label="Opening Stock"
                            type="number"
                            value={quickAddData.openingStock}
                            onChange={(e) => setQuickAddData({ ...quickAddData, openingStock: parseFloat(e.target.value) || 0 })}
                            fullWidth
                        />
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setQuickAddOpen(false)}>Cancel</Button>
                    <Button
                        variant="contained"
                        disabled={!quickAddData.name.trim() || quickAddData.sellingPrice <= 0 || quickAddMutation.isLoading}
                        onClick={() => {
                            quickAddMutation.mutate({
                                name: quickAddData.name,
                                barcode: quickAddData.barcode,
                                sellingPrice: quickAddData.sellingPrice,
                                costPrice: quickAddData.costPrice,
                                openingStock: quickAddData.openingStock,
                                isTaxable: true,
                                taxRate: 0,
                                trackInventory: true,
                                allowSaleWithoutStock: true,
                            });
                        }}
                        startIcon={quickAddMutation.isLoading ? <CircularProgress size={18} /> : undefined}
                    >
                        Save & Add to Sale
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
