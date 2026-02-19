import axios from "axios";
import { useAuthStore } from "@/stores/authStore";

export const apiClient = axios.create({
  baseURL: "",
  headers: { "Content-Type": "application/json" },
});

apiClient.interceptors.request.use((config) => {
  const apiKey = useAuthStore.getState().apiKey;
  if (apiKey) {
    config.headers["X-Api-Key"] = apiKey;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      useAuthStore.getState().logout();
      window.location.href = "/login";
    }
    return Promise.reject(error);
  },
);
