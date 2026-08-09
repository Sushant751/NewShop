import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import {
    Box,
    Typography,
    Card,
    CardContent,
    Button,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    TextField,
    MenuItem,
    Chip,
    IconButton,
    Alert
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import AddIcon from '@mui/icons-material/Add';
import BlockIcon from '@mui/icons-material/Block';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import { usersApi, UserDto, CreateUserRequest, UpdateUserRequest } from '../api/endpoints';
import { useAppSelector, RootState } from '../store';
import { Roles } from '../types';

export default function StaffPage() {
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [formData, setFormData] = useState<CreateUserRequest>({
        fullName: '',
        email: '',
        phoneNumber: '',
        password: '',
        role: Roles.Clerk
    });
    const [error, setError] = useState<string | null>(null);

    const queryClient = useQueryClient();
    const currentUser = useAppSelector((state: RootState) => state.auth.user);
    const isGlobalAdmin = currentUser?.roles?.includes(Roles.GlobalAdmin);

    const { data: users, isLoading } = useQuery('users', usersApi.list);

    const createMutation = useMutation(usersApi.create, {
        onSuccess: () => {
            queryClient.invalidateQueries('users');
            setIsCreateOpen(false);
            setFormData({ fullName: '', email: '', phoneNumber: '', password: '', role: Roles.Clerk });
            setError(null);
        },
        onError: (err: any) => {
            setError(err.message || 'Failed to create user');
        }
    });

    const updateMutation = useMutation(
        ({ id, data }: { id: string, data: UpdateUserRequest }) => usersApi.update(id, data),
        {
            onSuccess: () => queryClient.invalidateQueries('users')
        }
    );

    const handleCreate = (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        createMutation.mutate(formData);
    };

    const toggleStatus = (user: UserDto) => {
        updateMutation.mutate({
            id: user.id,
            data: {
                isActive: !user.isActive,
                role: user.roles?.[0] || Roles.Clerk
            }
        });
    };

    const columns: GridColDef[] = [
        { field: 'fullName', headerName: 'Full Name', flex: 1 },
        { field: 'email', headerName: 'Email', flex: 1 },
        ...(isGlobalAdmin ? [{
            field: 'tenantName',
            headerName: 'Shop / Tenant',
            width: 180,
            renderCell: (params: any) => (
                <Chip label={params.value || '—'} size="small" color="secondary" variant="outlined" />
            )
        } as GridColDef] : []),
        { 
            field: 'role', 
            headerName: 'Role', 
            width: 150,
            valueGetter: (_: any, row: any) => row?.roles?.[0] || 'Unknown',
            renderCell: (params: any) => (
                <Chip label={params.value} size="small" color="primary" variant="outlined" />
            )
        },
        {
            field: 'isActive',
            headerName: 'Status',
            width: 120,
            renderCell: (params: any) => (
                <Chip 
                    label={params.value ? 'Active' : 'Inactive'} 
                    size="small" 
                    color={params.value ? 'success' : 'error'} 
                />
            )
        },
        {
            field: 'actions',
            headerName: 'Actions',
            width: 150,
            sortable: false,
            renderCell: (params: any) => (
                <Box>
                    <IconButton 
                        size="small" 
                        color={params.row.isActive ? 'error' : 'success'}
                        onClick={() => toggleStatus(params.row)}
                        title={params.row.isActive ? 'Deactivate' : 'Activate'}
                        disabled={params.row.id === currentUser?.userId} // Cannot toggle self
                    >
                        {params.row.isActive ? <BlockIcon /> : <CheckCircleIcon />}
                    </IconButton>
                </Box>
            )
        }
    ];

    const availableRoles = isGlobalAdmin 
        ? [Roles.GlobalAdmin, Roles.ShopAdmin, Roles.Manager, Roles.Cashier, Roles.Staff, Roles.Clerk]
        : [Roles.Clerk, Roles.Cashier, Roles.Manager];

    return (
        <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
                <Typography variant="h4" fontWeight="600" color="text.primary">
                    Staff & Users
                </Typography>
                <Button 
                    variant="contained" 
                    startIcon={<AddIcon />}
                    onClick={() => setIsCreateOpen(true)}
                >
                    Create User
                </Button>
            </Box>

            <Card sx={{ borderRadius: 2, boxShadow: '0 4px 20px rgba(0,0,0,0.05)' }}>
                <CardContent sx={{ p: 0 }}>
                    <DataGrid
                        rows={users || []}
                        columns={columns}
                        loading={isLoading}
                        autoHeight
                        disableRowSelectionOnClick
                        hideFooterSelectedRowCount
                    />
                </CardContent>
            </Card>

            <Dialog open={isCreateOpen} onClose={() => setIsCreateOpen(false)} maxWidth="sm" fullWidth>
                <form onSubmit={handleCreate}>
                    <DialogTitle>Create New User</DialogTitle>
                    <DialogContent dividers>
                        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
                        
                        <TextField
                            fullWidth
                            label="Full Name"
                            value={formData.fullName}
                            onChange={(e) => setFormData({...formData, fullName: e.target.value})}
                            margin="normal"
                            required
                        />
                        <TextField
                            fullWidth
                            label="Email"
                            type="email"
                            value={formData.email}
                            onChange={(e) => setFormData({...formData, email: e.target.value})}
                            margin="normal"
                            required
                        />
                        <TextField
                            fullWidth
                            label="Phone Number"
                            value={formData.phoneNumber}
                            onChange={(e) => setFormData({...formData, phoneNumber: e.target.value})}
                            margin="normal"
                        />
                        <TextField
                            fullWidth
                            label="Password"
                            type="password"
                            value={formData.password}
                            onChange={(e) => setFormData({...formData, password: e.target.value})}
                            margin="normal"
                            required
                            helperText="Must be at least 6 characters"
                        />
                        <TextField
                            select
                            fullWidth
                            label="Role"
                            value={formData.role}
                            onChange={(e) => setFormData({...formData, role: e.target.value})}
                            margin="normal"
                            required
                        >
                            {availableRoles.map(role => (
                                <MenuItem key={role} value={role}>{role}</MenuItem>
                            ))}
                        </TextField>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={() => setIsCreateOpen(false)}>Cancel</Button>
                        <Button 
                            type="submit" 
                            variant="contained" 
                            disabled={createMutation.isLoading}
                        >
                            {createMutation.isLoading ? 'Creating...' : 'Create'}
                        </Button>
                    </DialogActions>
                </form>
            </Dialog>
        </Box>
    );
}
