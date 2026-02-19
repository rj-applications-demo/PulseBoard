export interface TimeSeriesPoint {
  timestamp: string;
  value: number;
}

export interface TimeSeriesResponse {
  projectKey: string;
  metric: string;
  interval: string;
  dimensionKey: string | null;
  source: string;
  dataPoints: TimeSeriesPoint[];
}

export interface ProjectMetric {
  projectKey: string;
  value: number;
}

export interface TopProjectsResponse {
  metric: string;
  interval: string;
  dimensionKey: string | null;
  source: string;
  projects: ProjectMetric[];
}

export interface DimensionDto {
  key: string;
  value: string;
}

export interface IncomingEventDto {
  eventId: string;
  projectKey: string;
  timestamp: string;
  payload?: Record<string, unknown>;
  dimensions?: DimensionDto[];
}

export interface EventAcceptedResponse {
  eventId: string;
}

export interface ApiErrorResponse {
  error: string;
}