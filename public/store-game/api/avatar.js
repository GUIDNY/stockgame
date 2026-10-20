/* /api/avatar: a selfie in, a rigged 3D figure of that person out, made by Tripo (tripo3d.ai).
   Behind the mall's own gate (middleware.js): only the accounts on its list reach it. The key is TRIPO_API_KEY in the
   project's environment and never reaches the page.

   Stateless. POST {image: data URL} uploads the photo and starts the first task; each GET ?stage=&t= checks the
   current task and, once it is done, starts the next, so the browser's polling drives the whole chain:
     img   the selfie (and a full-body photo, if there is one) drawn as a full-body figure in a T-pose (generate_image, 10 credits)
     model that drawing made 3D, with detailed textures (image_to_model, about 40)
     rig   a skeleton put in it (animate_rig, about 25)
     done  url: the rigged GLB
   GET ?file=<url> passes a finished file through, for when Tripo's storage will not answer the page directly. */
const API = 'https://api.tripo3d.ai/v2/openapi';
// the drawing: as like the person as a stylised figure can be. With a full-body photo too, their build and clothes come from it
const LIKE = 'Keep the likeness exact: the same face shape, jaw, nose, eyes and eyebrows, the same hairstyle, hair length and hair colour, '
  + 'the same skin tone, the same beard or stubble or moustache, glasses only if they wear them. The person must be clearly recognisable. ';
const POSE = 'High-quality stylised 3D animated-film look (like a modern Pixar character), clean shapes, natural proportions, not a caricature. '
  + 'Standing in a T-pose: arms straight out to the sides, palms down, legs straight and slightly apart, feet flat, facing the camera. '
  + 'The whole body from head to feet in frame, centred. Plain white background, soft even light, no shadows, no props, no text. '
  + 'Clothes without loose strings, straps or hanging cords.';
const PROMPT1 = 'Turn the person in image 1 into a full-body 3D game character for a friendly, colourful shopping-mall game. ' + LIKE
  + 'Casual modern clothes, sneakers. ' + POSE;
const PROMPT2 = 'Turn the person in these photos into a full-body 3D game character for a friendly, colourful shopping-mall game. '
  + 'Image 1 is a close-up of their face: take the face from it. Image 2 shows their whole body: take their build, height proportions, '
  + 'clothes, colours and shoes from it. ' + LIKE + POSE;
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
      const r = await fetch(f.href); if (!r.ok || !r.body) return send(res, 502, { error: 'fetch ' + r.status });
      res.statusCode = 200; res.setHeader('Content-Type', r.headers.get('content-type') || 'model/gltf-binary'); res.setHeader('Cache-Control', 'private, max-age=3600');
      // streamed, chunk by chunk: a buffered answer may not pass 4.5 MB, a figure with detailed textures can
      const rd = r.body.getReader();
      for (;;) { const { done, value } = await rd.read(); if (done) break; if (!res.write(Buffer.from(value))) await new Promise(z => res.once('drain', z)); }
      return res.end();
    }

    // a new selfie: upload it and start drawing the figure
    if (req.method === 'POST') {
      const b = await readJson(req), pics = [b.image, b.body].filter(Boolean).slice(0, 2), files = [];
      for (const p of pics) {
        const m = /^data:image\/(jpeg|png|webp);base64,(.+)$/.exec(p || ''); if (!m) return send(res, 400, { error: 'image' });
        const ext = m[1] === 'jpeg' ? 'jpg' : m[1], fd = new FormData(); fd.append('file', new Blob([Buffer.from(m[2], 'base64')], { type: 'image/' + m[1] }), 'photo.' + ext);
        const up = await tripo('/upload', { method: 'POST', body: fd }); files.push({ type: ext, file_token: up.image_token });
      }
      if (!files.length) return send(res, 400, { error: 'image' });
      // Gemini 3 Pro Image keeps a face best (10 credits)
      const t = await task(Object.assign({ type: 'generate_image', model_version: 'gemini_3_pro_image_preview', prompt: files.length > 1 ? PROMPT2 : PROMPT1, t_pose: true },
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
    return send(res, 200, { stage: 'done', url });
  } catch (e) {
    return send(res, e.code === 2010 ? 402 : 500, { error: e.code === 2010 ? 'credit' : String(e.message || e).slice(0, 200) });
  }
};
