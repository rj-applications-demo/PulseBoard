import { useEffect, type ReactNode } from "react";
import { useAuthStore } from "@/stores/authStore";
import { useThemeStore } from "@/stores/themeStore";

export function SessionProvider({ children }: { children: ReactNode }) {
  const hydrate = useAuthStore((s) => s.hydrate);
  const initTheme = useThemeStore((s) => s.initTheme);

  useEffect(() => {
    hydrate();
    initTheme();
  }, [hydrate, initTheme]);

  return <>{children}</>;
}
