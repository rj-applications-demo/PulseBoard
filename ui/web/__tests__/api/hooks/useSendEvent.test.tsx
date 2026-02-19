import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { useSendEvent } from "@/api/hooks/useSendEvent";
import { apiClient } from "@/api/axios";

jest.mock("@/api/axios", () => ({
  apiClient: {
    post: jest.fn(),
    get: jest.fn(),
  },
}));

jest.mock("@/stores/authStore", () => ({
  useAuthStore: Object.assign(
    (selector: (s: { apiKey: string }) => unknown) =>
      selector({ apiKey: "test-key" }),
    { getState: () => ({ apiKey: "test-key" }) },
  ),
}));

const mockPost = apiClient.post as jest.Mock;

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };
}

beforeEach(() => {
  jest.clearAllMocks();
});

describe("useSendEvent", () => {
  it("sends event and returns eventId", async () => {
    mockPost.mockResolvedValueOnce({ data: { eventId: "evt-123" } });

    const { result } = renderHook(() => useSendEvent(), {
      wrapper: createWrapper(),
    });

    result.current.mutate({
      eventId: "evt-123",
      projectKey: "demo",
      timestamp: "2025-01-01T00:00:00Z",
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data).toEqual({ eventId: "evt-123" });
    expect(mockPost).toHaveBeenCalledWith("/events", {
      eventId: "evt-123",
      projectKey: "demo",
      timestamp: "2025-01-01T00:00:00Z",
    });
  });

  it("handles error", async () => {
    mockPost.mockRejectedValueOnce(new Error("Server error"));

    const { result } = renderHook(() => useSendEvent(), {
      wrapper: createWrapper(),
    });

    result.current.mutate({
      eventId: "evt-456",
      projectKey: "demo",
      timestamp: "2025-01-01T00:00:00Z",
    });

    await waitFor(() => expect(result.current.isError).toBe(true));
    expect(result.current.error?.message).toBe("Server error");
  });
});
