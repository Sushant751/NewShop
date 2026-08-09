import { useState, FormEvent } from 'react';
import { useAppDispatch, useAppSelector } from '../store';
import { changePassword } from '../store/slices/authSlice';
import {
    Box,
    Button,
    Card,
    CardContent,
    CircularProgress,
    Alert,
    Typography,
    TextField,
    Grid,
    Divider,
    Chip,
    Avatar,
} from '@mui/material';
import LockIcon from '@mui/icons-material/Lock';
import PersonIcon from '@mui/icons-material/Person';
import BusinessIcon from '@mui/icons-material/Business';

function SettingsPage() {
    const dispatch = useAppDispatch();
    const { user } = useAppSelector((state) => state.auth);

    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [formError, setFormError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);
    const [loading, setLoading] = useState(false);

    const handleChangePassword = async (e: FormEvent) => {
        e.preventDefault();
        setFormError(null);
        setSuccess(false);

        if (!currentPassword || !newPassword || !confirmPassword) {
            setFormError('All fields are required');
            return;
        }
        if (newPassword.length < 8) {
            setFormError('New password must be at least 8 characters');
            return;
        }
        if (newPassword !== confirmPassword) {
            setFormError('New passwords do not match');
            return;
        }
        if (currentPassword === newPassword) {
            setFormError('New password must be different from current password');
            return;
        }

        setLoading(true);
        try {
            await dispatch(changePassword({ currentPassword, newPassword })).unwrap();
            setSuccess(true);
            setCurrentPassword('');
            setNewPassword('');
            setConfirmPassword('');
        } catch (err) {
            setFormError(err instanceof Error ? err.message : 'Failed to change password');
        } finally {
            setLoading(false);
        }
    };

    const getInitials = (name: string) => {
        return name
            .split(' ')
            .map((n) => n[0])
            .slice(0, 2)
            .join('')
            .toUpperCase();
    };

    return (
        <Box>
            <Typography variant="h4" gutterBottom>
                Settings
            </Typography>

            <Grid container spacing={3}>
                {/* Profile Info */}
                <Grid item xs={12} md={6}>
                    <Card>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                                <PersonIcon color="primary" />
                                <Typography variant="h6">Profile Information</Typography>
                            </Box>
                            <Divider sx={{ mb: 2 }} />
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
                                <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main', fontSize: 24 }}>
                                    {user ? getInitials(user.fullName) : '?'}
                                </Avatar>
                                <Box>
                                    <Typography variant="h6">{user?.fullName}</Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        {user?.email}
                                    </Typography>
                                </Box>
                            </Box>
                            <Grid container spacing={2}>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Username
                                    </Typography>
                                    <Typography variant="body1" gutterBottom>
                                        {user?.userName}
                                    </Typography>
                                </Grid>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        User ID
                                    </Typography>
                                    <Typography variant="body2" gutterBottom sx={{ wordBreak: 'break-all' }}>
                                        {user?.userId}
                                    </Typography>
                                </Grid>
                                <Grid item xs={12}>
                                    <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                                        Roles
                                    </Typography>
                                    <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                                        {user?.roles.map((role) => (
                                            <Chip key={role} label={role} size="small" color="primary" variant="outlined" />
                                        ))}
                                    </Box>
                                </Grid>
                            </Grid>
                        </CardContent>
                    </Card>
                </Grid>

                {/* Tenant Info */}
                <Grid item xs={12} md={6}>
                    <Card>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                                <BusinessIcon color="primary" />
                                <Typography variant="h6">Tenant Information</Typography>
                            </Box>
                            <Divider sx={{ mb: 2 }} />
                            <Grid container spacing={2}>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Shop / Tenant Name
                                    </Typography>
                                    <Typography variant="body2" fontWeight="600" gutterBottom>
                                        {user?.roles?.includes('GlobalAdmin') ? 'App Admin (All Tenants)' : (user?.tenantName || 'My Shop')}
                                    </Typography>
                                </Grid>
                                <Grid item xs={12} sm={6}>
                                    <Typography variant="body2" color="text.secondary">
                                        Tenant ID
                                    </Typography>
                                    <Typography variant="body2" gutterBottom sx={{ wordBreak: 'break-all' }}>
                                        {user?.tenantId}
                                    </Typography>
                                </Grid>
                                <Grid item xs={12}>
                                    <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                                        Permissions
                                    </Typography>
                                    <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                                        {user?.permissions.map((perm) => (
                                            <Chip key={perm} label={perm} size="small" variant="outlined" />
                                        ))}
                                    </Box>
                                </Grid>
                            </Grid>
                        </CardContent>
                    </Card>
                </Grid>

                {/* Change Password */}
                <Grid item xs={12} md={6}>
                    <Card>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                                <LockIcon color="primary" />
                                <Typography variant="h6">Change Password</Typography>
                            </Box>
                            <Divider sx={{ mb: 2 }} />
                            {formError && (
                                <Alert severity="error" sx={{ mb: 2 }}>
                                    {formError}
                                </Alert>
                            )}
                            {success && (
                                <Alert severity="success" sx={{ mb: 2 }}>
                                    Password changed successfully.
                                </Alert>
                            )}
                            <form onSubmit={handleChangePassword}>
                                <Grid container spacing={2}>
                                    <Grid item xs={12}>
                                        <TextField
                                            label="Current Password"
                                            type="password"
                                            fullWidth
                                            value={currentPassword}
                                            onChange={(e) => setCurrentPassword(e.target.value)}
                                        />
                                    </Grid>
                                    <Grid item xs={12}>
                                        <TextField
                                            label="New Password"
                                            type="password"
                                            fullWidth
                                            value={newPassword}
                                            onChange={(e) => setNewPassword(e.target.value)}
                                            helperText="Minimum 8 characters"
                                        />
                                    </Grid>
                                    <Grid item xs={12}>
                                        <TextField
                                            label="Confirm New Password"
                                            type="password"
                                            fullWidth
                                            value={confirmPassword}
                                            onChange={(e) => setConfirmPassword(e.target.value)}
                                        />
                                    </Grid>
                                    <Grid item xs={12}>
                                        <Button
                                            type="submit"
                                            variant="contained"
                                            disabled={loading}
                                            startIcon={loading ? <CircularProgress size={20} /> : <LockIcon />}
                                        >
                                            Change Password
                                        </Button>
                                    </Grid>
                                </Grid>
                            </form>
                        </CardContent>
                    </Card>
                </Grid>
            </Grid>
        </Box>
    );
}

export default SettingsPage;
