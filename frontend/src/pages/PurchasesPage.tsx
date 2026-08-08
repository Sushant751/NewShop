import { useState, FormEvent } from 'react';
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
    TextField,
    InputAdornment,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Grid,
    IconButton,
    MenuItem,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Paper,
    Divider,
    Autocomplete,
} from '@mui/material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import SearchIcon from '@mui/icons-material/Search';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { purchasesApi, suppliersApi, productsApi } from '../api/endpoints';
import { Permissions } from '../types';
import type { PurchaseDto, CreatePurchaseRequest, SupplierDto, ProductDto, PagedResult } from '../types';
import { hasPermission } from '../components/ProtectedRoute';
import { useAppSelector } from '../store';
import { getErrorMessage } from '../utils/helpers';

interface PurchaseItemForm {
    productId: string;
    productName: string;
    quantity: string;
    unitCost: string;
}

function PurchasesPage() {
    const queryClient = useQueryClient();
    const user = useAppSelector((state) => state.auth.user);
    const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
        page: 0,
        pageSize: 10,
    });
    const [search, setSearch] = useState('');
    const [searchInput, setSearchInput] = useState('');

    const [createDialogOpen, setCreateDialogOpen] = useState(false);
    const [detailDialogOpen, setDetailDialogOpen] = useState(false);
    const [selectedPurchase, setSelectedPurchase] = useState<PurchaseDto | null>(null);

    const [supplierId, setSupplierId] = useState<string>('');
    const [notes, setNotes] = useState('');
    const [items, setItems] = useState<PurchaseItemForm[]>([]);
    const [formError, setFormError] = useState<string | null>(null);

    const [productSearch, setProductSearch] = useState('');

    const canCreate = hasPermission(user, Permissions.PurchasesCreate);

    const { data, isLoading, error } = useQuery(
        ['purchases', paginationModel.page, paginationModel.pageSize, search],
        () =>
            purchasesApi.list({
                page: paginationModel.page + 1,
                pageSize: paginationModel.pageSize,
                search: search || undefined,
            }),
    );

    const { data: suppliers } = useQuery<PagedResult<SupplierDto>>(
        ['suppliers-all'],
        () => suppliersApi.list({ page: 1, pageSize: 1000 }),
    );

    const productSearchQuery = useQuery<ProductDto[]>(
        ['product-search', productSearch],
        () => productsApi.search(productSearch, 20),
        { enabled: productSearch.length >= 2 },
    );

    const createMutation = useMutation(
        (data: CreatePurchaseRequest) => purchasesApi.create(data),
        {
            onSuccess: () => {
                queryClient.invalidateQueries(['purchases']);
                setCreateDialogOpen(false);
                resetForm();
            },
        },
    );

    const detailQuery = useQuery<PurchaseDto>(
        ['purchase-detail', selectedPurchase?.id],
        () => purchasesApi.getById(selectedPurchase!.id),
        { enabled: !!selectedPurchase && detailDialogOpen },
    );

    const formatCurrency = (val: number) => `₹${val.toFixed(2)}`;

    const handleSearch = () => {
        setSearch(searchInput);
        setPaginationModel((prev) => ({ ...prev, page: 0 }));
    };

    const resetForm = () => {
        setSupplierId('');
        setNotes('');
        setItems([]);
        setFormError(null);
        setProductSearch('');
    };

    const openCreate = () => {
        resetForm();
        setCreateDialogOpen(true);
    };

    const addProductToItems = (product: ProductDto) => {
        const existing = items.find((i) => i.productId === product.id);
        if (existing) {
            setFormError(`${product.name} is already in the list`);
            return;
        }
        setItems([
            ...items,
            {
                productId: product.id,
                productName: product.name,
                quantity: '1',
                unitCost: String(product.costPrice),
            },
        ]);
        setProductSearch('');
        setFormError(null);
    };

    const updateItem = (index: number, field: keyof PurchaseItemForm, value: string) => {
        const updated = [...items];
        updated[index] = { ...updated[index], [field]: value };
        setItems(updated);
    };

    const removeItem = (index: number) => {
        setItems(items.filter((_, i) => i !== index));
    };

    const calculateTotal = () => {
        return items.reduce((sum, item) => {
            const qty = parseFloat(item.quantity) || 0;
            const cost = parseFloat(item.unitCost) || 0;
            return sum + qty * cost;
        }, 0);
    };

    const handleSubmit = (e: FormEvent) => {
        e.preventDefault();
        setFormError(null);

        if (!supplierId) {
            setFormError('Please select a supplier');
            return;
        }
        if (items.length === 0) {
            setFormError('Please add at least one item');
            return;
        }
        for (const item of items) {
            const qty = parseFloat(item.quantity);
            const cost = parseFloat(item.unitCost);
            if (!qty || qty <= 0) {
                setFormError(`Invalid quantity for ${item.productName}`);
                return;
            }
            if (isNaN(cost) || cost < 0) {
                setFormError(`Invalid unit cost for ${item.productName}`);
                return;
            }
        }

        // Use first shop from user context - in a real app this would come from a shop selector
        const shopId = user?.userId || '';

        createMutation.mutate({
            supplierId,
            shopId,
            items: items.map((item) => ({
                productId: item.productId,
                quantity: parseFloat(item.quantity),
                unitCost: parseFloat(item.unitCost),
            })),
            notes: notes.trim() || null,
        });
    };

    const getStatusChip = (status: string) => {
        const statusMap: Record<string, { label: string; color: 'success' | 'error' | 'warning' | 'default' }> = {
            Draft: { label: 'Draft', color: 'default' },
            Ordered: { label: 'Ordered', color: 'info' as 'success' },
            PartiallyReceived: { label: 'Partial', color: 'warning' },
            Received: { label: 'Received', color: 'success' },
            Cancelled: { label: 'Cancelled', color: 'error' },
        };
        const info = statusMap[status] || { label: status, color: 'default' as const };
        return <Chip label={info.label} size="small" color={info.color} variant="outlined" />;
    };

    const columns: GridColDef[] = [
        { field: 'purchaseNumber', headerName: 'Purchase #', flex: 1, minWidth: 120 },
        {
            field: 'purchaseDate',
            headerName: 'Date',
            flex: 1,
            minWidth: 150,
            valueFormatter: (value: string) => new Date(value).toLocaleString(),
        },
        { field: 'supplierName', headerName: 'Supplier', flex: 1, minWidth: 150 },
        {
            field: 'status',
            headerName: 'Status',
            flex: 0.7,
            minWidth: 100,
            renderCell: (params) => getStatusChip(params.value as string),
        },
        {
            field: 'grandTotal',
            headerName: 'Total',
            flex: 0.7,
            minWidth: 100,
            type: 'number',
            valueFormatter: (value: number) => formatCurrency(value),
        },
        {
            field: 'actions',
            headerName: 'Actions',
            flex: 0.5,
            minWidth: 70,
            sortable: false,
            renderCell: (params) => (
                <IconButton
                    size="small"
                    onClick={() => {
                        setSelectedPurchase(params.row as PurchaseDto);
                        setDetailDialogOpen(true);
                    }}
                >
                    <VisibilityIcon fontSize="small" />
                </IconButton>
            ),
        },
    ];

    const rows: PurchaseDto[] = data?.items || [];
    const supplierList = suppliers?.items || [];

    return (
        <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h4">Purchases</Typography>
                {canCreate && (
                    <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
                        New Purchase
                    </Button>
                )}
            </Box>

            {(!!error || !!createMutation.error) && (
                <Alert severity="error" sx={{ mb: 2 }}>
                    {getErrorMessage(error || createMutation.error)}
                </Alert>
            )}

            <Card sx={{ mb: 2 }}>
                <CardContent>
                    <TextField
                        placeholder="Search purchases..."
                        value={searchInput}
                        onChange={(e) => setSearchInput(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <SearchIcon />
                                </InputAdornment>
                            ),
                        }}
                        sx={{ width: 300 }}
                    />
                </CardContent>
            </Card>

            <Card>
                <CardContent>
                    {isLoading ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                            <CircularProgress />
                        </Box>
                    ) : (
                        <DataGrid
                            rows={rows}
                            columns={columns}
                            paginationModel={paginationModel}
                            onPaginationModelChange={setPaginationModel}
                            pageSizeOptions={[10, 25, 50]}
                            rowCount={data?.total || 0}
                            paginationMode="server"
                            autoHeight
                            getRowId={(row) => row.id}
                            disableRowSelectionOnClick
                        />
                    )}
                </CardContent>
            </Card>

            {/* Create Purchase Dialog */}
            <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
                <form onSubmit={handleSubmit}>
                    <DialogTitle>New Purchase Order</DialogTitle>
                    <DialogContent>
                        {formError && (
                            <Alert severity="error" sx={{ mb: 2 }}>
                                {formError}
                            </Alert>
                        )}
                        <Grid container spacing={2} sx={{ mt: 0 }}>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    select
                                    label="Supplier *"
                                    fullWidth
                                    value={supplierId}
                                    onChange={(e) => setSupplierId(e.target.value)}
                                >
                                    {supplierList.map((s) => (
                                        <MenuItem key={s.id} value={s.id}>
                                            {s.name}
                                        </MenuItem>
                                    ))}
                                </TextField>
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <Autocomplete
                                    freeSolo
                                    options={productSearchQuery.data || []}
                                    getOptionLabel={(option) =>
                                        typeof option === 'string' ? option : option.name
                                    }
                                    inputValue={productSearch}
                                    onInputChange={(_, value) => setProductSearch(value)}
                                    onChange={(_, value) => {
                                        if (value && typeof value !== 'string') {
                                            addProductToItems(value);
                                        }
                                    }}
                                    renderInput={(params) => (
                                        <TextField
                                            {...params}
                                            label="Search product to add..."
                                            placeholder="Type product name..."
                                        />
                                    )}
                                    isOptionEqualToValue={(option, value) => option.id === value.id}
                                />
                            </Grid>
                        </Grid>

                        {items.length > 0 && (
                            <TableContainer component={Paper} variant="outlined" sx={{ mt: 2 }}>
                                <Table size="small">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Product</TableCell>
                                            <TableCell align="right" sx={{ width: 100 }}>Quantity</TableCell>
                                            <TableCell align="right" sx={{ width: 120 }}>Unit Cost</TableCell>
                                            <TableCell align="right" sx={{ width: 120 }}>Total</TableCell>
                                            <TableCell sx={{ width: 50 }}></TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {items.map((item, idx) => (
                                            <TableRow key={idx}>
                                                <TableCell>{item.productName}</TableCell>
                                                <TableCell align="right">
                                                    <TextField
                                                        size="small"
                                                        type="number"
                                                        value={item.quantity}
                                                        onChange={(e) => updateItem(idx, 'quantity', e.target.value)}
                                                        sx={{ width: 80 }}
                                                        inputProps={{ min: 1 }}
                                                    />
                                                </TableCell>
                                                <TableCell align="right">
                                                    <TextField
                                                        size="small"
                                                        type="number"
                                                        value={item.unitCost}
                                                        onChange={(e) => updateItem(idx, 'unitCost', e.target.value)}
                                                        sx={{ width: 100 }}
                                                        inputProps={{ min: 0, step: '0.01' }}
                                                    />
                                                </TableCell>
                                                <TableCell align="right">
                                                    {formatCurrency(
                                                        (parseFloat(item.quantity) || 0) * (parseFloat(item.unitCost) || 0),
                                                    )}
                                                </TableCell>
                                                <TableCell>
                                                    <IconButton size="small" color="error" onClick={() => removeItem(idx)}>
                                                        <DeleteIcon fontSize="small" />
                                                    </IconButton>
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        )}

                        {items.length > 0 && (
                            <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 2 }}>
                                <Typography variant="h6">
                                    Grand Total: {formatCurrency(calculateTotal())}
                                </Typography>
                            </Box>
                        )}

                        <TextField
                            label="Notes"
                            fullWidth
                            multiline
                            rows={2}
                            value={notes}
                            onChange={(e) => setNotes(e.target.value)}
                            sx={{ mt: 2 }}
                        />
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
                        <Button
                            type="submit"
                            variant="contained"
                            disabled={createMutation.isLoading}
                        >
                            {createMutation.isLoading ? <CircularProgress size={24} /> : 'Create Purchase'}
                        </Button>
                    </DialogActions>
                </form>
            </Dialog>

            {/* Purchase Detail Dialog */}
            <Dialog open={detailDialogOpen} onClose={() => setDetailDialogOpen(false)} maxWidth="md" fullWidth>
                <DialogTitle>Purchase Details</DialogTitle>
                <DialogContent>
                    {detailQuery.isLoading ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                            <CircularProgress />
                        </Box>
                    ) : detailQuery.data ? (
                        <Box>
                            <Grid container spacing={2} sx={{ mb: 2 }}>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">Purchase Number</Typography>
                                    <Typography variant="body1">{detailQuery.data.purchaseNumber}</Typography>
                                </Grid>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">Date</Typography>
                                    <Typography variant="body1">
                                        {new Date(detailQuery.data.purchaseDate).toLocaleString()}
                                    </Typography>
                                </Grid>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">Supplier</Typography>
                                    <Typography variant="body1">{detailQuery.data.supplierName || '—'}</Typography>
                                </Grid>
                                <Grid item xs={6}>
                                    <Typography variant="body2" color="text.secondary">Status</Typography>
                                    <Box sx={{ mt: 0.5 }}>{getStatusChip(detailQuery.data.status)}</Box>
                                </Grid>
                            </Grid>
                            <Divider sx={{ mb: 2 }} />
                            <TableContainer component={Paper} variant="outlined">
                                <Table size="small">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Product</TableCell>
                                            <TableCell align="right">Qty</TableCell>
                                            <TableCell align="right">Unit Cost</TableCell>
                                            <TableCell align="right">Total</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {detailQuery.data.items.map((item, idx) => (
                                            <TableRow key={idx}>
                                                <TableCell>{item.productName}</TableCell>
                                                <TableCell align="right">{item.quantity}</TableCell>
                                                <TableCell align="right">{formatCurrency(item.unitCost)}</TableCell>
                                                <TableCell align="right">{formatCurrency(item.lineTotal)}</TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                            <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 2 }}>
                                <Typography variant="h6">
                                    Total: {formatCurrency(detailQuery.data.grandTotal)}
                                </Typography>
                            </Box>
                            {detailQuery.data.notes && (
                                <Box sx={{ mt: 2 }}>
                                    <Typography variant="body2" color="text.secondary">Notes</Typography>
                                    <Typography variant="body1">{detailQuery.data.notes}</Typography>
                                </Box>
                            )}
                        </Box>
                    ) : (
                        <Alert severity="error">Failed to load purchase details</Alert>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDetailDialogOpen(false)}>Close</Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
}

export default PurchasesPage;
