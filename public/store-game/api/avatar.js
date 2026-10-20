/* /api/avatar: a selfie in, a rigged 3D figure of that person out, made by Tripo (tripo3d.ai).
   Behind the mall's own gate (middleware.js): only the accounts on its list reach it. The key is TRIPO_API_KEY in the
   project's environment and never reaches the page.

   Stateless. POST {image: data URL} uploads the photo and starts the first task; each GET ?stage=&t= checks the
   current task and, once it is done, starts the next, so the browser's polling drives the whole chain:
     img   the selfie drawn as a full-body cartoon figure in a T-pose (generate_image, about 5 credits)
     model that drawing made 3D, textured (image_to_model, about 30)
     rig   a skeleton put in it (animate_rig, about 25)
     done  url: the rigged GLB
   GET ?file=<url> passes a finished file through, for when Tripo's storage will not answer the page directly. */
const API = 'https://api.tripo3d.ai/v2/openapi';
const PROMPT = 'Turn the person in this photo into a full-body 3D game character for a friendly, colourful shopping-mall game. '
  + 'Stylised, Pixar-like proportions, but keep what makes them recognisable: face shape, hairstyle and hair colour, skin tone, '
  + 'beard or moustache, glasses if they wear them. Casual modern clothes, sneakers. Standing in a T-pose: arms straight out to '
  + 'the sides, palms down, legs slightly apart, facing the camera. The whole body from head to feet in frame, centred. '
  + 'Plain white background, soft even light, no shadows, no props, no text.';
const ID = /^[\w-]{8,80}$/;

const key = () => process.env.TRIPO_API_KEY || '';
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
  if (!key()) return send(res, 503, { error: 'not configured' });
  try {
    const u = new URL(req.url, 'https://x');

    // a finished file, passed through (Tripo's own hosts only)
    const file = u.searchParams.get('file');
    if (req.method === 'GET' && file) {
      const f = new URL(file);
      if (f.protocol !== 'https:' || !/tripo/i.test(f.hostname)) return send(res, 400, { error: 'host' });
      const r = await fetch(f.href); if (!r.ok) return send(res, 502, { error: 'fetch ' + r.status });
      res.statusCode = 200; res.setHeader('Content-Type', r.headers.get('content-type') || 'application/octet-stream'); res.setHeader('Cache-Control', 'private, max-age=3600');
      return res.end(Buffer.from(await r.arrayBuffer()));
    }

    // a new selfie: upload it and start drawing the figure
    if (req.method === 'POST') {
      const b = await readJson(req), m = /^data:image\/(jpeg|png|webp);base64,(.+)$/.exec(b.image || '');
      if (!m) return send(res, 400, { error: 'image' });
      const fd = new FormData(); fd.append('file', new Blob([Buffer.from(m[2], 'base64')], { type: 'image/' + m[1] }), 'selfie.' + (m[1] === 'jpeg' ? 'jpg' : m[1]));
      const up = await tripo('/upload', { method: 'POST', body: fd });
      const t = await task({ type: 'generate_image', model_version: 'gemini_2.5_flash_image_preview', prompt: PROMPT, t_pose: true,
        file: { type: m[1] === 'jpeg' ? 'jpg' : m[1], file_token: up.image_token } });
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
      const n = await task({ type: 'image_to_model', model_version: 'v3.1-20260211', file: { type: 'jpg', url: img }, texture: true, pbr: true, face_limit: 40000 });
      return send(res, 200, { stage: 'model', t: n, preview: img });
    }
    if (stage === 'model') {
      const n = await task({ type: 'animate_rig', original_model_task_id: t, model_version: 'v2.5-20260210', out_format: 'glb', rig_type: 'biped', spec: 'tripo' });
      return send(res, 200, { stage: 'rig', t: n, render: o.rendered_image || null });
    }
    const url = o.model || o.pbr_model || o.base_model; if (!url) return send(res, 200, { stage: 'failed', error: 'no model', at: stage });
    return send(res, 200, { stage: 'done', url });
  } catch (e) {
    return send(res, e.code === 2010 ? 402 : 500, { error: e.code === 2010 ? 'credit' : String(e.message || e).slice(0, 200) });
  }
};
