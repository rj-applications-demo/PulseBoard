import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/api/axios";
import type { TimeSeriesResponse } from "@/types/api";
import type { Interval } from "@/types/metrics";

interface UseTimeSeriesOptions {
  projectKey: string;
  interval?: Interval;
  dimension?: string;
  enabled?: boolean;
}

export function useTimeSeries({
  projectKey,
  interval = "60s",
  dimension,
  enabled = true,
}: UseTimeSeriesOptions) {
  return useQuery<TimeSeriesResponse>({
    queryKey: ["timeseries", projectKey, interval, dimension],
    queryFn: async () => {
      const params: Record<string, string> = { projectKey, interval };
      if (dimension) params.dimension = dimension;
      const { data } = await apiClient.get<TimeSeriesResponse>(
        "/api/metrics/timeseries",
        { params },
      );
      return data;
    },
    enabled: enabled && !!projectKey,
    refetchInterval: 5000,
  });
}
