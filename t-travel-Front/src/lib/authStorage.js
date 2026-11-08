const KEY = 'ttravel_access_token';

export function getToken() {
  try { return sessionStorage.getItem(KEY); } catch { return null; }
}

export function saveToken(token) {
  try { sessionStorage.setItem(KEY, token); } catch { /* Доступ к storage может быть ограничен. */ }
}

export function clearToken() {
  try { sessionStorage.removeItem(KEY); } catch { }
}

export function extractToken(payload) {
  return payload?.accessToken ?? payload?.token ?? payload?.jwt ?? payload?.data?.accessToken ?? payload?.data?.token ?? null;
}
