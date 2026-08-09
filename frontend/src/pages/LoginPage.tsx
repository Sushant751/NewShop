import { useState } from 'react';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import {
    Box,
    Button,
    Card,
    CardContent,
    Container,
    Grid,
    Link,
    TextField,
    Typography,
    Alert,
    InputAdornment,
    IconButton,
    Chip,
} from '@mui/material';
import VisibilityIcon from '@mui/icons-material/VisibilityOutlined';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOffOutlined';
import StorefrontIcon from '@mui/icons-material/Storefront';
import { useAppDispatch, useAppSelector } from '../store';
import { loginUser, clearError } from '../store/slices/authSlice';

function LoginPage() {
    const navigate = useNavigate();
    const dispatch = useAppDispatch();
    const { isLoading, error } = useAppSelector((state) => state.auth);

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [tenantSlug, setTenantSlug] = useState('');
    const [showPassword, setShowPassword] = useState(false);

    const handleDemoLogin = (demoEmail: string, demoPass: string) => {
        setEmail(demoEmail);
        setPassword(demoPass);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        dispatch(clearError());
        const result = await dispatch(
            loginUser({
                email,
                password,
                tenantSlug: tenantSlug || undefined,
            }),
        );
        if (loginUser.fulfilled.match(result)) {
            navigate('/dashboard');
        }
    };

    return (
        <Box
            sx={{
                minHeight: '100vh',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: '#f4f6f8',
                py: 6,
            }}
        >
            <Container maxWidth="xs">
                <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', mb: 3 }}>
                    <Box
                        sx={{
                            bgcolor: '#4680ff',
                            color: '#ffffff',
                            borderRadius: 2.5,
                            p: 1.5,
                            mb: 2,
                            display: 'flex',
                            boxShadow: '0 4px 14px rgba(70, 128, 255, 0.35)',
                        }}
                    >
                        <StorefrontIcon sx={{ fontSize: 36 }} />
                    </Box>
                    <Typography variant="h4" sx={{ fontWeight: 700, color: '#1d2630', mb: 0.5 }}>
                        Easy Billing
                    </Typography>
                    <Typography variant="body2" color="textSecondary">
                        Multi-Tenant POS & Enterprise Inventory SaaS
                    </Typography>
                </Box>

                <Card
                    sx={{
                        borderRadius: 3,
                        border: '1px solid #e6ebf1',
                        boxShadow: '0px 4px 20px rgba(32, 40, 45, 0.06)',
                    }}
                >
                    <CardContent sx={{ p: 4 }}>
                        <Typography variant="h5" sx={{ fontWeight: 700, mb: 1 }}>
                            Sign In
                        </Typography>
                        <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
                            Enter your account credentials to access your store workspace.
                        </Typography>

                        {error && (
                            <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }} onClose={() => dispatch(clearError())}>
                                {error}
                            </Alert>
                        )}

                        <Box component="form" onSubmit={handleSubmit}>
                            <Grid container spacing={2.5}>
                                <Grid item xs={12}>
                                    <TextField
                                        label="Email Address"
                                        type="email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        required
                                        autoComplete="email"
                                        autoFocus
                                        placeholder="admin@billingsystem.com"
                                    />
                                </Grid>
                                <Grid item xs={12}>
                                    <TextField
                                        label="Password"
                                        type={showPassword ? 'text' : 'password'}
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        required
                                        autoComplete="current-password"
                                        InputProps={{
                                            endAdornment: (
                                                <InputAdornment position="end">
                                                    <IconButton
                                                        onClick={() => setShowPassword(!showPassword)}
                                                        edge="end"
                                                        size="small"
                                                    >
                                                        {showPassword ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
                                                    </IconButton>
                                                </InputAdornment>
                                            ),
                                        }}
                                    />
                                </Grid>
                                <Grid item xs={12}>
                                    <TextField
                                        label="Tenant Workspace Slug (optional)"
                                        value={tenantSlug}
                                        onChange={(e) => setTenantSlug(e.target.value)}
                                        placeholder="e.g. demo-shop"
                                        helperText="Leave blank if accessing default single-tenant database"
                                    />
                                </Grid>
                            </Grid>

                            <Button
                                type="submit"
                                fullWidth
                                variant="contained"
                                size="large"
                                disabled={isLoading}
                                sx={{
                                    mt: 3,
                                    mb: 2.5,
                                    py: 1.2,
                                    fontSize: '0.9375rem',
                                    borderRadius: 2,
                                    bgcolor: '#4680ff',
                                    '&:hover': { bgcolor: '#2b63d9' },
                                }}
                            >
                                {isLoading ? 'Signing In...' : 'Sign In'}
                            </Button>

                            <Grid container justifyContent="space-between" alignItems="center">
                                <Grid item>
                                    <Link component={RouterLink} to="/forgot-password" variant="body2" underline="hover" sx={{ color: '#4680ff' }}>
                                        Forgot password?
                                    </Link>
                                </Grid>
                                <Grid item>
                                    <Link component={RouterLink} to="/register" variant="body2" underline="hover" sx={{ color: '#4680ff' }}>
                                        Create Store Account
                                    </Link>
                                </Grid>
                            </Grid>
                        </Box>
                    </CardContent>
                </Card>

                {/* Demo Credentials Box */}
                <Box
                    sx={{
                        mt: 3,
                        p: 2,
                        borderRadius: 2.5,
                        bgcolor: '#ffffff',
                        border: '1px solid #e6ebf1',
                        textAlign: 'center',
                    }}
                >
                    <Typography variant="caption" sx={{ color: '#5b6b79', fontWeight: 600, display: 'block', mb: 1 }}>
                        💡 Quick Demo Login (Click to auto-fill)
                    </Typography>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
                            <Chip label="Admin" size="small" variant="outlined" onClick={() => handleDemoLogin('admin@billingsystem.com', 'Admin@123')} sx={{ fontSize: '0.75rem', cursor: 'pointer', '&:hover': { bgcolor: '#f4f6f8' } }} />
                        </Box>
                        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
                            <Chip label="Shop Admin" size="small" variant="outlined" onClick={() => handleDemoLogin('shopadmin@demo.com', 'ShopAdmin@123')} sx={{ fontSize: '0.75rem', cursor: 'pointer', '&:hover': { bgcolor: '#f4f6f8' } }} />
                            <Chip label="Clerk" size="small" variant="outlined" onClick={() => handleDemoLogin('clerk@demo.com', 'Clerk@123')} sx={{ fontSize: '0.75rem', cursor: 'pointer', '&:hover': { bgcolor: '#f4f6f8' } }} />
                        </Box>
                    </Box>
                </Box>
            </Container>
        </Box>
    );
}

export default LoginPage;
