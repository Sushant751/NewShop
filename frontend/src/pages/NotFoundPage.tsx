import { Button, Box, Typography, Container } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import HomeIcon from '@mui/icons-material/Home';

function NotFoundPage() {
    const navigate = useNavigate();

    return (
        <Container maxWidth="sm">
            <Box
                sx={{
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    minHeight: '60vh',
                    textAlign: 'center',
                }}
            >
                <Typography variant="h1" component="div" sx={{ fontWeight: 700, color: 'primary.main' }}>
                    404
                </Typography>
                <Typography variant="h5" gutterBottom>
                    Page Not Found
                </Typography>
                <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
                    The page you are looking for doesn't exist or has been moved.
                </Typography>
                <Button
                    variant="contained"
                    size="large"
                    startIcon={<HomeIcon />}
                    onClick={() => navigate('/dashboard')}
                >
                    Go to Dashboard
                </Button>
            </Box>
        </Container>
    );
}

export default NotFoundPage;
