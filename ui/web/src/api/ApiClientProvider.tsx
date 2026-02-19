import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createContext, useContext, useMemo, type ReactNode } from "react";
import { apiClient } from "./axios";
import type { AxiosInstance } from "axios";

const ApiClientContext = createContext<AxiosInstance>(apiClient);

export function useApiClient() {
  return useContext(ApiClientContext);
}

export function ApiClientProvider({ children }: { children: ReactNode }) {
  const queryClient = useMemo(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 10_000,
            retry: 1,
            refetchOnWindowFocus: false,
          },
        },
      }),
    [],
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ApiClientContext.Provider value={apiClient}>
        {children}
      </ApiClientContext.Provider>
    </QueryClientProvider>
  );
}
