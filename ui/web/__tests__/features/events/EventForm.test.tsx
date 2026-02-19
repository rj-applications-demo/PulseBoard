import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { EventForm } from "@/features/events/EventForm";
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

function renderForm() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <EventForm />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  jest.clearAllMocks();
});

describe("EventForm", () => {
  it("renders with default values", () => {
    renderForm();
    const eventIdInput = screen.getByLabelText(/event id/i) as HTMLInputElement;
    expect(eventIdInput.value).toBeTruthy();
    expect(screen.getByLabelText(/project key/i)).toHaveValue("demo-project");
    expect(screen.getByRole("button", { name: /send event/i })).toBeInTheDocument();
  });

  it("submits event successfully", async () => {
    const user = userEvent.setup();
    mockPost.mockResolvedValueOnce({ data: { eventId: "test-id" } });

    renderForm();
    await user.click(screen.getByRole("button", { name: /send event/i }));

    await waitFor(() => {
      expect(screen.getByText(/event accepted/i)).toBeInTheDocument();
    });
    expect(mockPost).toHaveBeenCalledWith(
      "/events",
      expect.objectContaining({
        projectKey: "demo-project",
      }),
    );
  });

  it("shows validation error for invalid dimension value", async () => {
    const user = userEvent.setup();
    renderForm();

    await user.type(screen.getByLabelText(/event type/i), "INVALID!!!");
    await user.click(screen.getByRole("button", { name: /send event/i }));

    await waitFor(() => {
      expect(screen.getByText(/only contain lowercase/i)).toBeInTheDocument();
    });
    expect(mockPost).not.toHaveBeenCalled();
  });

  it("shows validation error for empty project key", async () => {
    const user = userEvent.setup();
    renderForm();

    await user.clear(screen.getByLabelText(/project key/i));
    await user.click(screen.getByRole("button", { name: /send event/i }));

    await waitFor(() => {
      expect(screen.getByText(/project key is required/i)).toBeInTheDocument();
    });
  });

  it("regenerates event ID", async () => {
    const user = userEvent.setup();
    renderForm();

    const input = screen.getByLabelText(/event id/i) as HTMLInputElement;
    const originalId = input.value;

    await user.click(screen.getByRole("button", { name: /regenerate/i }));

    expect(input.value).not.toBe(originalId);
  });
});
