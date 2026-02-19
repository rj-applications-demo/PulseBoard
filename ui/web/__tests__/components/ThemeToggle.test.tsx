import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useThemeStore } from "@/stores/themeStore";
import { ThemeToggle } from "@/components/ThemeToggle";

beforeEach(() => {
  useThemeStore.setState({ theme: "light" });
});

describe("ThemeToggle", () => {
  it("renders with switch-to-dark label in light mode", () => {
    render(<ThemeToggle />);
    expect(screen.getByRole("button", { name: /switch to dark mode/i })).toBeInTheDocument();
  });

  it("renders with switch-to-light label in dark mode", () => {
    useThemeStore.setState({ theme: "dark" });
    render(<ThemeToggle />);
    expect(screen.getByRole("button", { name: /switch to light mode/i })).toBeInTheDocument();
  });

  it("toggles theme on click", async () => {
    const user = userEvent.setup();
    render(<ThemeToggle />);

    await user.click(screen.getByRole("button"));
    expect(useThemeStore.getState().theme).toBe("dark");
  });
});
