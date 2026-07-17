// Simple load test — 10 virtual users hammering the (anonymous, cached) schools
// endpoint for 20 seconds. No token or IDs needed.
//   Run:  k6 run k6/schools-smoke.js
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5119';

export const options = {
  vus: 10,
  duration: '20s',
  thresholds: {
    http_req_duration: ['p(95)<500'],   // 95% of requests under 500ms
    http_req_failed:   ['rate<0.01'],   // fewer than 1% errors
  },
};

export default function () {
  const res = http.get(`${BASE_URL}/api/v1/schools`);
  check(res, { 'status is 200': r => r.status === 200 });
}
