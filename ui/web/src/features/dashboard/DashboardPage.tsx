import { useState } from "react";
import { useTimeSeries } from "@/api/hooks/useTimeSeries";
import { IntervalSelector } from "./IntervalSelector";
import { TimeSeriesChart } from "./TimeSeriesChart";
import { TopProjectsCard } from "./TopProjectsCard";
import { MetricsSummary } from "./MetricsSummary";
import type { Interval } from "@/types/metrics";

export function DashboardPage() {
  const [projectKey, setProjectKey] = useState("demo-project");
  const [interval, setInterval] = useState<Interval>("60s");
  const [projectInput, setProjectInput] = useState("demo-project");
  const [eventType, setEventType] = useState("");

  const dimension = eventType.trim() ? `type:${eventType.trim()}` : undefined;

  const { data: restData, isLoading } = useTimeSeries({
    projectKey,
    interval,
    dimension,
  });

  const displayData = restData?.dataPoints ?? [];

  const handleProjectSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = projectInput.trim();
    if (trimmed) setProjectKey(trimmed);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-gray-900 dark:text-white">
            Dashboard
          </h1>
          <p className="text-sm text-gray-500 dark:text-slate-400 mt-0.5">
            Real-time event metrics
          </p>
        </div>
        <IntervalSelector value={interval} onChange={setInterval} />
      </div>

      {/* Filter bar */}
      <div className="rounded-xl border border-gray-200/60 dark:border-slate-800/60 bg-white dark:bg-slate-900 px-5 py-4">
        <div className="flex flex-col sm:flex-row gap-4">
          {/* Project key */}
          <form onSubmit={handleProjectSubmit} className="flex-1 min-w-0">
            <label htmlFor="dash-project" className="block text-[11px] font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500 mb-1.5">
              Project Key
            </label>
            <div className="flex gap-2">
              <div className="relative flex-1">
                <svg className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400 dark:text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
                <input
                  id="dash-project"
                  type="text"
                  value={projectInput}
                  onChange={(e) => setProjectInput(e.target.value)}
                  placeholder="e.g. demo, my-app"
                  className="w-full rounded-lg border border-gray-200 dark:border-slate-700 bg-gray-50 dark:bg-slate-800
                             pl-9 pr-3 py-2 text-sm text-gray-900 dark:text-gray-100
                             placeholder:text-gray-400 dark:placeholder:text-slate-500
                             focus:outline-none focus:ring-2 focus:ring-pulse-500/30 focus:border-pulse-500
                             focus:bg-white dark:focus:bg-slate-800 transition-all"
                />
              </div>
              <button
                type="submit"
                className="rounded-lg bg-pulse-500 hover:bg-pulse-600 px-4 py-2 text-sm font-semibold
                           text-white shadow-sm shadow-pulse-500/20 hover:shadow-pulse-500/30
                           active:scale-[0.97] transition-all cursor-pointer"
              >
                Go
              </button>
            </div>
            {projectKey !== projectInput.trim() && projectInput.trim() && (
              <p className="mt-1 text-[11px] text-gray-400 dark:text-slate-500">
                Press Go or Enter to apply
              </p>
            )}
          </form>

          {/* Divider */}
          <div className="hidden sm:block w-px bg-gray-200 dark:bg-slate-700/60 self-stretch" />

          {/* Event type filter */}
          <div className="flex-1 min-w-0">
            <label htmlFor="dash-event-type" className="block text-[11px] font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500 mb-1.5">
              Filter by Event Type
            </label>
            <div className="relative">
              <svg className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400 dark:text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
              </svg>
              <input
                id="dash-event-type"
                type="text"
                value={eventType}
                onChange={(e) => setEventType(e.target.value.toLowerCase())}
                placeholder="e.g. error, info, user.signup"
                className={`w-full rounded-lg border bg-gray-50 dark:bg-slate-800
                           pl-9 pr-9 py-2 text-sm text-gray-900 dark:text-gray-100
                           placeholder:text-gray-400 dark:placeholder:text-slate-500
                           focus:outline-none focus:ring-2 focus:ring-pulse-500/30 focus:border-pulse-500
                           focus:bg-white dark:focus:bg-slate-800 transition-all
                           ${eventType.trim()
                             ? "border-pulse-400 dark:border-pulse-600 bg-pulse-50/50 dark:bg-pulse-950/20"
                             : "border-gray-200 dark:border-slate-700"}`}
              />
              {eventType.trim() && (
                <button
                  type="button"
                  onClick={() => setEventType("")}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 rounded-full p-0.5
                             text-gray-400 dark:text-slate-500 hover:text-gray-600 dark:hover:text-slate-300
                             hover:bg-gray-200 dark:hover:bg-slate-700 transition-colors cursor-pointer"
                  aria-label="Clear event type filter"
                >
                  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              )}
            </div>
            <p className="mt-1 text-[11px] text-gray-400 dark:text-slate-500">
              {eventType.trim()
                ? <span className="text-pulse-600 dark:text-pulse-400">Filtering on &ldquo;{eventType.trim()}&rdquo;</span>
                : "Leave empty to show all events"}
            </p>
          </div>
        </div>
      </div>

      {/* Summary cards */}
      <MetricsSummary dataPoints={displayData} interval={interval} />

      {/* Main chart area */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 rounded-xl border border-gray-200/60 dark:border-slate-800/60 bg-white dark:bg-slate-900 p-5">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-gray-700 dark:text-slate-300">
                Event Count — {projectKey}{dimension ? ` (${eventType.trim()})` : ""}
              </h3>
              <div className="group relative">
                <svg className="h-3.5 w-3.5 text-gray-400 dark:text-slate-500 cursor-help" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-56 rounded-lg bg-gray-800 dark:bg-slate-700 px-3 py-2
                                text-[11px] leading-relaxed text-gray-200 opacity-0 pointer-events-none
                                group-hover:opacity-100 group-hover:pointer-events-auto transition-opacity duration-200 z-10">
                  Event counts are eventually consistent. Low event volume may take a moment to reflect the current state.
                  <div className="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-gray-800 dark:border-t-slate-700" />
                </div>
              </div>
            </div>
            {restData?.source && (
              <span className="rounded-full bg-gray-100 dark:bg-slate-800 px-2 py-0.5 text-[10px] font-medium text-gray-500 dark:text-slate-400 uppercase tracking-wider">
                {restData.source}
              </span>
            )}
          </div>
          {isLoading ? (
            <div className="h-[300px] flex items-center justify-center">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-pulse-200 border-t-pulse-500" />
            </div>
          ) : (
            <TimeSeriesChart dataPoints={displayData} interval={interval} />
          )}
        </div>
        <div className="lg:col-span-1">
          <TopProjectsCard interval={interval} />
        </div>
      </div>
    </div>
  );
}
