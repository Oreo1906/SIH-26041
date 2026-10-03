export type Health = { status: 'ok'; version: string };

export async function getHealth(signal?: AbortSignal): Promise<Health> {
  const response = await fetch('/health', { signal, headers: { Accept: 'application/json' } });
  if (!response.ok) throw new Error('Service unavailable');
  const data: unknown = await response.json();
  if (
    typeof data !== 'object' || data === null ||
    !('status' in data) || data.status !== 'ok' ||
    !('version' in data) || typeof data.version !== 'string' || !data.version
  ) throw new Error('Unexpected health response');
  return { status: data.status, version: data.version };
}
