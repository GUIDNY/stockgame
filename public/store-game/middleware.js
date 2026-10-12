/* The 3D mall is private until the patent application is filed: only two accounts get in.
   Every page request needs a pass. The shop (buytoday.co.il/api/game/pass) mints one only for a
   signed-in account on its list (src/lib/mall-access.ts in GUIDNY/pr): a JWT signed with
   MALL_GATE_SECRET, which this project and the shop share and nothing else knows. It arrives once
   at /__pass and is kept as this host's own cookie; the shop's /mall (the iOS app's address for the
   game) forwards it in a header instead. Anyone without one is sent to the shop, which sends them
   on to its home page unless they are on the list. No secret here means nobody gets in.

   Only pages are guarded. Models, textures, music and the libraries stay open because the game
   inside the app loads them cross-origin without cookies, and they say nothing about how it works. */
const ALLOWED = ['bd12123@gmail.com', 'asaf@buytoday.co.il'];
const COOKIE = 'bt_play';
const PASS_URL = 'https://buytoday.co.il/api/game/pass';

export const config = { matcher: '/:path*' };

const b64u = s => Uint8Array.from(atob(s.replace(/-/g, '+').replace(/_/g, '/') + '==='.slice((s.length + 3) % 4)), c => c.charCodeAt(0));

async function passOk(token) {
  const secret = process.env.MALL_GATE_SECRET;
  if (!secret || secret.length < 32 || !token) return false;
  try {
    const [h, p, sig] = token.split('.'); if (!h || !p || !sig) return false;
    if (JSON.parse(new TextDecoder().decode(b64u(h))).alg !== 'HS256') return false;
    const key = await crypto.subtle.importKey('raw', new TextEncoder().encode(secret), { name: 'HMAC', hash: 'SHA-256' }, false, ['verify']);
    if (!(await crypto.subtle.verify('HMAC', key, b64u(sig), new TextEncoder().encode(h + '.' + p)))) return false;
    const c = JSON.parse(new TextDecoder().decode(b64u(p)));
    const aud = Array.isArray(c.aud) ? c.aud : [c.aud];
    return aud.includes('bt-mall') && typeof c.exp === 'number' && c.exp * 1000 > Date.now() && ALLOWED.includes(String(c.email || '').toLowerCase());
  } catch (e) { return false; }
}

const cookieOf = (req, name) => { const m = (req.headers.get('cookie') || '').match(new RegExp('(?:^|;\\s*)' + name + '=([^;]+)')); return m ? decodeURIComponent(m[1]) : ''; };
const away = () => new Response(null, { status: 302, headers: { Location: PASS_URL, 'Cache-Control': 'no-store', 'X-Robots-Tag': 'noindex, nofollow' } });

export default async function middleware(req) {
  const url = new URL(req.url), path = url.pathname;

  if (path === '/__pass') {
    const t = url.searchParams.get('t') || '';
    if (!(await passOk(t))) return away();
    url.searchParams.delete('t');
    const q = url.searchParams.toString();
    return new Response(null, { status: 303, headers: {
      Location: '/' + (q ? '?' + q : ''), 'Cache-Control': 'no-store',
      'Set-Cookie': COOKIE + '=' + encodeURIComponent(t) + '; Path=/; Max-Age=2592000; HttpOnly; Secure; SameSite=Lax' } });
  }

  const last = path.split('/').pop();
  const page = path === '/' || /\.html?$/i.test(path) || !last.includes('.') || path === '/og.jpg';
  if (!page || path.startsWith('/.well-known/')) return;   // assets: through as before

  if (await passOk(cookieOf(req, COOKIE)) || await passOk(req.headers.get('x-bt-mall-pass') || '')) return;
  return away();
}
