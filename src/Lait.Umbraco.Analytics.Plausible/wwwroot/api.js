// Tiny client for the Lait Tracking Management API.
// The endpoints require an authenticated backoffice user; we attach the bearer token
// obtained from the backoffice auth context.

const BASE = '/umbraco/management/api/v1/lait-tracking';

async function getJson(url, token) {
  const response = await fetch(url, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  if (!response.ok) {
    throw new Error(`Tracking API request failed (${response.status})`);
  }
  return response.json();
}

export function getStats(period, token) {
  return getJson(`${BASE}/stats?period=${encodeURIComponent(period)}`, token);
}

export function getPageStats(documentId, period, token) {
  return getJson(
    `${BASE}/page?id=${encodeURIComponent(documentId)}&period=${encodeURIComponent(period)}`,
    token,
  );
}
