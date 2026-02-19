import { EventForm } from "./EventForm";

export function EventSenderPage() {
  return (
    <div className="max-w-2xl">
      <div className="mb-6">
        <h1 className="text-xl font-bold text-gray-900 dark:text-white">
          Send Events
        </h1>
        <p className="text-sm text-gray-500 dark:text-slate-400 mt-0.5">
          Create and submit events to the PulseBoard API
        </p>
      </div>

      <div className="rounded-xl border border-gray-200/60 dark:border-slate-800/60 bg-white dark:bg-slate-900 p-6">
        <EventForm />
      </div>

      <div className="mt-4 rounded-lg bg-gray-50 dark:bg-slate-900/50 border border-gray-100 dark:border-slate-800/40 px-4 py-3">
        <p className="text-xs text-gray-400 dark:text-slate-500 leading-relaxed">
          Events are published to the ingestion pipeline and appear in the dashboard after processing.
          Default values are pre-filled so you can submit immediately for a quick demo.
        </p>
      </div>
    </div>
  );
}
