import { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import {
    Box,
    Button,
    Card,
    CardContent,
    Grid,
    TextField,
    Typography,
    Alert,
    CircularProgress,
    Switch,
    FormControlLabel,
    Divider,
    InputAdornment,
    Tooltip,
    Chip,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import WarningIcon from '@mui/icons-material/Warning';
import { productsApi } from '../api/endpoints';
import type { CreateProductRequest, UpdateProductRequest, ProductDto } from '../types';

// Web Audio API Beep Synthesizer for scanner feedback
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
        // Audio synthesis fallback
    }
};

function ProductFormPage() {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const queryClient = useQueryClient();
    const isEdit = !!id;
    const barcodeInputRef = useRef<HTMLInputElement>(null);

    const [formData, setFormData] = useState({
        name: '',
        description: '',
        sku: '',
        barcode: '',
        categoryId: '',
        brandId: '',
        unitId: '',
        costPrice: 0,
        sellingPrice: 0,
        taxRate: 0,
        isTaxable: true,
        reorderLevel: 0,
        openingStock: 0,
        imageUrl: '',
        trackInventory: true,
        allowSaleWithoutStock: false,
        isActive: true,
    });
    const [error, setError] = useState('');
    const [barcodeStatus, setBarcodeStatus] = useState<{
        checking: boolean;
        message: string;
        severity: 'success' | 'warning' | 'error' | null;
        existingProduct?: ProductDto;
    } | null>(null);

    const { data: product, isLoading } = useQuery(
        ['product', id],
        () => productsApi.getById(id!),
        { enabled: isEdit },
    );

    const checkBarcodeAvailability = useCallback(async (code: string) => {
        const cleanCode = code.trim();
        if (!cleanCode) {
            setBarcodeStatus(null);
            return;
        }

        setBarcodeStatus({ checking: true, message: 'Checking barcode availability...', severity: null });

        try {
            const results = await productsApi.search(cleanCode, 10);
            const match = results?.find(
                (p) => (p.barcode?.toLowerCase() === cleanCode.toLowerCase() || p.sku?.toLowerCase() === cleanCode.toLowerCase()) && p.id !== id
            );

            if (match) {
                playBeep('error');
                setBarcodeStatus({
                    checking: false,
                    message: `Barcode "${cleanCode}" is ALREADY registered to "${match.name}" (SKU: ${match.sku || 'N/A'}, Price: ₹${match.sellingPrice.toFixed(2)})`,
                    severity: 'warning',
                    existingProduct: match,
                });
            } else {
                playBeep('success');
                setBarcodeStatus({
                    checking: false,
                    message: `Barcode "${cleanCode}" is available!`,
                    severity: 'success',
                });
            }
        } catch {
            setBarcodeStatus({
                checking: false,
                message: 'Failed to verify barcode availability.',
                severity: 'error',
            });
        }
    }, [id]);

    // Axevia 2D Barcode Scanner listener for Product Form
    useEffect(() => {
        let buffer = '';
        let lastKeyTime = Date.now();

        const handleKeyDown = (e: KeyboardEvent) => {
            const target = e.target as HTMLElement;
            const isInput = target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable;

            if (e.key === 'Enter') {
                if (buffer.length >= 2) {
                    e.preventDefault();
                    const scanned = buffer.trim();
                    buffer = '';
                    setFormData((prev) => ({ ...prev, barcode: scanned }));
                    checkBarcodeAvailability(scanned);
                }
                return;
            }

            if (e.key.length === 1) {
                const now = Date.now();
                const timeDiff = now - lastKeyTime;
                lastKeyTime = now;

                if (timeDiff < 50 || !isInput) {
                    buffer += e.key;
                } else {
                    buffer = e.key;
                }
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [checkBarcodeAvailability]);

    useEffect(() => {
        if (product) {
            setFormData({
                name: product.name,
                description: product.description || '',
                sku: product.sku || '',
                barcode: product.barcode || '',
                categoryId: product.categoryId || '',
                brandId: product.brandId || '',
                unitId: product.unitId || '',
                costPrice: product.costPrice,
                sellingPrice: product.sellingPrice,
                taxRate: product.taxRate,
                isTaxable: product.isTaxable,
                reorderLevel: product.reorderLevel,
                openingStock: product.currentStock,
                imageUrl: product.imageUrl || '',
                trackInventory: product.trackInventory,
                allowSaleWithoutStock: false,
                isActive: product.isActive,
            });
        }
    }, [product]);

    const createMutation = useMutation(
        (data: CreateProductRequest) => productsApi.create(data),
        {
            onSuccess: () => {
                queryClient.invalidateQueries('products');
                navigate('/products');
            },
            onError: (err: Error) => setError(err.message),
        },
    );

    const updateMutation = useMutation(
        (data: UpdateProductRequest) => productsApi.update(id!, data),
        {
            onSuccess: () => {
                queryClient.invalidateQueries('products');
                queryClient.invalidateQueries(['product', id]);
                navigate('/products');
            },
            onError: (err: Error) => setError(err.message),
        },
    );

    const handleChange = (field: string, value: string | number | boolean) => {
        setFormData((prev) => {
            const newData = { ...prev, [field]: value };
            // Auto-toggle isTaxable based on taxRate
            if (field === 'taxRate') {
                newData.isTaxable = (value as number) > 0;
            }
            return newData;
        });
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        if (!formData.name.trim()) {
            setError('Product name is required');
            return;
        }
        if (formData.sellingPrice < 0 || formData.costPrice < 0) {
            setError('Prices cannot be negative');
            return;
        }

        if (isEdit) {
            const payload: UpdateProductRequest = {
                name: formData.name,
                description: formData.description || null,
                sku: formData.sku || null,
                barcode: formData.barcode || null,
                categoryId: formData.categoryId || null,
                brandId: formData.brandId || null,
                unitId: formData.unitId || null,
                costPrice: formData.costPrice,
                sellingPrice: formData.sellingPrice,
                taxRate: formData.taxRate,
                isTaxable: formData.isTaxable,
                reorderLevel: formData.reorderLevel,
                imageUrl: formData.imageUrl || null,
                trackInventory: formData.trackInventory,
                isActive: formData.isActive,
            };
            updateMutation.mutate(payload);
        } else {
            const payload: CreateProductRequest = {
                name: formData.name,
                description: formData.description || null,
                sku: formData.sku || null,
                barcode: formData.barcode || null,
                categoryId: formData.categoryId || null,
                brandId: formData.brandId || null,
                unitId: formData.unitId || null,
                costPrice: formData.costPrice,
                sellingPrice: formData.sellingPrice,
                taxRate: formData.taxRate,
                isTaxable: formData.isTaxable,
                reorderLevel: formData.reorderLevel,
                openingStock: formData.openingStock,
                imageUrl: formData.imageUrl || null,
                trackInventory: formData.trackInventory,
                allowSaleWithoutStock: formData.allowSaleWithoutStock,
            };
            createMutation.mutate(payload);
        }
    };

    const isSaving = createMutation.isLoading || updateMutation.isLoading;

    if (isEdit && isLoading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/products')} sx={{ mr: 2 }}>
                        Back
                    </Button>
                    <Typography variant="h4">{isEdit ? 'Edit Product' : 'New Product'}</Typography>
                </Box>

                <Chip
                    icon={<QrCodeScannerIcon sx={{ color: '#2e7d32 !important' }} />}
                    label="Axevia 2D Scanner Active"
                    color="success"
                    variant="outlined"
                    sx={{ fontWeight: 600, bgcolor: 'rgba(46, 125, 50, 0.08)', borderColor: '#2e7d32' }}
                />
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
                    {error}
                </Alert>
            )}

            {barcodeStatus && barcodeStatus.severity && (
                <Alert
                    severity={barcodeStatus.severity}
                    sx={{ mb: 2 }}
                    action={
                        barcodeStatus.existingProduct ? (
                            <Button
                                color="inherit"
                                size="small"
                                onClick={() => navigate(`/products/edit/${barcodeStatus.existingProduct?.id}`)}
                            >
                                Edit Existing Product
                            </Button>
                        ) : undefined
                    }
                >
                    {barcodeStatus.message}
                </Alert>
            )}

            <Card>
                <CardContent sx={{ p: 4 }}>
                    <Box component="form" onSubmit={handleSubmit}>
                        <Grid container spacing={3}>
                            {/* Basic Info */}
                            <Grid item xs={12}>
                                <Typography variant="h6" gutterBottom>
                                    Basic Information
                                </Typography>
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    label="Product Name *"
                                    value={formData.name}
                                    onChange={(e) => handleChange('name', e.target.value)}
                                    required
                                    fullWidth
                                />
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    label="SKU"
                                    value={formData.sku}
                                    onChange={(e) => handleChange('sku', e.target.value)}
                                    fullWidth
                                />
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    label="Barcode"
                                    value={formData.barcode}
                                    onChange={(e) => {
                                        const val = e.target.value;
                                        handleChange('barcode', val);
                                        if (val.length >= 3) {
                                            checkBarcodeAvailability(val);
                                        } else {
                                            setBarcodeStatus(null);
                                        }
                                    }}
                                    inputRef={barcodeInputRef}
                                    InputProps={{
                                        endAdornment: (
                                            <InputAdornment position="end">
                                                <Tooltip title="Scan barcode with Axevia scanner or click to focus">
                                                    <Button 
                                                        size="small" 
                                                        variant="contained" 
                                                        color="primary"
                                                        startIcon={<QrCodeScannerIcon />}
                                                        onClick={() => barcodeInputRef.current?.focus()}
                                                    >
                                                        Scan
                                                    </Button>
                                                </Tooltip>
                                            </InputAdornment>
                                        )
                                    }}
                                    fullWidth
                                />
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    label="Image URL"
                                    value={formData.imageUrl}
                                    onChange={(e) => handleChange('imageUrl', e.target.value)}
                                    fullWidth
                                />
                            </Grid>
                            <Grid item xs={12}>
                                <TextField
                                    label="Description"
                                    value={formData.description}
                                    onChange={(e) => handleChange('description', e.target.value)}
                                    multiline
                                    rows={2}
                                    fullWidth
                                />
                            </Grid>

                            <Grid item xs={12}>
                                <Divider sx={{ my: 1 }} />
                                <Typography variant="h6" gutterBottom>
                                    Pricing
                                </Typography>
                            </Grid>
                            <Grid item xs={12} sm={4}>
                                <TextField
                                    label="Cost Price"
                                    type="number"
                                    value={formData.costPrice}
                                    onChange={(e) => handleChange('costPrice', parseFloat(e.target.value) || 0)}
                                    InputProps={{ startAdornment: '₹' }}
                                    fullWidth
                                />
                            </Grid>
                            <Grid item xs={12} sm={4}>
                                <TextField
                                    label="Selling Price"
                                    type="number"
                                    value={formData.sellingPrice}
                                    onChange={(e) => handleChange('sellingPrice', parseFloat(e.target.value) || 0)}
                                    InputProps={{ startAdornment: '₹' }}
                                    fullWidth
                                />
                            </Grid>
                            <Grid item xs={12} sm={4}>
                                <TextField
                                    label="Tax Rate (%)"
                                    type="number"
                                    value={formData.taxRate}
                                    onChange={(e) => handleChange('taxRate', parseFloat(e.target.value) || 0)}
                                    fullWidth
                                />
                            </Grid>
                            <Grid item xs={12}>
                                <FormControlLabel
                                    control={
                                        <Switch
                                            checked={formData.isTaxable}
                                            onChange={(e) => handleChange('isTaxable', e.target.checked)}
                                        />
                                    }
                                    label="Taxable"
                                />
                            </Grid>

                            <Grid item xs={12}>
                                <Divider sx={{ my: 1 }} />
                                <Typography variant="h6" gutterBottom>
                                    Inventory
                                </Typography>
                            </Grid>
                            <Grid item xs={12} sm={4}>
                                <TextField
                                    label="Reorder Level"
                                    type="number"
                                    value={formData.reorderLevel}
                                    onChange={(e) => handleChange('reorderLevel', parseFloat(e.target.value) || 0)}
                                    fullWidth
                                />
                            </Grid>
                            {!isEdit && (
                                <Grid item xs={12} sm={4}>
                                    <TextField
                                        label="Opening Stock"
                                        type="number"
                                        value={formData.openingStock}
                                        onChange={(e) => handleChange('openingStock', parseFloat(e.target.value) || 0)}
                                        fullWidth
                                    />
                                </Grid>
                            )}
                            <Grid item xs={12}>
                                <FormControlLabel
                                    control={
                                        <Switch
                                            checked={formData.trackInventory}
                                            onChange={(e) => handleChange('trackInventory', e.target.checked)}
                                        />
                                    }
                                    label="Track Inventory"
                                />
                                {!isEdit && (
                                    <FormControlLabel
                                        control={
                                            <Switch
                                                checked={formData.allowSaleWithoutStock}
                                                onChange={(e) => handleChange('allowSaleWithoutStock', e.target.checked)}
                                            />
                                        }
                                        label="Allow Sale Without Stock"
                                    />
                                )}
                                {isEdit && (
                                    <FormControlLabel
                                        control={
                                            <Switch
                                                checked={formData.isActive}
                                                onChange={(e) => handleChange('isActive', e.target.checked)}
                                            />
                                        }
                                        label="Active"
                                    />
                                )}
                            </Grid>
                        </Grid>

                        <Box sx={{ display: 'flex', gap: 2, mt: 4, justifyContent: 'flex-end' }}>
                            <Button onClick={() => navigate('/products')} variant="outlined">
                                Cancel
                            </Button>
                            <Button
                                type="submit"
                                variant="contained"
                                disabled={isSaving}
                                startIcon={isSaving ? <CircularProgress size={20} /> : undefined}
                            >
                                {isEdit ? 'Update Product' : 'Create Product'}
                            </Button>
                        </Box>
                    </Box>
                </CardContent>
            </Card>
        </Box>
    );
}

export default ProductFormPage;
