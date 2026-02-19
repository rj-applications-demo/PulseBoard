export type Interval = "60s" | "60m" | "24h";

export interface ChartDataPoint {
  time: string;
  value: number;
  timestamp: number;
}
