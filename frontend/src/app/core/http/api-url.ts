declare global {
  interface Window {
    __CENTRAL_CONTABIL_CONFIG__?: { apiOrigin?: string };
  }
}

export function apiUrl(path: string): string {
  const origin = window.__CENTRAL_CONTABIL_CONFIG__?.apiOrigin?.replace(/\/+$/, '') ?? '';
  return `${origin}${path}`;
}
