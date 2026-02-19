import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import type { TimeSeriesPoint } from "@/types/api";
import { useThemeStore } from "@/stores/themeStore";

interface TimeSeriesChartProps {
  dataPoints: TimeSeriesPoint[];
  interval: string;
}

interface ChartDatum {
  time: string;
  value: number;
  fullTime: string;
}

export function TimeSeriesChart({ dataPoints, interval }: TimeSeriesChartProps) {
  const theme = useThemeStore((s) => s.theme);
  const isDark = theme === "dark";

  const chartData: ChartDatum[] = fillBuckets(dataPoints, interval);

  if (chartData.length === 0) {
    return (
      <div className="flex items-center justify-center h-[300px] rounded-xl border border-dashed border-gray-300 dark:border-slate-700 bg-gray-50/50 dark:bg-slate-800/30">
        <div className="text-center">
          <svg className="h-10 w-10 text-gray-300 dark:text-slate-600 mx-auto mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" />
          </svg>
          <p className="text-sm text-gray-400 dark:text-slate-500">
            No data points yet
          </p>
          <p className="text-xs text-gray-300 dark:text-slate-600 mt-1">
            Send some events to see charts
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-[300px] w-full">
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={chartData} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
          <defs>
            <linearGradient id="pulseGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#06b6d4" stopOpacity={0.3} />
              <stop offset="95%" stopColor="#06b6d4" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid
            strokeDasharray="3 3"
            stroke={isDark ? "#1e293b" : "#f1f5f9"}
            vertical={false}
          />
          <XAxis
            dataKey="time"
            tick={false}
            axisLine={false}
            tickLine={false}
          />
          <YAxis
            tick={{ fontSize: 11, fill: isDark ? "#64748b" : "#94a3b8" }}
            axisLine={false}
            tickLine={false}
            allowDecimals={false}
          />
          <Tooltip
            contentStyle={{
              background: isDark ? "#0f172a" : "#ffffff",
              border: `1px solid ${isDark ? "#1e293b" : "#e2e8f0"}`,
              borderRadius: "0.75rem",
              fontSize: "0.8rem",
              boxShadow: "0 10px 25px -5px rgba(0,0,0,0.1)",
            }}
            labelStyle={{ color: isDark ? "#94a3b8" : "#64748b", marginBottom: 4 }}
            itemStyle={{ color: "#06b6d4" }}
            labelFormatter={(_label, payload) => {
              if (payload?.[0]) {
                const datum = payload[0].payload as ChartDatum;
                return datum.fullTime;
              }
              return _label;
            }}
          />
          <Area
            type="monotone"
            dataKey="value"
            stroke="#06b6d4"
            strokeWidth={2}
            fill="url(#pulseGradient)"
            animationDuration={600}
            dot={false}
            activeDot={{ r: 4, fill: "#06b6d4", stroke: isDark ? "#0f172a" : "#fff", strokeWidth: 2 }}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}

function formatTime(date: Date, interval: string): string {
  if (interval === "24h") {
    return date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  }
  return date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" });
}

/** Floor a timestamp to its bucket boundary. */
function floorToBucket(ms: number, interval: string): number {
  if (interval === "60s") {
    return Math.floor(ms / 1000) * 1000;
  }
  if (interval === "60m") {
    return Math.floor(ms / 60_000) * 60_000;
  }
  // 24h — per hour
  return Math.floor(ms / 3_600_000) * 3_600_000;
}

/**
 * Generate a complete time grid and merge API data into it.
 * - 60s  → 60 buckets, 1 second apart
 * - 60m  → 60 buckets, 1 minute apart
 * - 24h  → 24 buckets, 1 hour apart
 */
function fillBuckets(dataPoints: TimeSeriesPoint[], interval: string): ChartDatum[] {
  const config = {
    "60s": { count: 60, stepMs: 1_000 },
    "60m": { count: 60, stepMs: 60_000 },
    "24h": { count: 24, stepMs: 3_600_000 },
  } as const;

  const key = interval in config ? (interval as keyof typeof config) : "60s";
  const { count, stepMs } = config[key];

  // Build a lookup: bucket timestamp → summed value
  const valueMap = new Map<number, number>();
  for (const dp of dataPoints) {
    const key = floorToBucket(new Date(dp.timestamp).getTime(), interval);
    valueMap.set(key, (valueMap.get(key) ?? 0) + dp.value);
  }

  // Generate the full bucket grid ending at "now"
  const nowBucket = floorToBucket(Date.now(), interval);
  const startBucket = nowBucket - (count - 1) * stepMs;

  const result: ChartDatum[] = [];
  for (let ts = startBucket; ts <= nowBucket; ts += stepMs) {
    const date = new Date(ts);
    result.push({
      time: formatTime(date, interval),
      fullTime: date.toLocaleString(),
      value: valueMap.get(ts) ?? 0,
    });
  }

  return result;
}
