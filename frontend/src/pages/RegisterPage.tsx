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
    Avatar,
} from '@mui/material';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useAppDispatch, useAppSelector } from '../store';
import { registerUser, clearError } from '../store/slices/authSlice';

function RegisterPage() {
    const navigate = useNavigate();
    const dispatch = useAppDispatch();
    const { isLoading, error } = useAppSelector((state) => state.auth);

    const [fullName, setFullName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [phoneNumber, setPhoneNumber] = useState('');
    const [tenantName, setTenantName] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [validationError, setValidationError] = useState('');

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setValidationError('');
        dispatch(clearError());

        if (password !== confirmPassword) {
            setValidationError('Passwords do not match');
            return;
        }
        if (password.length < 8) {
            setValidationError('Password must be at least 8 characters');
            return;
        }

        const result = await dispatch(
            registerUser({
                fullName,
                email,
                password,
                phoneNumber: phoneNumber || undefined,
                tenantName: tenantName || undefined,
            }),
        );
        if (registerUser.fulfilled.match(result)) {
            navigate('/dashboard');
        }
    };

    return (
        <Container component="main" maxWidth="sm">
            <Box
                sx={{
                    marginTop: 4,
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                }}
            >
                <Avatar sx={{ m: 1, bgcolor: 'secondary.main', width: 56, height: 56 }}>
                    <PersonAddIcon fontSize="large" />
                </Avatar>
                <Typography component="h1" variant="h4" sx={{ mb: 1 }}>
                    Create Account
                </Typography>
                <Typography variant="subtitle1" color="textSecondary" sx={{ mb: 3 }}>
                    Register your business
                </Typography>

                <Card sx={{ width: '100%', maxWidth: 480 }}>
                    <CardContent sx={{ p: 4 }}>
                        {(error || validationError) && (
                            <Alert
                                severity="error"
                                sx={{ mb: 2 }}
                                onClose={() => {
                                    dispatch(clearError());
                                    setValidationError('');
                                }}
                            >
                                {validationError || error}
                            </Alert>
                        )}
                        <Box component="form" onSubmit={handleSubmit}>
                            <Grid container spacing={2}>
                                <Grid item xs={12}>
                                    <TextField
                                        label="Full Name"
                                        value={fullName}
                                        onChange={(e) => setFullName(e.target.value)}
                                        required
                                        autoFocus
                                    />
                                </Grid>
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
                                <Grid item xs={12} sm={6}>
                                    <TextField
                                        label="Phone Number"
                                        value={phoneNumber}
                                        onChange={(e) => setPhoneNumber(e.target.value)}
                                        fullWidth
                                    />
                                </Grid>
                                <Grid item xs={12} sm={6}>
                                    <TextField
                                        label="Business Name"
                                        value={tenantName}
                                        onChange={(e) => setTenantName(e.target.value)}
                                        helperText="Your shop/company name"
                                        fullWidth
                                    />
                                </Grid>
                                <Grid item xs={12} sm={6}>
                                    <TextField
                                        label="Password"
                                        type={showPassword ? 'text' : 'password'}
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        required
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
                                        fullWidth
                                    />
                                </Grid>
                                <Grid item xs={12} sm={6}>
                                    <TextField
                                        label="Confirm Password"
                                        type={showPassword ? 'text' : 'password'}
                                        value={confirmPassword}
                                        onChange={(e) => setConfirmPassword(e.target.value)}
                                        required
                                        fullWidth
                                    />
                                </Grid>
                            </Grid>
                            <Button
                                type="submit"
                                fullWidth
                                variant="contained"
                                size="large"
                                disabled={isLoading}
                                sx={{ mt: 3, mb: 2 }}
                            >
                                {isLoading ? 'Creating account...' : 'Sign Up'}
                            </Button>
                            <Grid container justifyContent="center">
                                <Grid item>
                                    <Link component={RouterLink} to="/login" variant="body2">
                                        Already have an account? Sign In
                                    </Link>
                                </Grid>
                            </Grid>
                        </Box>
                    </CardContent>
                </Card>
            </Box>
        </Container>
    );
}

export default RegisterPage;
