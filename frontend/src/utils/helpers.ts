// ============================================================================
// Shared utility helpers
// ============================================================================

/**
 * Safely extract an error message from an unknown error value.
 * React Query's `error` field is typed as `unknown`, so we need runtime narrowing.
 */
export function getErrorMessage(err: unknown, fallback = 'An error occurred'): string {
    if (err instanceof Error) return err.message;
    if (typeof err === 'string') return err;
    return fallback;
}

/** Format a number as currency (USD). Null-safe — returns '—' for null/undefined. */
export function formatCurrency(val: number | null | undefined): string {
    return val != null ? `₹${val.toFixed(2)}` : '—';
}
