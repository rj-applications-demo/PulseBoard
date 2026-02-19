import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Routes, Route } from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useAuthStore } from "@/stores/authStore";
import { LoginPage } from "@/features/auth/LoginPage";
import { apiClient } from "@/api/axios";

jest.mock("@/api/axios", () => ({
  apiClient: {
    get: jest.fn(),
  },
}));

jest.mock("js-cookie", () => ({
  __esModule: true,
  default: {
    set: jest.fn(),
    get: jest.fn(),
    remove: jest.fn(),
  },
}));

const mockGet = apiClient.get as jest.Mock;

function getApiKeyInput() {
  return screen.getByPlaceholderText("a1b2c3d4-e5f6-...");
}

function renderLogin() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/login"]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/dashboard" element={<div>Dashboard</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  useAuthStore.setState({ apiKey: null, isAuthenticated: false });
  jest.clearAllMocks();
});

describe("LoginPage", () => {
  it("renders the login form", () => {
    renderLogin();
    expect(screen.getByText("PulseBoard")).toBeInTheDocument();
    expect(getApiKeyInput()).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /connect/i })).toBeInTheDocument();
  });

  it("shows error on invalid API key", async () => {
    const user = userEvent.setup();
    mockGet.mockRejectedValueOnce(new Error("Unauthorized"));

    renderLogin();
    await user.type(getApiKeyInput(), "bad-key");
    await user.click(screen.getByRole("button", { name: /connect/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid api key/i)).toBeInTheDocument();
    });
  });

  it("logs in and redirects on valid key", async () => {
    const user = userEvent.setup();
    mockGet.mockResolvedValueOnce({ data: { projects: [] } });

    renderLogin();
    await user.type(getApiKeyInput(), "valid-key");
    await user.click(screen.getByRole("button", { name: /connect/i }));

    await waitFor(() => {
      expect(screen.getByText("Dashboard")).toBeInTheDocument();
    });
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
  });

  it("fills demo key when clicking use demo key", async () => {
    const user = userEvent.setup();
    renderLogin();

    await user.click(screen.getByRole("button", { name: /use demo key/i }));
    expect(getApiKeyInput()).toHaveValue(
      "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    );
  });

  it("redirects to dashboard if already authenticated", () => {
    useAuthStore.setState({ apiKey: "key", isAuthenticated: true });
    renderLogin();
    expect(screen.getByText("Dashboard")).toBeInTheDocument();
  });
});
