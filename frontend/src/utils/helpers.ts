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

export function formatIndianDateTime(value: string | Date | null | undefined): string {
    if (!value) return '—';
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '—';

    return new Intl.DateTimeFormat('en-IN', {
        timeZone: 'Asia/Kolkata',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: true,
    }).format(date);
}

export function formatIndianDate(value: string | Date | null | undefined): string {
    if (!value) return '—';
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '—';

    return new Intl.DateTimeFormat('en-IN', {
        timeZone: 'Asia/Kolkata',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    }).format(date);
}

export function getIndiaDateString(date: Date): string {
    const parts = new Intl.DateTimeFormat('en-CA', {
        timeZone: 'Asia/Kolkata',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
    }).formatToParts(date);

    const year = parts.find((p) => p.type === 'year')?.value ?? '2024';
    const month = parts.find((p) => p.type === 'month')?.value ?? '01';
    const day = parts.find((p) => p.type === 'day')?.value ?? '01';

    return `${year}-${month}-${day}`;
}
