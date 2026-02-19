import { BrowserRouter, Routes, Route, Navigate } from "react-router";
import { ApiClientProvider } from "@/api/ApiClientProvider";
import { SessionProvider } from "@/providers/SessionProvider";
import { ProtectedRoute } from "@/components/ProtectedRoute";
import { Layout } from "@/components/Layout";
import { LoginPage } from "@/features/auth/LoginPage";
import { DashboardPage } from "@/features/dashboard/DashboardPage";
import { EventSenderPage } from "@/features/events/EventSenderPage";

export function App() {
  return (
    <SessionProvider>
      <ApiClientProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<ProtectedRoute />}>
              <Route element={<Layout />}>
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/events" element={<EventSenderPage />} />
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </BrowserRouter>
      </ApiClientProvider>
    </SessionProvider>
  );
}
