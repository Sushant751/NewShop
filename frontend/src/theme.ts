import { createTheme } from '@mui/material/styles';

// ============================================================================
// Mantis Admin Theme Design Tokens & Component Overrides
// Reference: Mantis / Able Pro Admin Theme Design System
// ============================================================================

const theme = createTheme({
    palette: {
        mode: 'light',
        primary: {
            main: '#4680ff',        // Mantis Signature Electric Blue
            light: '#e8f0ff',       // Light blue background tint
            dark: '#2b63d9',
            contrastText: '#ffffff',
        },
        secondary: {
            main: '#8c8c8c',        // Neutral slate
            light: '#f5f5f5',
            dark: '#595959',
            contrastText: '#ffffff',
        },
        success: {
            main: '#52c41a',        // Vibrant Mantis Green
            light: '#f6ffed',
            dark: '#389e0d',
        },
        warning: {
            main: '#faad14',        // Bright Amber / Warning
            light: '#fffbe6',
            dark: '#d48806',
        },
        error: {
            main: '#ff4d4f',        // Soft Crimson Red
            light: '#fff2f0',
            dark: '#cf1322',
        },
        info: {
            main: '#13c2c2',        // Cyan / Info
            light: '#e6fffb',
            dark: '#08979c',
        },
        text: {
            primary: '#1d2630',     // Dark charcoal heading
            secondary: '#5b6b79',   // Slate body text
            disabled: '#bfbfbf',
        },
        background: {
            default: '#f4f6f8',     // Light grayish canvas
            paper: '#ffffff',       // Pure white cards
        },
        divider: '#e6ebf1',
    },
    typography: {
        fontFamily: '"Public Sans", "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
        h1: { fontSize: '2.25rem', fontWeight: 700, color: '#1d2630' },
        h2: { fontSize: '1.875rem', fontWeight: 700, color: '#1d2630' },
        h3: { fontSize: '1.5rem', fontWeight: 600, color: '#1d2630' },
        h4: { fontSize: '1.25rem', fontWeight: 600, color: '#1d2630' },
        h5: { fontSize: '1.125rem', fontWeight: 600, color: '#1d2630' },
        h6: { fontSize: '1rem', fontWeight: 600, color: '#1d2630' },
        subtitle1: { fontSize: '0.9375rem', color: '#5b6b79' },
        subtitle2: { fontSize: '0.8125rem', color: '#5b6b79', fontWeight: 500 },
        body1: { fontSize: '0.875rem', color: '#1d2630' },
        body2: { fontSize: '0.8125rem', color: '#5b6b79' },
        button: { textTransform: 'none', fontWeight: 600 },
    },
    shape: {
        borderRadius: 8,
    },
    components: {
        MuiCssBaseline: {
            styleOverrides: {
                body: {
                    backgroundColor: '#f4f6f8',
                    scrollbarWidth: 'thin',
                },
            },
        },
        MuiButton: {
            defaultProps: {
                disableElevation: true,
            },
            styleOverrides: {
                root: {
                    borderRadius: 8,
                    padding: '8px 18px',
                    fontWeight: 600,
                    textTransform: 'none',
                    transition: 'all 0.2s ease-in-out',
                },
                containedPrimary: {
                    boxShadow: '0 2px 6px rgba(70, 128, 255, 0.25)',
                    '&:hover': {
                        backgroundColor: '#2b63d9',
                        boxShadow: '0 4px 12px rgba(70, 128, 255, 0.35)',
                    },
                },
            },
        },
        MuiCard: {
            styleOverrides: {
                root: {
                    borderRadius: 10,
                    border: '1px solid #e6ebf1',
                    boxShadow: '0px 2px 8px rgba(32, 40, 45, 0.05)',
                    backgroundImage: 'none',
                },
            },
        },
        MuiPaper: {
            styleOverrides: {
                root: {
                    borderRadius: 10,
                    backgroundImage: 'none',
                },
                outlined: {
                    borderColor: '#e6ebf1',
                },
            },
        },
        MuiTextField: {
            defaultProps: {
                size: 'small',
                fullWidth: true,
            },
            styleOverrides: {
                root: {
                    '& .MuiOutlinedInput-root': {
                        borderRadius: 8,
                        '& fieldset': {
                            borderColor: '#d9d9d9',
                        },
                        '&:hover fieldset': {
                            borderColor: '#4680ff',
                        },
                        '&.Mui-focused fieldset': {
                            borderColor: '#4680ff',
                            borderWidth: 1.5,
                        },
                    },
                },
            },
        },
        MuiTableCell: {
            styleOverrides: {
                root: {
                    padding: '12px 16px',
                    borderBottom: '1px solid #e6ebf1',
                    fontSize: '0.8125rem',
                },
                head: {
                    fontWeight: 600,
                    backgroundColor: '#fafafa',
                    color: '#262626',
                },
            },
        },
        MuiChip: {
            styleOverrides: {
                root: {
                    borderRadius: 6,
                    fontWeight: 600,
                    fontSize: '0.75rem',
                },
            },
        },
        MuiDrawer: {
            styleOverrides: {
                paper: {
                    borderColor: '#e6ebf1',
                    backgroundColor: '#ffffff',
                },
            },
        },
        MuiAppBar: {
            styleOverrides: {
                root: {
                    backgroundColor: '#ffffff',
                    color: '#1d2630',
                    boxShadow: 'none',
                    borderBottom: '1px solid #e6ebf1',
                },
            },
        },
    },
});

export default theme;
