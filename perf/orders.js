import http from 'k6/http';
import { check, sleep } from 'k6';

// Merchant order-list endpoint. Pages through sequential pages to exercise
// the "load all, paginate in memory" pattern — the deeper the page, the worse
// the existing implementation should look relative to a SQL-pushdown version.

const API_URL = __ENV.API_URL || 'http://localhost:5299';
const JWT = __ENV.JWT;
const VUS = parseInt(__ENV.VUS || '10', 10);
const DURATION = __ENV.DURATION || '30s';
const PAGE_SIZE = parseInt(__ENV.PAGE_SIZE || '20', 10);
const MAX_PAGE = parseInt(__ENV.MAX_PAGE || '10', 10);

if (!JWT) throw new Error('JWT env var is required (merchant token)');

export const options = {
  vus: VUS,
  duration: DURATION,
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const page = Math.floor(Math.random() * MAX_PAGE) + 1;
  const res = http.get(
    `${API_URL}/api/v1/orders?page=${page}&pageSize=${PAGE_SIZE}`,
    { headers: { Authorization: `Bearer ${JWT}` } }
  );
  check(res, { 'status is 200': r => r.status === 200 });
  sleep(0.1);
}
