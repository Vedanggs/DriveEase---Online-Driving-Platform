/**
 * Backend timestamps are stored as UTC but the API serializes them without a
 * trailing "Z" (or explicit offset). Treat any such string as UTC so it's
 * interpreted correctly instead of being misread as already-local time —
 * the single source of truth for this conversion across the app.
 */
export function toUtcDate(iso: string): Date {
  const withZone = iso.endsWith('Z') || iso.includes('+') ? iso : iso + 'Z';
  return new Date(withZone);
}
