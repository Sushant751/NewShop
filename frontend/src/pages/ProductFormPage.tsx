import { useState, useEffect, useRef } from 'react';
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
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { productsApi } from '../api/endpoints';
import type { CreateProductRequest, UpdateProductRequest } from '../types';

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

    const { data: product, isLoading } = useQuery(
        ['product', id],
        () => productsApi.getById(id!),
        { enabled: isEdit },
    );

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
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/products')} sx={{ mr: 2 }}>
                    Back
                </Button>
                <Typography variant="h4">{isEdit ? 'Edit Product' : 'New Product'}</Typography>
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
                    {error}
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
                                    onChange={(e) => handleChange('barcode', e.target.value)}
                                    inputRef={barcodeInputRef}
                                    InputProps={{
                                        endAdornment: (
                                            <InputAdornment position="end">
                                                <Tooltip title="Click to focus and scan barcode">
                                                    <Button 
                                                        size="small" 
                                                        variant="outlined" 
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
