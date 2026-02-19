import type { Interval } from "@/types/metrics";

const intervals: { value: Interval; label: string }[] = [
  { value: "60s", label: "60s" },
  { value: "60m", label: "60m" },
  { value: "24h", label: "24h" },
];

interface IntervalSelectorProps {
  value: Interval;
  onChange: (interval: Interval) => void;
}

export function IntervalSelector({ value, onChange }: IntervalSelectorProps) {
  return (
    <div className="inline-flex rounded-lg bg-gray-100 dark:bg-slate-800 p-1 gap-0.5">
      {intervals.map((item) => (
        <button
          key={item.value}
          onClick={() => onChange(item.value)}
          className={`rounded-md px-3.5 py-1.5 text-xs font-semibold transition-all cursor-pointer ${
            value === item.value
              ? "bg-white dark:bg-slate-700 text-pulse-600 dark:text-pulse-400 shadow-sm"
              : "text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-300"
          }`}
        >
          {item.label}
        </button>
      ))}
    </div>
  );
}
