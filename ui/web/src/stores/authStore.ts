import { create } from "zustand";
import Cookies from "js-cookie";

const COOKIE_NAME = "pb_api_key";
const COOKIE_EXPIRY_DAYS = 7;

interface AuthState {
  apiKey: string | null;
  isAuthenticated: boolean;
  login: (apiKey: string) => void;
  logout: () => void;
  hydrate: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  apiKey: null,
  isAuthenticated: false,

  login: (apiKey: string) => {
    Cookies.set(COOKIE_NAME, apiKey, {
      expires: COOKIE_EXPIRY_DAYS,
      sameSite: "Strict",
      path: "/",
    });
    set({ apiKey, isAuthenticated: true });
  },

  logout: () => {
    Cookies.remove(COOKIE_NAME, { path: "/" });
    set({ apiKey: null, isAuthenticated: false });
  },

  hydrate: () => {
    const apiKey = Cookies.get(COOKIE_NAME);
    if (apiKey) {
      set({ apiKey, isAuthenticated: true });
    }
  },
}));
