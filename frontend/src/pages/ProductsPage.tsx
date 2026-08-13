import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import {
    Box,
    Button,
    Card,
    CardContent,
    CircularProgress,
    Alert,
    Dialog,
    DialogActions,
    DialogContent,
    DialogContentText,
    DialogTitle,
    IconButton,
    TextField,
    InputAdornment,
    Chip,
    Typography,
} from '@mui/material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import SearchIcon from '@mui/icons-material/Search';
import { productsApi } from '../api/endpoints';
import { useAppSelector } from '../store';
import { Permissions, Roles } from '../types';
import type { ProductDto } from '../types';
import { getErrorMessage } from '../utils/helpers';

function ProductsPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const user = useAppSelector((state) => state.auth.user);

    const [search, setSearch] = useState('');
    const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
        page: 0,
        pageSize: 10,
    });
    const [deleteId, setDeleteId] = useState<string | null>(null);

    const isGlobalAdmin = user?.roles?.includes(Roles.GlobalAdmin);
    const isShopAdmin = user?.roles?.includes(Roles.ShopAdmin);
    const canCreate = isGlobalAdmin || isShopAdmin || user?.permissions?.includes(Permissions.ProductsCreate) || false;
    const canEdit = isGlobalAdmin || isShopAdmin || user?.permissions?.includes(Permissions.ProductsEdit) || false;
    const canDelete = isGlobalAdmin || isShopAdmin || user?.permissions?.includes(Permissions.ProductsDelete) || false;

    const { data, isLoading, error } = useQuery(
        ['products', paginationModel.page, paginationModel.pageSize, search],
        () =>
            productsApi.list({
                page: paginationModel.page + 1,
                pageSize: paginationModel.pageSize,
                search: search || undefined,
            }),
    );

    const deleteMutation = useMutation((id: string) => productsApi.delete(id), {
        onSuccess: () => {
            queryClient.invalidateQueries('products');
            setDeleteId(null);
        },
    });

    const columns: GridColDef[] = [
        { field: 'name', headerName: 'Name', flex: 1.5, minWidth: 150 },
        { field: 'sku', headerName: 'SKU', flex: 1, minWidth: 100 },
        { field: 'categoryName', headerName: 'Category', flex: 1, minWidth: 120 },
        { field: 'brandName', headerName: 'Brand', flex: 1, minWidth: 100 },
        { field: 'unitName', headerName: 'Unit', flex: 0.5, minWidth: 60 },
        {
            field: 'costPrice',
            headerName: 'Cost',
            flex: 0.7,
            minWidth: 80,
            type: 'number',
            valueFormatter: (value: number) => `₹${value.toFixed(2)}`,
        },
        {
            field: 'sellingPrice',
            headerName: 'Price',
            flex: 0.7,
            minWidth: 80,
            type: 'number',
            valueFormatter: (value: number) => `₹${value.toFixed(2)}`,
        },
        {
            field: 'taxRate',
            headerName: 'Tax',
            flex: 0.7,
            minWidth: 70,
            type: 'number',
            valueFormatter: (value: number) => `${value}%`,
        },
        {
            field: 'currentStock',
            headerName: 'Stock',
            flex: 0.7,
            minWidth: 80,
            type: 'number',
            renderCell: (params) => {
                const stock = params.value as number;
                const reorder = params.row.reorderLevel as number;
                const isLow = stock <= reorder;
                return (
                    <Chip
                        label={stock}
                        size="small"
                        color={isLow ? 'error' : 'success'}
                        variant={isLow ? 'filled' : 'outlined'}
                    />
                );
            },
        },
        {
            field: 'isActive',
            headerName: 'Status',
            flex: 0.7,
            minWidth: 80,
            renderCell: (params) => (
                <Chip
                    label={params.value ? 'Active' : 'Inactive'}
                    size="small"
                    color={params.value ? 'success' : 'default'}
                    variant="outlined"
                />
            ),
        },
        {
            field: 'actions',
            headerName: 'Actions',
            flex: 0.8,
            minWidth: 100,
            sortable: false,
            renderCell: (params) => (
                <Box>
                    {canEdit && (
                        <IconButton
                            size="small"
                            onClick={() => navigate(`/products/${params.row.id}/edit`)}
                            color="primary"
                        >
                            <EditIcon fontSize="small" />
                        </IconButton>
                    )}
                    {canDelete && (
                        <IconButton
                            size="small"
                            onClick={() => setDeleteId(params.row.id as string)}
                            color="error"
                        >
                            <DeleteIcon fontSize="small" />
                        </IconButton>
                    )}
                </Box>
            ),
        },
    ];

    const rows: ProductDto[] = data?.items || [];

    return (
        <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
                <Typography variant="h4">Products</Typography>
                {canCreate && (
                    <Button
                        variant="contained"
                        startIcon={<AddIcon />}
                        onClick={() => navigate('/products/new')}
                    >
                        Add Product
                    </Button>
                )}
            </Box>

            {!!error && (
                <Alert severity="error" sx={{ mb: 2 }}>
                    {getErrorMessage(error, 'Failed to load products')}
                </Alert>
            )}

            <Card>
                <CardContent>
                    <Box sx={{ mb: 2 }}>
                        <TextField
                            placeholder="Search products by name, SKU, or barcode..."
                            value={search}
                            onChange={(e) => {
                                setSearch(e.target.value);
                                setPaginationModel((prev) => ({ ...prev, page: 0 }));
                            }}
                            sx={{ maxWidth: 400 }}
                            InputProps={{
                                startAdornment: (
                                    <InputAdornment position="start">
                                        <SearchIcon />
                                    </InputAdornment>
                                ),
                            }}
                        />
                    </Box>

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

            {/* Delete Confirmation Dialog */}
            <Dialog open={!!deleteId} onClose={() => setDeleteId(null)}>
                <DialogTitle>Delete Product</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        Are you sure you want to delete this product? This action cannot be undone.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteId(null)}>Cancel</Button>
                    <Button
                        onClick={() => deleteId && deleteMutation.mutate(deleteId)}
                        color="error"
                        variant="contained"
                        disabled={deleteMutation.isLoading}
                    >
                        {deleteMutation.isLoading ? 'Deleting...' : 'Delete'}
                    </Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
}

export default ProductsPage;
