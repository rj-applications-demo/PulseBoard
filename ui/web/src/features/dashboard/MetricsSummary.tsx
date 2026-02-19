import type { TimeSeriesPoint } from "@/types/api";

interface MetricsSummaryProps {
  dataPoints: TimeSeriesPoint[];
  interval: string;
}

export function MetricsSummary({ dataPoints, interval }: MetricsSummaryProps) {
  const total = dataPoints.reduce((sum, dp) => sum + dp.value, 0);
  const peak = dataPoints.length > 0 ? Math.max(...dataPoints.map((dp) => dp.value)) : 0;
  const latest = dataPoints.length > 0 ? dataPoints[dataPoints.length - 1]!.value : 0;

  const cards = [
    {
      label: "Total Events",
      value: formatNumber(total),
      sublabel: `in ${interval} window`,
      color: "text-pulse-600 dark:text-pulse-400",
      bg: "bg-pulse-50 dark:bg-pulse-950/30",
    },
    {
      label: "Peak",
      value: formatNumber(peak),
      sublabel: "max in bucket",
      color: "text-amber-600 dark:text-amber-400",
      bg: "bg-amber-50 dark:bg-amber-950/30",
    },
    {
      label: "Latest",
      value: formatNumber(latest),
      sublabel: "most recent",
      color: "text-emerald-600 dark:text-emerald-400",
      bg: "bg-emerald-50 dark:bg-emerald-950/30",
    },
  ];

  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
      {cards.map((card) => (
        <div
          key={card.label}
          className="rounded-xl border border-gray-200/60 dark:border-slate-800/60 bg-white dark:bg-slate-900 p-4"
        >
          <p className="text-xs font-medium text-gray-500 dark:text-slate-400 mb-1">
            {card.label}
          </p>
          <p className={`text-2xl font-bold tabular-nums ${card.color}`}>
            {card.value}
          </p>
          <p className="text-[11px] text-gray-400 dark:text-slate-500 mt-0.5">
            {card.sublabel}
          </p>
        </div>
      ))}
    </div>
  );
}

function formatNumber(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return n.toLocaleString();
}
