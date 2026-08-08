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
    Switch,
    FormControlLabel,
    IconButton,
} from '@mui/material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import SearchIcon from '@mui/icons-material/Search';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import { customersApi } from '../api/endpoints';
import { Permissions } from '../types';
import type { CustomerDto, CreateCustomerRequest, UpdateCustomerRequest } from '../types';
import { hasPermission } from '../components/ProtectedRoute';
import { useAppSelector } from '../store';
import { getErrorMessage } from '../utils/helpers';

interface CustomerFormData {
    name: string;
    email: string;
    phone: string;
    address: string;
    creditLimit: string;
    isActive: boolean;
}

const emptyForm: CustomerFormData = {
    name: '',
    email: '',
    phone: '',
    address: '',
    creditLimit: '0',
    isActive: true,
};

function CustomersPage() {
    const queryClient = useQueryClient();
    const user = useAppSelector((state) => state.auth.user);
    const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
        page: 0,
        pageSize: 10,
    });
    const [search, setSearch] = useState('');
    const [searchInput, setSearchInput] = useState('');

    const [dialogOpen, setDialogOpen] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [form, setForm] = useState<CustomerFormData>(emptyForm);
    const [formError, setFormError] = useState<string | null>(null);

    const [deleteId, setDeleteId] = useState<string | null>(null);

    const canCreate = hasPermission(user, Permissions.CustomersCreate);
    const canEdit = hasPermission(user, Permissions.CustomersEdit);
    const canDelete = hasPermission(user, Permissions.CustomersDelete);

    const { data, isLoading, error } = useQuery(
        ['customers', paginationModel.page, paginationModel.pageSize, search],
        () =>
            customersApi.list({
                page: paginationModel.page + 1,
                pageSize: paginationModel.pageSize,
                search: search || undefined,
            }),
    );

    const createMutation = useMutation(
        (data: CreateCustomerRequest) => customersApi.create(data),
        {
            onSuccess: () => {
                queryClient.invalidateQueries(['customers']);
                setDialogOpen(false);
            },
        },
    );

    const updateMutation = useMutation(
        (params: { id: string; data: UpdateCustomerRequest }) =>
            customersApi.update(params.id, params.data),
        {
            onSuccess: () => {
                queryClient.invalidateQueries(['customers']);
                setDialogOpen(false);
            },
        },
    );

    const deleteMutation = useMutation(
        (id: string) => customersApi.delete(id),
        {
            onSuccess: () => {
                queryClient.invalidateQueries(['customers']);
                setDeleteId(null);
            },
        },
    );

    const formatCurrency = (val: number) => `₹${val.toFixed(2)}`;

    const handleSearch = () => {
        setSearch(searchInput);
        setPaginationModel((prev) => ({ ...prev, page: 0 }));
    };

    const openCreate = () => {
        setEditingId(null);
        setForm(emptyForm);
        setFormError(null);
        setDialogOpen(true);
    };

    const openEdit = (customer: CustomerDto) => {
        setEditingId(customer.id);
        setForm({
            name: customer.name,
            email: customer.email || '',
            phone: customer.phone || '',
            address: customer.address || '',
            creditLimit: String(customer.creditLimit),
            isActive: customer.isActive,
        });
        setFormError(null);
        setDialogOpen(true);
    };

    const handleSubmit = (e: FormEvent) => {
        e.preventDefault();
        setFormError(null);

        if (!form.name.trim()) {
            setFormError('Name is required');
            return;
        }

        const creditLimitNum = parseFloat(form.creditLimit) || 0;

        if (editingId) {
            updateMutation.mutate({
                id: editingId,
                data: {
                    name: form.name.trim(),
                    email: form.email.trim() || null,
                    phone: form.phone.trim() || null,
                    address: form.address.trim() || null,
                    creditLimit: creditLimitNum,
                    isActive: form.isActive,
                },
            });
        } else {
            createMutation.mutate({
                name: form.name.trim(),
                email: form.email.trim() || null,
                phone: form.phone.trim() || null,
                address: form.address.trim() || null,
                creditLimit: creditLimitNum,
            });
        }
    };

    const columns: GridColDef[] = [
        { field: 'name', headerName: 'Name', flex: 1, minWidth: 150 },
        { field: 'email', headerName: 'Email', flex: 1, minWidth: 180 },
        { field: 'phone', headerName: 'Phone', flex: 0.8, minWidth: 120 },
        {
            field: 'creditLimit',
            headerName: 'Credit Limit',
            flex: 0.7,
            minWidth: 100,
            type: 'number',
            valueFormatter: (value: number) => formatCurrency(value),
        },
        {
            field: 'currentBalance',
            headerName: 'Balance',
            flex: 0.7,
            minWidth: 100,
            type: 'number',
            valueFormatter: (value: number) => formatCurrency(value),
        },
        {
            field: 'isActive',
            headerName: 'Active',
            flex: 0.5,
            minWidth: 80,
            renderCell: (params) =>
                params.value ? (
                    <Chip label="Active" size="small" color="success" variant="outlined" />
                ) : (
                    <Chip label="Inactive" size="small" color="default" variant="outlined" />
                ),
        },
        {
            field: 'actions',
            headerName: 'Actions',
            flex: 0.7,
            minWidth: 100,
            sortable: false,
            renderCell: (params) => (
                <Box sx={{ display: 'flex', gap: 0.5 }}>
                    {canEdit && (
                        <IconButton size="small" onClick={() => openEdit(params.row as CustomerDto)}>
                            <EditIcon fontSize="small" />
                        </IconButton>
                    )}
                    {canDelete && (
                        <IconButton
                            size="small"
                            color="error"
                            onClick={() => setDeleteId(params.row.id)}
                        >
                            <DeleteIcon fontSize="small" />
                        </IconButton>
                    )}
                </Box>
            ),
        },
    ];

    const rows: CustomerDto[] = data?.items || [];
    const mutationError = createMutation.error || updateMutation.error || deleteMutation.error;

    return (
        <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h4">Customers</Typography>
                {canCreate && (
                    <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
                        Add Customer
                    </Button>
                )}
            </Box>

            {(!!error || !!mutationError) && (
                <Alert severity="error" sx={{ mb: 2 }}>
                    {getErrorMessage(error || mutationError)}
                </Alert>
            )}

            <Card sx={{ mb: 2 }}>
                <CardContent>
                    <TextField
                        placeholder="Search customers..."
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

            {/* Create/Edit Dialog */}
            <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth>
                <form onSubmit={handleSubmit}>
                    <DialogTitle>{editingId ? 'Edit Customer' : 'Add Customer'}</DialogTitle>
                    <DialogContent>
                        {formError && (
                            <Alert severity="error" sx={{ mb: 2 }}>
                                {formError}
                            </Alert>
                        )}
                        <Grid container spacing={2} sx={{ mt: 0 }}>
                            <Grid item xs={12}>
                                <TextField
                                    label="Name *"
                                    fullWidth
                                    value={form.name}
                                    onChange={(e) => setForm({ ...form, name: e.target.value })}
                                />
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    label="Email"
                                    fullWidth
                                    type="email"
                                    value={form.email}
                                    onChange={(e) => setForm({ ...form, email: e.target.value })}
                                />
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    label="Phone"
                                    fullWidth
                                    value={form.phone}
                                    onChange={(e) => setForm({ ...form, phone: e.target.value })}
                                />
                            </Grid>
                            <Grid item xs={12}>
                                <TextField
                                    label="Address"
                                    fullWidth
                                    multiline
                                    rows={2}
                                    value={form.address}
                                    onChange={(e) => setForm({ ...form, address: e.target.value })}
                                />
                            </Grid>
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    label="Credit Limit"
                                    fullWidth
                                    type="number"
                                    value={form.creditLimit}
                                    onChange={(e) => setForm({ ...form, creditLimit: e.target.value })}
                                />
                            </Grid>
                            {editingId && (
                                <Grid item xs={12} sm={6}>
                                    <FormControlLabel
                                        control={
                                            <Switch
                                                checked={form.isActive}
                                                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                                            />
                                        }
                                        label="Active"
                                    />
                                </Grid>
                            )}
                        </Grid>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
                        <Button
                            type="submit"
                            variant="contained"
                            disabled={createMutation.isLoading || updateMutation.isLoading}
                        >
                            {createMutation.isLoading || updateMutation.isLoading ? (
                                <CircularProgress size={24} />
                            ) : editingId ? (
                                'Update'
                            ) : (
                                'Create'
                            )}
                        </Button>
                    </DialogActions>
                </form>
            </Dialog>

            {/* Delete Confirmation */}
            <Dialog open={!!deleteId} onClose={() => setDeleteId(null)}>
                <DialogTitle>Delete Customer</DialogTitle>
                <DialogContent>
                    <Typography>Are you sure you want to delete this customer? This action cannot be undone.</Typography>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteId(null)}>Cancel</Button>
                    <Button
                        color="error"
                        variant="contained"
                        disabled={deleteMutation.isLoading}
                        onClick={() => deleteId && deleteMutation.mutate(deleteId)}
                    >
                        {deleteMutation.isLoading ? <CircularProgress size={24} /> : 'Delete'}
                    </Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
}

export default CustomersPage;
