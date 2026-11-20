#!/usr/bin/env node
/**
 * Enzo Book — live HTTP security audit (black box, real requests).
 *
 * Prereq: the app is running on BASE_URL with the development database.
 *   node tests/e2e/http-audit.mjs [baseUrl]
 *
 * It registers disposable accounts, exercises guards, CSRF, IDOR, XSS,
 * cart validation, guest-cart merge and the login rate limiter.
 * Exit code is non-zero when any check fails.
 */

const BASE = (process.argv[2] || 'http://127.0.0.1:5102').replace(/\/$/, '');

const results = [];
function record(name, ok, evidence) {
  results.push({ name, ok, evidence });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}  ::  ${evidence}`);
}

class Client {
  constructor(label) {
    this.label = label;
    this.cookies = new Map();
  }

  cookieHeader() {
    return [...this.cookies.entries()].map(([k, v]) => `${k}=${v}`).join('; ');
  }

  store(response) {
    for (const raw of response.headers.getSetCookie?.() ?? []) {
      const [pair] = raw.split(';');
      const index = pair.indexOf('=');
      if (index > 0) this.cookies.set(pair.slice(0, index).trim(), pair.slice(index + 1).trim());
    }
  }

  async get(path) {
    const response = await fetch(BASE + path, {
      headers: { cookie: this.cookieHeader() },
      redirect: 'manual'
    });
    this.store(response);
    return { status: response.status, location: response.headers.get('location'), body: await response.text() };
  }

  async post(path, form, { withToken = true, tokenPage } = {}) {
    const fields = new URLSearchParams(form);
    if (withToken) fields.set('__RequestVerificationToken', await this.token(tokenPage || path.split('?')[0]));
    const response = await fetch(BASE + path, {
      method: 'POST',
      headers: {
        cookie: this.cookieHeader(),
        'content-type': 'application/x-www-form-urlencoded'
      },
      body: fields.toString(),
      redirect: 'manual'
    });
    this.store(response);

    // Signing in or out changes the identity the antiforgery token is bound to.
    if (/\/account\/(login|register|logout)/i.test(path)) this.lastToken = null;

    return { status: response.status, location: response.headers.get('location'), body: await response.text() };
  }

  async token(formPath) {
    // Antiforgery tokens are bound to the session cookie, so one per client is enough
    // (it also survives pages that render without a form, like an empty /checkout).
    if (this.lastToken) return this.lastToken;

    const page = await this.get(formPath);
    const match = page.body.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
    if (!match) throw new Error(`No antiforgery token on ${formPath} (status ${page.status})`);
    this.lastToken = match[1];
    return this.lastToken;
  }
}

const stamp = Date.now().toString(36);
const password = 'Enzo!Test2026';
const userAEmail = `qa.a.${stamp}@example.test`;
const userBEmail = `qa.b.${stamp}@example.test`;

async function register(client, email) {
  return client.post('/account/register', {
    FullName: 'QA Tester',
    Email: email,
    Phone: '01012345678',
    Password: password,
    ConfirmPassword: password,
    // Mass-assignment probes: must be ignored by the binder.
    Role: 'Admin',
    IsActive: 'true'
  });
}

async function main() {
  // ---------- 1. Headers ----------
  const plain = await fetch(BASE + '/');
  const headers = plain.headers;
  record('Security headers present', ['x-frame-options', 'x-content-type-options', 'content-security-policy', 'referrer-policy']
    .every(h => headers.get(h)), `CSP=${(headers.get('content-security-policy') || '').slice(0, 60)}…`);

  // ---------- 2. Anonymous guards ----------
  const anon = new Client('anon');
  const guardPaths = ['/admin', '/admin/orders', '/checkout', '/orders', '/profile', '/wishlist'];
  const guardStatuses = [];
  for (const path of guardPaths) {
    const response = await anon.get(path);
    guardStatuses.push(`${path}=${response.status}`);
  }
  record('Protected pages redirect anonymous users', guardStatuses.every(s => s.endsWith('=302')), guardStatuses.join(' '));

  // ---------- 3. CSRF ----------
  const csrf = new Client('csrf');
  const noToken = await csrf.post('/account/login', { Email: userAEmail, Password: password }, { withToken: false });
  record('POST without antiforgery token rejected', noToken.status === 400, `status=${noToken.status}`);

  // ---------- 4. Guest cart + registration + cart merge ----------
  const clientA = new Client('A');
  await clientA.get('/account/login');
  const addGuest = await clientA.post('/cart/add', { bookId: '1', quantity: '1' }, { tokenPage: '/books/1' });
  const guestCart = await clientA.get('/cart');
  record('Guest can add to cart', addGuest.status === 302 && /Last copy|Enzo|EGP/.test(guestCart.body),
    `add=${addGuest.status}`);

  const registerA = await register(clientA, userAEmail);
  const mergedCart = await clientA.get('/cart');
  record('Guest cart survives registration (merge)',
    registerA.status === 302 && !/Your bag is empty/i.test(mergedCart.body),
    `register=${registerA.status} location=${registerA.location}`);

  const loginA = await clientA.post('/account/login', { Email: userAEmail, Password: password });
  record('Login succeeds', loginA.status === 302 && !loginA.location?.includes('/account/login'),
    `status=${loginA.status} -> ${loginA.location}`);

  const asAdmin = await clientA.get('/admin');
  record('Mass-assigned Role did not grant admin', asAdmin.status !== 200, `GET /admin = ${asAdmin.status}`);

  // ---------- 5. XSS in review comment ----------
  const xssPayload = '<script>alert(1)</script><img src=x onerror=alert(2)>';
  const review = await clientA.post('/books/AddReview/1', {
    rating: '5',
    comment: xssPayload
  }, { tokenPage: '/books/1' });
  const bookPage = await clientA.get('/books/1');
  const escaped = bookPage.body.includes('&lt;script&gt;alert(1)&lt;/script&gt;');
  const rawScript = /<script>alert\(1\)<\/script>/.test(bookPage.body);
  record('Review comment is HTML-escaped', review.status === 302 && escaped && !rawScript,
    `post=${review.status} escaped=${escaped} rawScript=${rawScript}`);

  // ---------- 6. Negative quantity rejected ----------
  const negative = await clientA.post('/cart/update', { bookId: '1', quantity: '-5' }, { tokenPage: '/cart' });
  const cartAfter = await clientA.get('/cart');
  const quantityHeld = /class="qty-value"[^>]*>\s*1\s*</.test(cartAfter.body);
  record('Negative cart quantity rejected', negative.status === 302 && quantityHeld,
    `update=${negative.status} quantityStayedOne=${quantityHeld}`);

  // ---------- 7. Place order (server-priced) + IDOR ----------
  await clientA.get('/checkout');
  const idempotencyKey = `qa${stamp}`;
  const place = await clientA.post('/checkout/place', {
    FullName: 'QA Tester',
    Phone: '01012345678',
    City: 'Cairo',
    AddressLine: '12 Nile Street, Zamalek',
    PostalCode: '11211',
    PaymentMethod: 'Visa',
    IdempotencyKey: idempotencyKey,
    // Tampering probes: server must ignore these.
    Price: '0.01',
    Total: '0.01',
    Subtotal: '0.01'
  }, { tokenPage: '/checkout' });
  const orderMatch = (place.location || '').match(/\/orders\/success\/(\d+)/i);
  record('Checkout places an order', place.status === 302 && !!orderMatch,
    `status=${place.status} -> ${place.location}`);

  const doubleSubmit = await clientA.post('/checkout/place', {
    FullName: 'QA Tester', Phone: '01012345678', City: 'Cairo',
    AddressLine: '12 Nile Street, Zamalek', PaymentMethod: 'Visa', IdempotencyKey: idempotencyKey
  }, { tokenPage: '/checkout' });
  record('Double submit does not create a second order',
    doubleSubmit.status === 302 && doubleSubmit.location === place.location,
    `second -> ${doubleSubmit.location}`);

  if (orderMatch) {
    const orderId = orderMatch[1];
    const clientB = new Client('B');
    const registerB = await register(clientB, userBEmail);
    const loginB = await clientB.post('/account/login', { Email: userBEmail, Password: password });
    const foreignOrder = await clientB.get(`/orders/details/${orderId}`);
    record('IDOR: another customer cannot read the order',
      registerB.status === 302 && loginB.status === 302 && foreignOrder.status === 404,
      `registered=${registerB.status} login=${loginB.status} orderAsB=${foreignOrder.status}`);

    const ownerOrder = await clientA.get(`/orders/details/${orderId}`);
    record('Order detail page renders for its owner', ownerOrder.status === 200,
      `owner GET /orders/details/${orderId} = ${ownerOrder.status}`);
  }

  // ---------- 8. Login rate limiting ----------
  const limiter = new Client('limiter');
  const statuses = [];
  for (let i = 0; i < 14; i++) {
    const attempt = await limiter.post('/account/login', {
      Email: `nobody.${stamp}.${i}@example.test`,
      Password: 'Wrong!Pass2026'
    });
    statuses.push(attempt.status);
  }
  record('Login rate limiter returns 429', statuses.includes(429), `statuses=${statuses.join(',')}`);

  const failed = results.filter(r => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} checks passed`);
  if (failed.length) process.exitCode = 1;
}

main().catch(error => {
  console.error('AUDIT CRASHED:', error);
  process.exitCode = 2;
});
