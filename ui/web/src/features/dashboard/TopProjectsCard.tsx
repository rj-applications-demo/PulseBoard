import { useTopProjects } from "@/api/hooks/useTopProjects";
import type { Interval } from "@/types/metrics";

interface TopProjectsCardProps {
  interval: Interval;
}

export function TopProjectsCard({ interval }: TopProjectsCardProps) {
  const { data, isLoading } = useTopProjects({ interval });

  const projects = data?.projects ?? [];
  const maxValue = projects.length > 0 ? Math.max(...projects.map((p) => p.value)) : 1;

  return (
    <div className="rounded-xl border border-gray-200/60 dark:border-slate-800/60 bg-white dark:bg-slate-900 p-5">
      <h3 className="text-sm font-semibold text-gray-700 dark:text-slate-300 mb-4">
        Top Projects
      </h3>
      {isLoading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="animate-pulse">
              <div className="h-3 w-24 bg-gray-200 dark:bg-slate-700 rounded mb-2" />
              <div className="h-5 bg-gray-100 dark:bg-slate-800 rounded" />
            </div>
          ))}
        </div>
      ) : projects.length === 0 ? (
        <p className="text-sm text-gray-400 dark:text-slate-500 py-6 text-center">
          No project data yet
        </p>
      ) : (
        <div className="space-y-3">
          {projects.map((project, index) => {
            const pct = maxValue > 0 ? (project.value / maxValue) * 100 : 0;
            return (
              <div key={project.projectKey}>
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-medium text-gray-600 dark:text-slate-400 flex items-center gap-1.5">
                    <span className="text-[10px] font-bold text-gray-400 dark:text-slate-500 w-4">
                      {index + 1}
                    </span>
                    {project.projectKey}
                  </span>
                  <span className="text-xs font-mono font-semibold text-gray-700 dark:text-slate-300 tabular-nums">
                    {project.value.toLocaleString()}
                  </span>
                </div>
                <div className="h-2 rounded-full bg-gray-100 dark:bg-slate-800 overflow-hidden">
                  <div
                    className="h-full rounded-full bg-gradient-to-r from-pulse-400 to-pulse-500 transition-all duration-500"
                    style={{ width: `${pct}%` }}
                  />
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
