import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/api/axios";
import type { TopProjectsResponse } from "@/types/api";
import type { Interval } from "@/types/metrics";

interface UseTopProjectsOptions {
  interval?: Interval;
  limit?: number;
  dimension?: string;
}

export function useTopProjects({
  interval = "60m",
  limit = 10,
  dimension,
}: UseTopProjectsOptions = {}) {
  return useQuery<TopProjectsResponse>({
    queryKey: ["topProjects", interval, limit, dimension],
    queryFn: async () => {
      const params: Record<string, string | number> = { interval, limit };
      if (dimension) params.dimension = dimension;
      const { data } = await apiClient.get<TopProjectsResponse>(
        "/api/metrics/top",
        { params },
      );
      return data;
    },
    refetchInterval: 30000,
  });
}
