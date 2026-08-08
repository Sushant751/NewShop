import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from 'react-query';
import {
    Box,
    Button,
    Card,
    CardContent,
    CircularProgress,
    Alert,
    Typography,
    Chip,
} from '@mui/material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { salesApi } from '../api/endpoints';
import type { SaleDto } from '../types';
import { getErrorMessage } from '../utils/helpers';

function SalesPage() {
    const navigate = useNavigate();
    const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
        page: 0,
        pageSize: 10,
    });

    const { data, isLoading, error } = useQuery(
        ['sales', paginationModel.page, paginationModel.pageSize],
        () =>
            salesApi.list({
                page: paginationModel.page + 1,
                pageSize: paginationModel.pageSize,
            }),
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

    const columns: GridColDef[] = [
        { field: 'invoiceNumber', headerName: 'Invoice #', flex: 1, minWidth: 120 },
        {
            field: 'saleDate',
            headerName: 'Date',
            flex: 1,
            minWidth: 150,
            valueFormatter: (value: string) => new Date(value).toLocaleString(),
        },
        { field: 'customerName', headerName: 'Customer', flex: 1, minWidth: 120 },
        {
            field: 'status',
            headerName: 'Status',
            flex: 0.8,
            minWidth: 100,
            renderCell: (params) => getStatusChip(params.value as string),
        },
        {
            field: 'paymentStatus',
            headerName: 'Payment',
            flex: 0.8,
            minWidth: 100,
            renderCell: (params) => getPaymentChip(params.value as string),
        },
        {
            field: 'grandTotal',
            headerName: 'Total',
            flex: 0.7,
            minWidth: 80,
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
                <Button
                    size="small"
                    startIcon={<VisibilityIcon />}
                    onClick={() => navigate(`/sales/${params.row.id}`)}
                >
                    View
                </Button>
            ),
        },
    ];

    const rows: SaleDto[] = data?.items || [];

    return (
        <Box>
            <Typography variant="h4" gutterBottom>
                Sales
            </Typography>

            {!!error && (
                <Alert severity="error" sx={{ mb: 2 }}>
                    {getErrorMessage(error, 'Failed to load sales')}
                </Alert>
            )}

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
        </Box>
    );
}

export default SalesPage;
