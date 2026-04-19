import http from 'k6/http';
import { check, sleep } from 'k6';

// Hammers the merchant dashboard endpoint. Supply API_URL + JWT via env vars.
// The merchant JWT needs to belong to a tenant seeded with non-trivial data
// for the numbers to be meaningful — use POST /api/v1/dev/seed first.

const API_URL = __ENV.API_URL || 'http://localhost:5299';
const JWT = __ENV.JWT;
const VUS = parseInt(__ENV.VUS || '10', 10);
const DURATION = __ENV.DURATION || '30s';

if (!JWT) throw new Error('JWT env var is required (merchant token)');

export const options = {
  vus: VUS,
  duration: DURATION,
  thresholds: {
    // Soft thresholds — fail the run if p95 blows past 1s or error rate > 1%.
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const res = http.get(`${API_URL}/api/v1/tenants/dashboard`, {
    headers: { Authorization: `Bearer ${JWT}` },
  });
  check(res, {
    'status is 200': r => r.status === 200,
    'body has customerCount': r => r.json('customerCount') !== undefined,
  });
  sleep(0.1);
}
