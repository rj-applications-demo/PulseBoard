import { useThemeStore } from "@/stores/themeStore";

const mockClassList = {
  toggle: jest.fn(),
};

Object.defineProperty(document.documentElement, "classList", {
  value: mockClassList,
  writable: true,
});

beforeEach(() => {
  useThemeStore.setState({ theme: "light" });
  localStorage.clear();
  jest.clearAllMocks();
});

describe("themeStore", () => {
  it("toggles from light to dark", () => {
    useThemeStore.getState().toggleTheme();

    expect(useThemeStore.getState().theme).toBe("dark");
    expect(localStorage.getItem("pb_theme")).toBe("dark");
    expect(mockClassList.toggle).toHaveBeenCalledWith("dark", true);
  });

  it("toggles from dark to light", () => {
    useThemeStore.setState({ theme: "dark" });

    useThemeStore.getState().toggleTheme();

    expect(useThemeStore.getState().theme).toBe("light");
    expect(localStorage.getItem("pb_theme")).toBe("light");
    expect(mockClassList.toggle).toHaveBeenCalledWith("dark", false);
  });

  it("initTheme applies the current theme to the document", () => {
    useThemeStore.setState({ theme: "dark" });

    useThemeStore.getState().initTheme();

    expect(mockClassList.toggle).toHaveBeenCalledWith("dark", true);
  });
});
