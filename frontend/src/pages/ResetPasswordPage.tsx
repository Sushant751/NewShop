import { useState, useEffect } from 'react';
import { useNavigate, Link as RouterLink, useSearchParams } from 'react-router-dom';
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
    Avatar,
    InputAdornment,
    IconButton,
} from '@mui/material';
import LockResetIcon from '@mui/icons-material/LockReset';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { authApi } from '../api/endpoints';
import { getErrorMessage } from '../utils/helpers';

function ResetPasswordPage() {
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();

    const [email, setEmail] = useState('');
    const [token, setToken] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [validationError, setValidationError] = useState('');
    const [success, setSuccess] = useState(false);

    useEffect(() => {
        const emailParam = searchParams.get('email') || '';
        const tokenParam = searchParams.get('token') || '';
        setEmail(emailParam);
        setToken(tokenParam);
    }, [searchParams]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setValidationError('');

        if (newPassword !== confirmPassword) {
            setValidationError('Passwords do not match');
            return;
        }
        if (newPassword.length < 8) {
            setValidationError('Password must be at least 8 characters');
            return;
        }
        if (!email || !token) {
            setValidationError('Email and reset token are required. Please use the link from your email.');
            return;
        }

        setLoading(true);
        try {
            await authApi.resetPassword({ email, token, newPassword });
            setSuccess(true);
            setTimeout(() => navigate('/login'), 3000);
        } catch (err) {
            setError(getErrorMessage(err, 'Failed to reset password'));
        } finally {
            setLoading(false);
        }
    };

    return (
        <Container component="main" maxWidth="sm">
            <Box
                sx={{
                    marginTop: 8,
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                }}
            >
                <Avatar sx={{ m: 1, bgcolor: 'primary.main', width: 56, height: 56 }}>
                    <LockResetIcon fontSize="large" />
                </Avatar>
                <Typography component="h1" variant="h4" sx={{ mb: 1 }}>
                    Reset Password
                </Typography>
                <Typography variant="subtitle1" color="textSecondary" sx={{ mb: 3 }}>
                    Enter your new password
                </Typography>

                <Card sx={{ width: '100%', maxWidth: 440 }}>
                    <CardContent sx={{ p: 4 }}>
                        {error && (
                            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
                                {error}
                            </Alert>
                        )}
                        {(validationError || (!email || !token)) && (
                            <Alert severity="warning" sx={{ mb: 2 }} onClose={() => setValidationError('')}>
                                {validationError || 'Email or token missing. Please use the link from your reset email.'}
                            </Alert>
                        )}
                        {success ? (
                            <Box>
                                <Alert severity="success" sx={{ mb: 2 }}>
                                    Your password has been reset successfully! Redirecting to login...
                                </Alert>
                                <Button
                                    fullWidth
                                    variant="outlined"
                                    component={RouterLink}
                                    to="/login"
                                >
                                    Go to Sign In
                                </Button>
                            </Box>
                        ) : (
                            <Box component="form" onSubmit={handleSubmit}>
                                <Grid container spacing={2}>
                                    <Grid item xs={12}>
                                        <TextField
                                            label="Email Address"
                                            type="email"
                                            value={email}
                                            onChange={(e) => setEmail(e.target.value)}
                                            required
                                            autoComplete="email"
                                        />
                                    </Grid>
                                    <Grid item xs={12}>
                                        <TextField
                                            label="Reset Token"
                                            value={token}
                                            onChange={(e) => setToken(e.target.value)}
                                            required
                                            helperText="The token from your reset email"
                                        />
                                    </Grid>
                                    <Grid item xs={12}>
                                        <TextField
                                            label="New Password"
                                            type={showPassword ? 'text' : 'password'}
                                            value={newPassword}
                                            onChange={(e) => setNewPassword(e.target.value)}
                                            required
                                            autoComplete="new-password"
                                            InputProps={{
                                                endAdornment: (
                                                    <InputAdornment position="end">
                                                        <IconButton
                                                            onClick={() => setShowPassword(!showPassword)}
                                                            edge="end"
                                                        >
                                                            {showPassword ? <VisibilityOffIcon /> : <VisibilityIcon />}
                                                        </IconButton>
                                                    </InputAdornment>
                                                ),
                                            }}
                                        />
                                    </Grid>
                                    <Grid item xs={12}>
                                        <TextField
                                            label="Confirm New Password"
                                            type={showPassword ? 'text' : 'password'}
                                            value={confirmPassword}
                                            onChange={(e) => setConfirmPassword(e.target.value)}
                                            required
                                            autoComplete="new-password"
                                        />
                                    </Grid>
                                </Grid>
                                <Button
                                    type="submit"
                                    fullWidth
                                    variant="contained"
                                    size="large"
                                    disabled={loading}
                                    sx={{ mt: 3, mb: 2 }}
                                >
                                    {loading ? 'Resetting...' : 'Reset Password'}
                                </Button>
                                <Grid container justifyContent="center">
                                    <Grid item>
                                        <Link component={RouterLink} to="/login" variant="body2">
                                            Back to Sign In
                                        </Link>
                                    </Grid>
                                </Grid>
                            </Box>
                        )}
                    </CardContent>
                </Card>
            </Box>
        </Container>
    );
}

export default ResetPasswordPage;
