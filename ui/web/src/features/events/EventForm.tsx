import { useState, type FormEvent } from "react";
import { useSendEvent } from "@/api/hooks/useSendEvent";
import type { IncomingEventDto } from "@/types/api";

const DIMENSION_PATTERN = /^[a-z0-9._-]+$/;

function generateId(): string {
  return crypto.randomUUID();
}

export function EventForm() {
  const { mutate: sendEvent, isPending, isError, error } = useSendEvent();
  const [eventId, setEventId] = useState(generateId);
  const [projectKey, setProjectKey] = useState("demo-project");
  const [dimensionValue, setDimensionValue] = useState("");
  const [payload, setPayload] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [lastSentId, setLastSentId] = useState<string | null>(null);
  const [hasSent, setHasSent] = useState(false);
  const [sendSource, setSendSource] = useState<"form" | "another" | null>(null);

  const buildDto = (overrideEventId?: string): IncomingEventDto | null => {
    const id = overrideEventId ?? eventId.trim();
    if (!id) {
      setValidationError("Event ID is required.");
      return null;
    }
    if (!projectKey.trim()) {
      setValidationError("Project Key is required.");
      return null;
    }

    const dimVal = dimensionValue.trim();
    if (dimVal) {
      if (dimVal.length > 64) {
        setValidationError("Dimension value exceeds 64 characters.");
        return null;
      }
      if (!DIMENSION_PATTERN.test(dimVal)) {
        setValidationError("Dimension value may only contain lowercase letters, numbers, dots, underscores, and hyphens.");
        return null;
      }
    }

    let parsedPayload: Record<string, unknown> | undefined;
    if (payload.trim()) {
      try {
        parsedPayload = JSON.parse(payload.trim()) as Record<string, unknown>;
      } catch {
        setValidationError("Payload must be valid JSON.");
        return null;
      }
    }

    return {
      eventId: id,
      projectKey: projectKey.trim(),
      timestamp: new Date().toISOString(),
      payload: parsedPayload,
      dimensions: dimVal ? [{ key: "type", value: dimVal }] : undefined,
    };
  };

  const doSend = (dto: IncomingEventDto) => {
    sendEvent(dto, {
      onSuccess: (data) => {
        setLastSentId(data.eventId);
        setHasSent(true);
      },
    });
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    setValidationError(null);
    setSendSource("form");
    const dto = buildDto();
    if (dto) doSend(dto);
  };

  const handleSendAnother = () => {
    const newId = generateId();
    setEventId(newId);
    setValidationError(null);
    setSendSource("another");
    const dto = buildDto(newId);
    if (dto) doSend(dto);
  };

  const apiError = isError ? (error?.message || "Failed to send event.") : null;
  const displayError = validationError ?? apiError;

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      {/* Event ID */}
      <div>
        <label htmlFor="eventId" className="block text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500 mb-1.5">
          Event ID
        </label>
        <div className="flex gap-2">
          <input
            id="eventId"
            type="text"
            value={eventId}
            onChange={(e) => { setEventId(e.target.value); setValidationError(null); }}
            className="flex-1 rounded-lg border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800
                       px-3 py-2.5 text-sm font-mono text-gray-900 dark:text-gray-100
                       focus:outline-none focus:ring-2 focus:ring-pulse-500/30 focus:border-pulse-500 transition-all"
          />
          <button
            type="button"
            onClick={() => setEventId(generateId())}
            className="rounded-lg bg-gray-100 dark:bg-slate-800 px-3 py-2.5 text-xs font-medium
                       text-gray-500 dark:text-slate-400 hover:bg-gray-200 dark:hover:bg-slate-700
                       transition-colors whitespace-nowrap cursor-pointer"
          >
            Regenerate
          </button>
        </div>
      </div>

      {/* Project Key */}
      <div>
        <label htmlFor="projectKey" className="block text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500 mb-1.5">
          Project Key
        </label>
        <input
          id="projectKey"
          type="text"
          value={projectKey}
          onChange={(e) => { setProjectKey(e.target.value); setValidationError(null); }}
          className="w-full rounded-lg border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800
                     px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100
                     focus:outline-none focus:ring-2 focus:ring-pulse-500/30 focus:border-pulse-500 transition-all"
        />
      </div>

      {/* Event Type */}
      <div>
        <label htmlFor="eventType" className="block text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500 mb-1.5">
          Event Type <span className="text-gray-300 dark:text-slate-600 font-normal normal-case">(optional)</span>
        </label>
        <input
          id="eventType"
          type="text"
          value={dimensionValue}
          onChange={(e) => { setDimensionValue(e.target.value); setValidationError(null); }}
          placeholder="e.g. error, info, user.signup"
          className="w-full rounded-lg border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800
                     px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100
                     placeholder:text-gray-300 dark:placeholder:text-slate-600
                     focus:outline-none focus:ring-2 focus:ring-pulse-500/30 focus:border-pulse-500 transition-all"
        />
        <p className="mt-1 text-[11px] text-gray-400 dark:text-slate-500">
          Lowercase letters, numbers, dots, underscores, hyphens only
        </p>
      </div>

      {/* Payload */}
      <div>
        <label htmlFor="payload" className="block text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500 mb-1.5">
          Payload <span className="text-gray-300 dark:text-slate-600 font-normal normal-case">(optional JSON)</span>
        </label>
        <textarea
          id="payload"
          value={payload}
          onChange={(e) => { setPayload(e.target.value); setValidationError(null); }}
          placeholder='{"key": "value"}'
          rows={3}
          className="w-full rounded-lg border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800
                     px-3 py-2.5 text-sm font-mono text-gray-900 dark:text-gray-100
                     placeholder:text-gray-300 dark:placeholder:text-slate-600
                     focus:outline-none focus:ring-2 focus:ring-pulse-500/30 focus:border-pulse-500 transition-all
                     resize-y"
        />
      </div>

      {/* Error */}
      {displayError && (
        <div className="flex items-center gap-2 rounded-lg bg-red-50 dark:bg-red-950/30 border border-red-200/60 dark:border-red-800/40 px-3 py-2.5 text-sm text-red-600 dark:text-red-400">
          <svg className="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          {displayError}
        </div>
      )}

      {/* Actions */}
      <div className="flex gap-3 pt-1">
        <button
          type="submit"
          disabled={isPending}
          className="flex-1 rounded-xl bg-gradient-to-r from-pulse-500 to-pulse-600 hover:from-pulse-600 hover:to-pulse-700
                     px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-pulse-500/20
                     hover:shadow-pulse-500/35 active:scale-[0.98]
                     disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100
                     transition-all duration-200 cursor-pointer"
        >
          {isPending && sendSource === "form" ? "Sending..." : "Send Event"}
        </button>
        {hasSent && (
          <button
            type="button"
            disabled={isPending}
            onClick={handleSendAnother}
            className="flex items-center justify-center gap-2 rounded-xl border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-800
                       px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-slate-400
                       hover:bg-gray-50 dark:hover:bg-slate-700
                       disabled:opacity-60 disabled:cursor-not-allowed
                       transition-colors cursor-pointer"
          >
            {isPending && sendSource === "another" && (
              <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
            )}
            Send Another
          </button>
        )}
      </div>

      {/* Success */}
      {lastSentId && !isPending && !displayError && (
        <div className="flex items-center gap-2 rounded-lg bg-emerald-50 dark:bg-emerald-950/30 border border-emerald-200/60 dark:border-emerald-800/40 px-3 py-2.5 text-sm text-emerald-600 dark:text-emerald-400">
          <svg className="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
          </svg>
          Event accepted — <span className="font-mono text-xs">{lastSentId}</span>
        </div>
      )}
    </form>
  );
}
