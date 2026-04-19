import http from 'k6/http';
import { check, sleep } from 'k6';

// Public storefront catalog — no auth, but the Host header must match a
// seeded tenant's subdomain. Use STORE_SLUG env var to pick a tenant.

const API_URL = __ENV.API_URL || 'http://localhost:5299';
const STORE_SLUG = __ENV.STORE_SLUG;
const VUS = parseInt(__ENV.VUS || '20', 10);
const DURATION = __ENV.DURATION || '30s';
const PAGE_SIZE = parseInt(__ENV.PAGE_SIZE || '20', 10);
const MAX_PAGE = parseInt(__ENV.MAX_PAGE || '50', 10);

if (!STORE_SLUG) throw new Error('STORE_SLUG env var is required (e.g., seed-a1b2c3d4)');

export const options = {
  vus: VUS,
  duration: DURATION,
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const page = Math.floor(Math.random() * MAX_PAGE) + 1;
  const res = http.get(
    `${API_URL}/api/v1/storefront/products?page=${page}&pageSize=${PAGE_SIZE}`,
    { headers: { Host: `${STORE_SLUG}.shop.demetrioq.com` } }
  );
  check(res, { 'status is 200': r => r.status === 200 });
  sleep(0.05);
}
