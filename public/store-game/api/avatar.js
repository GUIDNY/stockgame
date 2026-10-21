/* /api/avatar: a selfie in, a rigged 3D figure of that person out, made by Tripo (tripo3d.ai).
   Behind the mall's own gate (middleware.js): only the accounts on its list reach it. The key is TRIPO_API_KEY in the
   project's environment and never reaches the page.

   Stateless. POST {image, side?, body?: data URLs} uploads the photos and starts the first task; each GET ?stage=&t= checks the
   current task and, once it is done, starts the next, so the browser's polling drives the whole chain:
     img   the selfie (and the face from the side and a full-body photo, if there are) drawn as a full-body figure in a T-pose (generate_image, 10 credits)
     model that drawing made 3D, with detailed textures (image_to_model, about 40)
     rig   a skeleton put in it (animate_rig, about 25)
     done  fig: the rigged GLB, made light (_fig.js, about 4 MB to under 1) and kept as this account's figure
   The figures are faces of real people: they live in a private Supabase bucket (mall-figures) and a table that knows
   them only by a hash of the account's email (mall_figures), both reached with the service role from here alone.
     GET ?me         this account's figure: {fig} or {fig:null}
     GET ?fig=<id>   a figure's file, for any player in the mall (the gate lets only them this far)
     DELETE ?me      forget this account's figure, file and all
   A scan is counted per account (mall_scans): at most LIMIT a day. */
const crypto = require('crypto');
const API = 'https://api.tripo3d.ai/v2/openapi';
const SB = process.env.SUPABASE_URL || 'https://fiyglezjgmltzruombno.supabase.co', BUCKET = 'mall-figures', LIMIT = 5;
const FIG = /^[a-z0-9]{20}$/;
// the drawing: as like the person as a stylised figure can be. With a full-body photo too, their build and clothes come from it
const LIKE = 'Keep the likeness exact: the same face shape, jaw, nose, eyes and eyebrows, the same hairstyle, hair length and hair colour, '
  + 'the same skin tone, the same beard or stubble or moustache, glasses only if they wear them. The person must be clearly recognisable. ';
const POSE = 'High-quality stylised 3D animated-film look (like a modern Pixar character), clean shapes, natural proportions, not a caricature. '
  + 'Standing in a T-pose: arms straight out to the sides, palms down, legs straight and slightly apart, feet flat, facing the camera. '
  + 'The whole body from head to feet in frame, centred. Plain white background, soft even light, no shadows, no props, no text. '
  + 'Clothes without loose strings, straps or hanging cords.';
// what the figure wears, picked on the page; 'mine' keeps the clothes in the photos
const WEAR = {
  bt: 'Dress them in a bright orange zip-up hoodie with a clean, smooth hood opening: no drawstrings, no cords, nothing hanging. A small white BuyToday logo on the chest, dark jeans and white sneakers. ',
  suit: 'Dress them in a well-fitted navy suit, a white shirt with an open collar, no tie, and brown leather shoes. ',
  sport: 'Dress them in sportswear: a fitted athletic t-shirt, track pants with a side stripe and running shoes. ',
  summer: 'Dress them for summer: a plain short-sleeved t-shirt, knee-length shorts and sneakers. ',
  casual: 'Casual modern clothes: a plain t-shirt or sweater, jeans and sneakers. ',
};
// the photos in order: the face, then the face from the side and the whole body when there are
function prompt(side, body, wear) {
  const dress = WEAR[wear] ? WEAR[wear] + 'Ignore the clothes they wear in the photos. ' : null;
  if (!side && !body) return 'Turn the person in image 1 into a full-body 3D game character for a friendly, colourful shopping-mall game. ' + LIKE
    + (dress || 'Keep the top they wear in image 1, with matching trousers and sneakers. ') + POSE;
  let s = 'Turn the person in these photos into a full-body 3D game character for a friendly, colourful shopping-mall game. '
    + 'Image 1 is a close-up of their face, from the front: take the face from it. ', n = 2;
  if (side) s += 'Image ' + (n++) + ' is the same face from the side: use it for the profile of the nose, the jaw and chin line, the ears '
    + 'and the volume and shape of the hair at the sides and back. ';
  if (body) s += 'Image ' + n + ' shows their whole body: take their build and height proportions' + (dress ? '' : ', clothes, colours and shoes') + ' from it. ';
  return s + (dress || (body ? '' : 'Keep the top they wear in image 1, with matching trousers and sneakers. ')) + LIKE + POSE;
}
const ID = /^[\w-]{8,80}$/;

const key = () => process.env.TRIPO_API_KEY || '';
const sbKey = () => process.env.SUPABASE_SERVICE_ROLE_KEY || '';
const sb = (path, init = {}) => fetch(SB + path, Object.assign({}, init, { headers: Object.assign({ apikey: sbKey(), Authorization: 'Bearer ' + sbKey() }, init.headers || {}) }));
const sbJson = async (path, init) => { const r = await sb(path, init); if (!r.ok) throw new Error('store ' + r.status + ' ' + (await r.text()).slice(0, 120)); return r.status === 204 ? null : r.json().catch(() => null); };

/* who is asking: the mall's pass (checked again here, as middleware.js checks it), the account as a hash */
const b64u = s => Buffer.from(s.replace(/-/g, '+').replace(/_/g, '/'), 'base64');
function passEmail(tok) {
  const secret = process.env.MALL_GATE_SECRET || ''; if (secret.length < 32 || !tok) return null;
  const [h, p, sig] = String(tok).split('.'); if (!h || !p || !sig) return null;
  try {
    if (JSON.parse(b64u(h).toString()).alg !== 'HS256') return null;
    const want = crypto.createHmac('sha256', secret).update(h + '.' + p).digest(), got = b64u(sig);
    if (got.length !== want.length || !crypto.timingSafeEqual(got, want)) return null;
    const c = JSON.parse(b64u(p).toString()), aud = Array.isArray(c.aud) ? c.aud : [c.aud];
    return aud.includes('bt-mall') && c.exp * 1000 > Date.now() && c.email ? String(c.email).toLowerCase() : null;
  } catch (e) { return null; }
}
function ownerOf(req) {
  const ck = /(?:^|;\s*)bt_play=([^;]+)/.exec(req.headers.cookie || ''), toks = [ck && decodeURIComponent(ck[1]), req.headers['x-bt-mall-pass']];
  for (const t of toks) { const e = passEmail(t); if (e) return crypto.createHash('sha256').update('bt-mall-figure:' + e).digest('hex'); }
  return null;
}
const myFig = async owner => { const r = await sbJson('/rest/v1/mall_figures?select=fig,task&owner=eq.' + owner); return r && r[0] || null; };
const dropFile = fig => sb('/storage/v1/object/' + BUCKET, { method: 'DELETE', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ prefixes: ['figs/' + fig + '.glb'] }) });
/* the finished rig: fetched from Tripo, made light, kept, and made this account's figure (the old one goes) */
async function keep(owner, task, url) {
  const cur = await myFig(owner); if (cur && cur.task === task) return cur.fig;   // asked again: already kept
  const r = await fetch(url); if (!r.ok) throw new Error('rig file ' + r.status);
  const out = await require('./_fig.js')(Buffer.from(await r.arrayBuffer())), fig = crypto.randomBytes(10).toString('hex');
  const up = await sb('/storage/v1/object/' + BUCKET + '/figs/' + fig + '.glb', { method: 'POST', headers: { 'Content-Type': 'model/gltf-binary', 'Cache-Control': '31536000' }, body: out });
  if (!up.ok) throw new Error('upload ' + up.status);
  await sbJson('/rest/v1/mall_figures?on_conflict=owner', { method: 'POST', headers: { 'Content-Type': 'application/json', Prefer: 'resolution=merge-duplicates,return=minimal' },
    body: JSON.stringify({ owner, fig, task, bytes: out.length, updated_at: new Date().toISOString() }) });
  if (cur) await dropFile(cur.fig).catch(() => {});
  return fig;
}
async function tripo(path, init) {
  const r = await fetch(API + path, Object.assign({}, init, { headers: Object.assign({ Authorization: 'Bearer ' + key() }, (init && init.headers) || {}) }));
  const j = await r.json().catch(() => ({}));
  if (j.code !== 0) { const e = new Error(j.message || ('tripo ' + r.status)); e.code = j.code; throw e; }
  return j.data;
}
const task = body => tripo('/task', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(d => d.task_id);
const send = (res, code, obj) => { res.statusCode = code; res.setHeader('Content-Type', 'application/json'); res.setHeader('Cache-Control', 'no-store'); res.end(JSON.stringify(obj)); };

async function readJson(req) {
  if (req.body && typeof req.body === 'object') return req.body;
  const chunks = []; let n = 0;
  for await (const c of req) { n += c.length; if (n > 4.4e6) throw new Error('too big'); chunks.push(c); }
  return JSON.parse(Buffer.concat(chunks).toString('utf8') || '{}');
}

module.exports = async (req, res) => {
  if (!key() || !sbKey()) return send(res, 503, { error: 'not configured' });
  try {
    const u = new URL(req.url, 'https://x'), owner = ownerOf(req);

    // a figure's file: any player in the mall sees the others; kept for good under its id, so cached for good
    const fig = u.searchParams.get('fig');
    if (req.method === 'GET' && fig) {
      if (!FIG.test(fig)) return send(res, 400, { error: 'fig' });
      const known = await sbJson('/rest/v1/mall_figures?select=fig&fig=eq.' + fig); if (!known || !known.length) return send(res, 404, { error: 'gone' });   // a forgotten figure is gone at once, cached copies or not
      const r = await sb('/storage/v1/object/authenticated/' + BUCKET + '/figs/' + fig + '.glb'); if (!r.ok) return send(res, r.status === 400 || r.status === 404 ? 404 : 502, { error: 'fig ' + r.status });
      res.statusCode = 200; res.setHeader('Content-Type', 'model/gltf-binary'); res.setHeader('Cache-Control', 'private, max-age=31536000, immutable');
      return res.end(Buffer.from(await r.arrayBuffer()));
    }
    if (!owner) return send(res, 401, { error: 'who' });
    if (u.searchParams.has('me')) {
      if (req.method === 'DELETE') { const cur = await myFig(owner); if (cur) { await dropFile(cur.fig); await sbJson('/rest/v1/mall_figures?owner=eq.' + owner, { method: 'DELETE' }); } return send(res, 200, { fig: null }); }
      const cur = await myFig(owner); return send(res, 200, { fig: cur ? cur.fig : null });
    }

    // a new selfie: upload it and start drawing the figure
    if (req.method === 'POST') {
      const since = new Date(Date.now() - 864e5).toISOString(), done = await sbJson('/rest/v1/mall_scans?select=id&owner=eq.' + owner + '&at=gte.' + since);
      if (done && done.length >= LIMIT) return send(res, 429, { error: 'limit' });
      const b = await readJson(req), pics = [b.image, b.side, b.body].filter(Boolean).slice(0, 3), files = [];
      for (const p of pics) {
        const m = /^data:image\/(jpeg|png|webp);base64,(.+)$/.exec(p || ''); if (!m) return send(res, 400, { error: 'image' });
        const ext = m[1] === 'jpeg' ? 'jpg' : m[1], fd = new FormData(); fd.append('file', new Blob([Buffer.from(m[2], 'base64')], { type: 'image/' + m[1] }), 'photo.' + ext);
        const up = await tripo('/upload', { method: 'POST', body: fd }); files.push({ type: ext, file_token: up.image_token });
      }
      if (!b.image || !b.side || !files.length) return send(res, 400, { error: 'image' });   // the face and the turned head, both
      await sbJson('/rest/v1/mall_scans', { method: 'POST', headers: { 'Content-Type': 'application/json', Prefer: 'return=minimal' }, body: JSON.stringify({ owner }) });
      // Gemini 3 Pro Image keeps a face best (10 credits)
      const t = await task(Object.assign({ type: 'generate_image', model_version: 'gemini_3_pro_image_preview', prompt: prompt(!!b.side, !!b.body, b.wear), t_pose: true },
        files.length > 1 ? { files } : { file: files[0] }));
      return send(res, 200, { stage: 'img', t });
    }

    // a poll: where the current task is, and the next one once it is done
    const stage = u.searchParams.get('stage'), t = u.searchParams.get('t') || '';
    if (!['img', 'model', 'rig'].includes(stage) || !ID.test(t)) return send(res, 400, { error: 'args' });
    const d = await tripo('/task/' + t, { method: 'GET' }), o = d.output || {};
    if (['failed', 'cancelled', 'banned', 'expired', 'unknown'].includes(d.status)) return send(res, 200, { stage: 'failed', error: d.status, at: stage });
    if (d.status !== 'success') return send(res, 200, { stage, t, progress: d.progress || 0 });
    if (stage === 'img') {
      const img = o.generated_image || o.image || o.rendered_image; if (!img) return send(res, 200, { stage: 'failed', error: 'no image', at: stage });
      const n = await task({ type: 'image_to_model', model_version: 'v3.1-20260211', file: { type: 'jpg', url: img }, texture: true, pbr: true, texture_quality: 'detailed', face_limit: 40000 });
      return send(res, 200, { stage: 'model', t: n, preview: img });
    }
    if (stage === 'model') {
      const n = await task({ type: 'animate_rig', original_model_task_id: t, model_version: 'v2.5-20260210', out_format: 'glb', rig_type: 'biped', spec: 'tripo' });
      return send(res, 200, { stage: 'rig', t: n, render: o.rendered_image || null });
    }
    const url = o.model || o.pbr_model || o.base_model; if (!url) return send(res, 200, { stage: 'failed', error: 'no model', at: stage });
    return send(res, 200, { stage: 'done', fig: await keep(owner, t, url) });
  } catch (e) {
    return send(res, e.code === 2010 ? 402 : 500, { error: e.code === 2010 ? 'credit' : String(e.message || e).slice(0, 200) });
  }
};
