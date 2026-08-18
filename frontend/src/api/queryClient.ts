import { QueryClient } from 'react-query';

// ============================================================================
// React Query Client Configuration
// ============================================================================

export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: 1,
            refetchOnWindowFocus: true,
            refetchOnMount: true,
            staleTime: 0,
            cacheTime: 10 * 60 * 1000, // 10 minutes
        },
        mutations: {
            retry: 0,
        },
    },
});
