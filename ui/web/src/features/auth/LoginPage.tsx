import { useState, useEffect, type FormEvent } from "react";
import { Navigate, useNavigate } from "react-router";
import { useAuthStore } from "@/stores/authStore";
import { apiClient } from "@/api/axios";

const DEMO_KEY = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

export function LoginPage() {
  const { isAuthenticated, login } = useAuthStore();
  const navigate = useNavigate();
  const [apiKey, setApiKey] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [showKey, setShowKey] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => setMounted(true), 50);
    return () => clearTimeout(t);
  }, []);

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const key = apiKey.trim();
    if (!key) {
      setError("Please enter an API key.");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await apiClient.get("/api/metrics/top", {
        headers: { "X-Api-Key": key },
      });
      login(key);
      navigate("/dashboard", { replace: true });
    } catch {
      setError("Invalid API key. Check your key and try again.");
    } finally {
      setLoading(false);
    }
  };

  const fillDemoKey = () => {
    setApiKey(DEMO_KEY);
    setError(null);
  };

  return (
    <div className="fixed inset-0 flex items-center justify-center overflow-hidden bg-gray-100 dark:bg-slate-950">
      {/* Atmospheric background */}
      <div className="absolute inset-0 overflow-hidden">
        {/* Gradient orbs */}
        <div className="absolute -top-1/4 -left-1/4 h-[800px] w-[800px] rounded-full bg-pulse-400/10 dark:bg-pulse-500/8 blur-[120px] animate-[drift_20s_ease-in-out_infinite]" />
        <div className="absolute -bottom-1/4 -right-1/4 h-[600px] w-[600px] rounded-full bg-indigo-400/8 dark:bg-indigo-500/6 blur-[100px] animate-[drift_25s_ease-in-out_infinite_reverse]" />
        {/* Dot grid */}
        <div
          className="absolute inset-0 opacity-[0.035] dark:opacity-[0.06]"
          style={{
            backgroundImage: "radial-gradient(circle, currentColor 1px, transparent 1px)",
            backgroundSize: "32px 32px",
          }}
        />
      </div>

      {/* Card */}
      <div
        className={`relative w-full max-w-md mx-4 transition-all duration-700 ease-out ${
          mounted ? "opacity-100 translate-y-0" : "opacity-0 translate-y-4"
        }`}
      >
        <div className="rounded-2xl border border-gray-200/60 dark:border-slate-800/60 bg-white/70 dark:bg-slate-900/70 backdrop-blur-xl shadow-2xl shadow-black/5 dark:shadow-black/30">
          <div className="px-8 pt-10 pb-8">
            {/* Logo */}
            <div className="flex flex-col items-center gap-3 mb-8">
              <div className="relative">
                <div className="absolute inset-0 rounded-2xl bg-pulse-400/30 blur-xl animate-pulse" />
                <div className="relative h-14 w-14 rounded-2xl bg-gradient-to-br from-pulse-400 to-pulse-600 flex items-center justify-center shadow-lg shadow-pulse-500/25">
                  <svg
                    className="h-8 w-8 text-white drop-shadow-sm"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    strokeWidth={2.5}
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M13 10V3L4 14h7v7l9-11h-7z"
                    />
                  </svg>
                </div>
              </div>
              <div className="text-center">
                <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
                  PulseBoard
                </h1>
                <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">
                  Enter your API key to continue
                </p>
              </div>
            </div>

            {/* Form */}
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label
                  htmlFor="apiKey"
                  className="block text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500 mb-2"
                >
                  API Key
                </label>
                <div className="relative">
                  <input
                    id="apiKey"
                    type={showKey ? "text" : "password"}
                    value={apiKey}
                    onChange={(e) => {
                      setApiKey(e.target.value);
                      setError(null);
                    }}
                    placeholder="a1b2c3d4-e5f6-..."
                    autoComplete="off"
                    className="w-full rounded-xl border border-gray-200 dark:border-slate-700 bg-gray-50 dark:bg-slate-800/50
                               px-4 py-3 pr-11 text-sm font-mono text-gray-900 dark:text-gray-100
                               placeholder:text-gray-300 dark:placeholder:text-slate-600
                               focus:outline-none focus:ring-2 focus:ring-pulse-500/40 focus:border-pulse-500
                               transition-all"
                  />
                  <button
                    type="button"
                    onClick={() => setShowKey(!showKey)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 dark:text-slate-500
                               hover:text-gray-600 dark:hover:text-slate-300 transition-colors cursor-pointer"
                    tabIndex={-1}
                    aria-label={showKey ? "Hide API key" : "Show API key"}
                  >
                    {showKey ? (
                      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L6.59 6.59m7.532 7.532l3.29 3.29M3 3l18 18" />
                      </svg>
                    ) : (
                      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path strokeLinecap="round" strokeLinejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                      </svg>
                    )}
                  </button>
                </div>
              </div>

              {/* Error message */}
              {error && (
                <div className="flex items-center gap-2 rounded-lg bg-red-50 dark:bg-red-950/30 border border-red-200/60 dark:border-red-800/40 px-3 py-2.5 text-sm text-red-600 dark:text-red-400 animate-[fadeShake_0.4s_ease-out]">
                  <svg className="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  {error}
                </div>
              )}

              {/* Submit */}
              <button
                type="submit"
                disabled={loading}
                className="w-full rounded-xl bg-gradient-to-r from-pulse-500 to-pulse-600 hover:from-pulse-600 hover:to-pulse-700
                           px-4 py-3 text-sm font-semibold text-white shadow-lg shadow-pulse-500/25
                           hover:shadow-pulse-500/40 active:scale-[0.98]
                           disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100
                           transition-all duration-200 cursor-pointer"
              >
                {loading ? (
                  <span className="flex items-center justify-center gap-2">
                    <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                    Validating...
                  </span>
                ) : (
                  "Connect"
                )}
              </button>
            </form>

            {/* Demo key helper */}
            <div className="mt-6 pt-5 border-t border-gray-100 dark:border-slate-800">
              <p className="text-xs text-center text-gray-400 dark:text-slate-500 mb-2">
                Trying the demo?
              </p>
              <button
                onClick={fillDemoKey}
                className="w-full rounded-lg border border-dashed border-gray-300 dark:border-slate-700
                           bg-gray-50/50 dark:bg-slate-800/30 px-3 py-2.5
                           text-xs font-mono text-gray-500 dark:text-slate-400
                           hover:border-pulse-400 hover:text-pulse-600 dark:hover:border-pulse-500 dark:hover:text-pulse-400
                           hover:bg-pulse-50/50 dark:hover:bg-pulse-950/20
                           transition-all cursor-pointer group"
              >
                <span className="flex items-center justify-center gap-2">
                  <svg className="h-3.5 w-3.5 text-gray-400 dark:text-slate-500 group-hover:text-pulse-500 transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3" />
                  </svg>
                  Use demo key
                </span>
              </button>
            </div>
          </div>
        </div>

        {/* Subtle footer */}
        <p className="mt-4 text-center text-xs text-gray-400 dark:text-slate-600">
          Event-driven analytics, in real time.
        </p>
      </div>

      <style>{`
        @keyframes drift {
          0%, 100% { transform: translate(0, 0) scale(1); }
          50% { transform: translate(30px, -20px) scale(1.05); }
        }
        @keyframes fadeShake {
          0% { opacity: 0; transform: translateX(-6px); }
          40% { transform: translateX(4px); }
          70% { transform: translateX(-2px); }
          100% { opacity: 1; transform: translateX(0); }
        }
      `}</style>
    </div>
  );
}
