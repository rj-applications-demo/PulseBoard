import { render, screen } from "@testing-library/react";
import { TimeSeriesChart } from "@/features/dashboard/TimeSeriesChart";
import type { TimeSeriesPoint } from "@/types/api";

jest.mock("@/stores/themeStore", () => ({
  useThemeStore: (selector: (s: { theme: string }) => unknown) =>
    selector({ theme: "light" }),
}));

// Mock recharts to avoid rendering SVG in jsdom
jest.mock("recharts", () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="chart-container">{children}</div>
  ),
  AreaChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="area-chart">{children}</div>
  ),
  Area: () => <div data-testid="area" />,
  XAxis: () => null,
  YAxis: () => null,
  CartesianGrid: () => null,
  Tooltip: () => null,
}));

describe("TimeSeriesChart", () => {
  it("renders chart even with no data points (zero-filled buckets)", () => {
    render(<TimeSeriesChart dataPoints={[]} interval="60s" />);
    expect(screen.getByTestId("chart-container")).toBeInTheDocument();
    expect(screen.getByTestId("area-chart")).toBeInTheDocument();
  });

  it("renders chart with data points", () => {
    const dataPoints: TimeSeriesPoint[] = [
      { timestamp: "2025-01-01T00:00:00Z", value: 10 },
      { timestamp: "2025-01-01T00:01:00Z", value: 20 },
      { timestamp: "2025-01-01T00:02:00Z", value: 15 },
    ];

    render(<TimeSeriesChart dataPoints={dataPoints} interval="60s" />);
    expect(screen.getByTestId("chart-container")).toBeInTheDocument();
    expect(screen.getByTestId("area-chart")).toBeInTheDocument();
  });
});
