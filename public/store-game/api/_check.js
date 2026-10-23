/* Before a scanned figure is given to its player: is it one the mall can move? Run here, on the server, with the
   game's own code (scan-view.js, three.js r128 and its GLTFLoader, as the browser runs them), on the figure as the
   player will get it:
   - its skeleton is read (legs, spine, arms, head: scan-view's mapByShape) and nothing the moves need is missing
   - it takes the Meshy figures' skeleton and skin (reskin), as in the mall
   - each limb moves what it should and nothing else: an arm raised moves that hand a long way and leaves the head,
     the other hand and the feet where they were; the same for each leg
   A figure that fails is never kept: avatar.js rigs it again, or tells the player. */
const fs = require('fs'), path = require('path'), vm = require('vm');

let CTX = null;
function context() {
  if (CTX) return CTX;
  const THREE = require('three');
  const ctx = { THREE, console, TextDecoder, TextEncoder, URL, Blob, setTimeout, clearTimeout, navigator: { userAgent: 'node' }, self: {} };
  ctx.window = ctx; vm.createContext(ctx);
  // the browser's own files: the loader the game loads, and the code that reads and reskins a figure
  for (const f of ['gltf-loader.js', 'scan-view.js']) vm.runInContext(fs.readFileSync(path.join(__dirname, '..', f), 'utf8'), ctx, { filename: f });
  return (CTX = ctx);
}
const parse = (ctx, buf) => new Promise((res, rej) => new ctx.THREE.GLTFLoader().parse(buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength), '', res, rej));

// the figure without its pictures (the server has no images to decode them into; the shape is what is checked)
async function bare(buf) {
  const { NodeIO } = await import('@gltf-transform/core'), { ALL_EXTENSIONS } = await import('@gltf-transform/extensions');
  const io = new NodeIO().registerExtensions(ALL_EXTENSIONS), doc = await io.readBinary(new Uint8Array(buf));
  doc.getRoot().listTextures().forEach(t => t.dispose());
  return Buffer.from(await io.writeBinary(doc));
}

module.exports = async function checkFigure(buf) {
  const ctx = context(), THREE = ctx.THREE;
  const gl = await parse(ctx, await bare(buf)), m = gl.scene;
  const map = ctx.scanMap(m), core = map.missing.filter(n => !/ToeBase|Shoulder|Spine2|Neck/.test(n));
  if (core.length) return { ok: false, why: 'bones', missing: core };
  if (map.fwd) m.rotation.y = Math.atan2(-map.fwd.x, map.fwd.z);
  m.updateMatrixWorld(true);
  // the donor: people/MESHY-1.glb, kept beside this file without its textures (_donor.glb)
  const dn = await parse(ctx, fs.readFileSync(path.join(__dirname, '_donor.glb')));
  if (!ctx.scanReskin(m, dn.scene)) return { ok: false, why: 'reskin' };
  ctx.scanFit(m, 1.87);

  const bone = n => { let b = null; m.traverse(o => { if (!b && o.isBone && o.name === 'mixamorig' + n) b = o; }); return b; };
  const meshes = []; m.traverse(o => { if (o.isSkinnedMesh) meshes.push(o); });
  if (!meshes.length) return { ok: false, why: 'mesh' };
  // the skinned points, as they stand now
  const pts = () => { m.updateMatrixWorld(true); const out = [], v = new THREE.Vector3();
    meshes.forEach(o => { const pa = o.geometry.attributes.position; for (let k = 0; k < pa.count; k += 3) { v.fromBufferAttribute(pa, k); o.boneTransform(k, v); v.applyMatrix4(o.matrixWorld); out.push(v.clone()); } });
    return out; };
  const rest = pts(), box = new THREE.Box3().setFromPoints(rest), size = box.getSize(new THREE.Vector3());
  if (Math.abs(size.y - 1.87) > .05 || size.x > 2.6 || size.z > 1.2) return { ok: false, why: 'size', size: size.toArray().map(v => +v.toFixed(2)) };
  // the longest edge of the skin as it is posed now, against the same edge at rest (all of it, every triangle)
  const restAll = meshes.map(o => { const pa = o.geometry.attributes.position, a = new Float32Array(pa.count * 3), v = new THREE.Vector3();
    for (let k = 0; k < pa.count; k++) { v.fromBufferAttribute(pa, k); o.boneTransform(k, v); v.applyMatrix4(o.matrixWorld); v.toArray(a, k * 3); } return a; });
  function stretch() {
    m.updateMatrixWorld(true); let worst = { r: 1, len: 0 }; const v = new THREE.Vector3();
    meshes.forEach((o, mi) => { const g = o.geometry, pa = g.attributes.position, now = new Float32Array(pa.count * 3), R = restAll[mi];
      for (let k = 0; k < pa.count; k++) { v.fromBufferAttribute(pa, k); o.boneTransform(k, v); v.applyMatrix4(o.matrixWorld); v.toArray(now, k * 3); }
      const ix = g.index ? g.index.array : null, n = ix ? ix.length : pa.count, at = i => ix ? ix[i] : i;
      const d = (A, i, j) => Math.hypot(A[i * 3] - A[j * 3], A[i * 3 + 1] - A[j * 3 + 1], A[i * 3 + 2] - A[j * 3 + 2]);
      for (let t = 0; t < n; t += 3) for (let e = 0; e < 3; e++) { const i = at(t + e), j = at(t + (e + 1) % 3), r0 = Math.max(d(R, i, j), .005), r1 = d(now, i, j);
        if (r1 / r0 > worst.r && r1 > worst.len * .5) worst = { r: r1 / r0, len: r1, v: i, mesh: mi }; } });
    return { r: +worst.r.toFixed(2), len: +worst.len.toFixed(3), v: worst.v }; }
  // the points nearest a joint (within a few centimetres), to watch it by
  const near = (b, r) => { const p = b.getWorldPosition(new THREE.Vector3()); return rest.map((q, i) => q.distanceTo(p) < r ? i : -1).filter(i => i >= 0); };
  const watch = { LeftHand: 'LeftHand', RightHand: 'RightHand', Head: 'Head', LeftFoot: 'LeftFoot', RightFoot: 'RightFoot' }, idx = {};
  for (const [k, n] of Object.entries(watch)) { const b = bone(n); if (!b) return { ok: false, why: 'bones', missing: [n] }; idx[k] = near(b, .09); if (idx[k].length < 3) return { ok: false, why: 'skin', at: k }; }
  const moved = (now, k) => { const a = idx[k]; let s = 0; a.forEach(i => { s += now[i].distanceTo(rest[i]); }); return s / a.length; };
  // one limb turned 60 degrees at its root: what it carries moves, the rest stays
  const tests = [['LeftArm', 'LeftHand'], ['RightArm', 'RightHand'], ['LeftUpLeg', 'LeftFoot'], ['RightUpLeg', 'RightFoot']];
  for (const [root, end] of tests) {
    // across the limb's own line (an arm held straight out to the side turned about its length would hardly move)
    const b = bone(root), q0 = b.quaternion.clone(), dir = bone(end).getWorldPosition(new THREE.Vector3()).sub(b.getWorldPosition(new THREE.Vector3())).normalize();
    const side = new THREE.Vector3().crossVectors(dir, new THREE.Vector3(0, 0, 1)); if (side.lengthSq() < .01) side.set(1, 0, 0); side.normalize();
    const pq = b.parent.getWorldQuaternion(new THREE.Quaternion()), wq = b.getWorldQuaternion(new THREE.Quaternion());
    const turn = new THREE.Quaternion().setFromAxisAngle(side, -Math.PI / 3);
    b.quaternion.copy(pq.invert().multiply(turn.multiply(wq)));
    const now = pts(); b.quaternion.copy(q0);
    const far = moved(now, end);
    if (far < .25) return { ok: false, why: 'stuck', bone: root, moved: +far.toFixed(3) };
    for (const k of Object.keys(watch)) if (k !== end && moved(now, k) > .03) return { ok: false, why: 'drags', bone: root, at: k, moved: +moved(now, k).toFixed(3) };
  }
  // a walking pose, everything at once (arms down and swinging, legs apart, the body turning, the head too): no part
  // of the skin may stretch out of shape. A point tied to a far bone (a spike from a hand across the room) shows here
  const POSE = [['LeftArm', 'z', 1.1], ['RightArm', 'z', -1.1], ['LeftArm', 'x', -.5], ['RightArm', 'x', .5], ['LeftForeArm', 'x', -.6], ['RightForeArm', 'x', -.6],
    ['LeftUpLeg', 'x', -.4], ['RightUpLeg', 'x', .35], ['LeftLeg', 'x', .6], ['RightLeg', 'x', .2], ['Spine1', 'y', .25], ['Head', 'y', -.4], ['LeftHand', 'z', .4], ['RightHand', 'z', -.4]];
  // and in that pose the palm, the head and the toes move exactly with their own bone: the points round each, carried
  // by that bone alone, would land where they do (a palm partly tied to the hips drifts from its hand at every step)
  const firm = [['LeftHand', 'LeftForeArm', .07, .05], ['RightHand', 'RightForeArm', .07, .05], ['Head', null, 0, .1], ['LeftFoot', 'LeftLeg', .1, .07], ['RightFoot', 'RightLeg', .1, .07]]
    .map(([by, from, ahead, r]) => { const b = bone(by); if (!b) return null; b.updateMatrixWorld(true);
      // the palm a little past the wrist, the foot toward the toes, the head itself: away from the joint, whose
      // points are rightly shared with the bone before it
      const c = b.getWorldPosition(new THREE.Vector3()), f = from && bone(from);
      if (f) { const d = c.clone().sub(f.getWorldPosition(new THREE.Vector3())); if (by.endsWith('Foot')) d.set(0, -.3, 1); c.addScaledVector(d.normalize(), ahead); }
      return { by: b, name: by, idx: rest.map((q, i) => q.distanceTo(c) < r ? i : -1).filter(i => i >= 0), inv: b.matrixWorld.clone().invert() }; }).filter(Boolean);
  if (firm.some(f => f.idx.length < 3)) return { ok: false, why: 'skin', at: firm.find(f => f.idx.length < 3).name };
  const kept = [];
  for (const [n, ax, a] of POSE) { const b = bone(n); if (!b) continue; kept.push([b, b.quaternion.clone()]);
    const pq = b.parent.getWorldQuaternion(new THREE.Quaternion()), wq = b.getWorldQuaternion(new THREE.Quaternion());
    const turn = new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(ax === 'x' ? 1 : 0, ax === 'y' ? 1 : 0, ax === 'z' ? 1 : 0), a);
    b.quaternion.copy(pq.invert().multiply(turn.multiply(wq))); b.updateMatrixWorld(true); }
  const worst = stretch(), now = pts(); let drift = null;
  for (const f of firm) { const T = f.by.matrixWorld.clone().multiply(f.inv); let s = 0;
    f.idx.forEach(i => { s += rest[i].clone().applyMatrix4(T).distanceTo(now[i]); }); s /= f.idx.length; if (!drift || s > drift.d) drift = { at: f.name, d: +s.toFixed(3) }; }
  kept.reverse().forEach(([b, q]) => b.quaternion.copy(q)); m.updateMatrixWorld(true);
  if (worst.r > 3 && worst.len > .2) return { ok: false, why: 'stretch', ...worst };
  if (drift && drift.d > .025) return { ok: false, why: 'drift', ...drift };
  return { ok: true, stretch: worst.len, drift: drift && drift.d, size: size.toArray().map(v => +v.toFixed(2)) };
};
