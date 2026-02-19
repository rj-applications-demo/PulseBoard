import { useAuthStore } from "@/stores/authStore";

const mockSet = jest.fn();
const mockGet = jest.fn();
const mockRemove = jest.fn();

jest.mock("js-cookie", () => ({
  __esModule: true,
  default: {
    set: (...args: unknown[]) => mockSet(...args),
    get: (...args: unknown[]) => mockGet(...args),
    remove: (...args: unknown[]) => mockRemove(...args),
  },
}));

beforeEach(() => {
  useAuthStore.setState({ apiKey: null, isAuthenticated: false });
  jest.clearAllMocks();
});

describe("authStore", () => {
  it("starts unauthenticated", () => {
    const state = useAuthStore.getState();
    expect(state.apiKey).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });

  it("login sets API key, marks authenticated, and sets cookie", () => {
    useAuthStore.getState().login("test-key-123");

    const state = useAuthStore.getState();
    expect(state.apiKey).toBe("test-key-123");
    expect(state.isAuthenticated).toBe(true);
    expect(mockSet).toHaveBeenCalledWith("pb_api_key", "test-key-123", {
      expires: 7,
      sameSite: "Strict",
      path: "/",
    });
  });

  it("logout clears state and removes cookie", () => {
    useAuthStore.setState({ apiKey: "key", isAuthenticated: true });

    useAuthStore.getState().logout();

    const state = useAuthStore.getState();
    expect(state.apiKey).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(mockRemove).toHaveBeenCalledWith("pb_api_key", { path: "/" });
  });

  it("hydrate restores state from cookie", () => {
    mockGet.mockReturnValue("saved-key");

    useAuthStore.getState().hydrate();

    const state = useAuthStore.getState();
    expect(state.apiKey).toBe("saved-key");
    expect(state.isAuthenticated).toBe(true);
  });

  it("hydrate does nothing when no cookie", () => {
    mockGet.mockReturnValue(undefined);

    useAuthStore.getState().hydrate();

    const state = useAuthStore.getState();
    expect(state.apiKey).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });
});
