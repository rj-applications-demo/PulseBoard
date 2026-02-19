import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@/api/axios";
import type { IncomingEventDto, EventAcceptedResponse } from "@/types/api";

export function useSendEvent() {
  return useMutation<EventAcceptedResponse, Error, IncomingEventDto>({
    mutationFn: async (event) => {
      const { data } = await apiClient.post<EventAcceptedResponse>(
        "/events",
        event,
      );
      return data;
    },
  });
}
