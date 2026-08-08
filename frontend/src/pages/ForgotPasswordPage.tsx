import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
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
} from '@mui/material';
import LockResetIcon from '@mui/icons-material/LockReset';
import { authApi } from '../api/endpoints';
import { getErrorMessage } from '../utils/helpers';

function ForgotPasswordPage() {
    const [email, setEmail] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setLoading(true);
        try {
            await authApi.forgotPassword({ email });
            setSuccess(true);
        } catch (err) {
            setError(getErrorMessage(err, 'Failed to send reset email'));
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
                    Forgot Password
                </Typography>
                <Typography variant="subtitle1" color="textSecondary" sx={{ mb: 3 }}>
                    Enter your email to receive a reset link
                </Typography>

                <Card sx={{ width: '100%', maxWidth: 440 }}>
                    <CardContent sx={{ p: 4 }}>
                        {error && (
                            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
                                {error}
                            </Alert>
                        )}
                        {success ? (
                            <Box>
                                <Alert severity="success" sx={{ mb: 2 }}>
                                    If an account exists for {email}, a password reset link has been sent.
                                </Alert>
                                <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                                    Please check your email inbox and follow the instructions to reset your password.
                                    The link will expire in 1 hour.
                                </Typography>
                                <Button
                                    fullWidth
                                    variant="outlined"
                                    component={RouterLink}
                                    to="/login"
                                >
                                    Back to Sign In
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
                                            autoFocus
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
                                    {loading ? 'Sending...' : 'Send Reset Link'}
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

export default ForgotPasswordPage;
