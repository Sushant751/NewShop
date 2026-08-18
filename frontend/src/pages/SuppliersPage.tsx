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
} from '@mui/material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import SearchIcon from '@mui/icons-material/Search';
import AddIcon from '@mui/icons-material/Add';
import { suppliersApi } from '../api/endpoints';
import { Permissions } from '../types';
import type { SupplierDto, CreateSupplierRequest } from '../types';
import { hasPermission } from '../components/ProtectedRoute';
import { useAppSelector } from '../store';
import { getErrorMessage } from '../utils/helpers';

interface SupplierFormData {
    name: string;
    email: string;
    phone: string;
    address: string;
    contactPerson: string;
}

const emptyForm: SupplierFormData = {
    name: '',
    email: '',
    phone: '',
    address: '',
    contactPerson: '',
};

function SuppliersPage() {
    const queryClient = useQueryClient();
    const user = useAppSelector((state) => state.auth.user);
    const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
        page: 0,
        pageSize: 10,
    });
    const [search, setSearch] = useState('');
    const [searchInput, setSearchInput] = useState('');

    const [dialogOpen, setDialogOpen] = useState(false);
    const [form, setForm] = useState<SupplierFormData>(emptyForm);
    const [formError, setFormError] = useState<string | null>(null);

    const canCreate = hasPermission(user, Permissions.PurchasesCreate);

    const { data, isLoading, error } = useQuery(
        ['suppliers', paginationModel.page, paginationModel.pageSize, search],
        () =>
            suppliersApi.list({
                page: paginationModel.page + 1,
                pageSize: paginationModel.pageSize,
                search: search || undefined,
            }),
    );

    const createMutation = useMutation(
        (data: CreateSupplierRequest) => suppliersApi.create(data),
        {
            onSuccess: () => {
                queryClient.invalidateQueries(['suppliers']);
                queryClient.invalidateQueries(['suppliers-all']);
                setDialogOpen(false);
            },
        },
    );

    const handleSearch = () => {
        setSearch(searchInput);
        setPaginationModel((prev) => ({ ...prev, page: 0 }));
    };

    const openCreate = () => {
        setForm(emptyForm);
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

        createMutation.mutate({
            name: form.name.trim(),
            email: form.email.trim() || null,
            phone: form.phone.trim() || null,
            address: form.address.trim() || null,
            contactPerson: form.contactPerson.trim() || null,
        });
    };

    const columns: GridColDef[] = [
        { field: 'name', headerName: 'Name', flex: 1, minWidth: 150 },
        { field: 'contactPerson', headerName: 'Contact Person', flex: 1, minWidth: 150 },
        { field: 'email', headerName: 'Email', flex: 1, minWidth: 180 },
        { field: 'phone', headerName: 'Phone', flex: 0.8, minWidth: 120 },
        { field: 'address', headerName: 'Address', flex: 1.5, minWidth: 200 },
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
    ];

    const rows: SupplierDto[] = data?.items || [];

    return (
        <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h4">Suppliers</Typography>
                {canCreate && (
                    <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
                        Add Supplier
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
                        placeholder="Search suppliers..."
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

            {/* Create Dialog */}
            <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth>
                <form onSubmit={handleSubmit}>
                    <DialogTitle>Add Supplier</DialogTitle>
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
                                    label="Contact Person"
                                    fullWidth
                                    value={form.contactPerson}
                                    onChange={(e) => setForm({ ...form, contactPerson: e.target.value })}
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
                                    label="Email"
                                    fullWidth
                                    type="email"
                                    value={form.email}
                                    onChange={(e) => setForm({ ...form, email: e.target.value })}
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
                        </Grid>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
                        <Button
                            type="submit"
                            variant="contained"
                            disabled={createMutation.isLoading}
                        >
                            {createMutation.isLoading ? <CircularProgress size={24} /> : 'Create'}
                        </Button>
                    </DialogActions>
                </form>
            </Dialog>
        </Box>
    );
}

export default SuppliersPage;
