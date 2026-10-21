/* scan-view.js: the figure made from a selfie, shown turning and dancing with the mall's own moves.
   Used by scan.html. Tripo's skeleton is a full human one under generic names ("0_Left_Limb_2", "bone_14"), so its
   bones are found by their shape in the rest pose: the legs are the chains that go down to the floor, the spine the
   one that goes up to where the arms and the head branch off, the arms the chains that reach out to the sides, left
   and right told by the way the toes point. They are renamed to the names the mall's clips use (Mixamo's), and the
   clips are carried over bone by bone, as the mall does for its own figures. */
(function(){
const MIX=['Hips','Spine','Spine1','Spine2','Neck','Head','LeftShoulder','LeftArm','LeftForeArm','LeftHand','RightShoulder','RightArm','RightForeArm','RightHand','LeftUpLeg','LeftLeg','LeftFoot','LeftToeBase','RightUpLeg','RightLeg','RightFoot','RightToeBase'];
const V=()=>new THREE.Vector3(), P=b=>b.getWorldPosition(V()), isB=o=>!!(o&&o.isBone), kids=b=>b.children.filter(isB);
// down a chain while each bone has a single bone child; ends on the bone that branches (a hand, the chest) or a tip
const walk=b=>{ const c=[b]; let q=b; while(kids(q).length===1){ q=kids(q)[0]; c.push(q); } return c; };

function mapByShape(model){
  model.updateMatrixWorld(true);
  const bones=[]; model.traverse(o=>{ if(o.isBone) bones.push(o); }); if(!bones.length) return {missing:MIX};
  const box=new THREE.Box3().setFromObject(model), H=box.max.y-box.min.y, floor=box.min.y;
  let hips=bones.find(b=>!isB(b.parent)); while(hips&&kids(hips).length===1) hips=kids(hips)[0];
  if(!hips) return {missing:MIX};
  const hp=P(hips), chains=kids(hips).map(walk);
  const legs=chains.filter(c=>P(c[c.length-1]).y<floor+H*.2&&c.length>=3);
  const ups=chains.filter(c=>kids(c[c.length-1]).length>=2&&P(c[c.length-1]).y>hp.y);
  if(legs.length<2||!ups.length) return {missing:MIX};
  const spine=ups.sort((a,b)=>P(b[b.length-1]).y-P(a[a.length-1]).y)[0], chest=spine[spine.length-1];
  // which way the figure faces. Across: from one leg to the other. Of the two ways square to that, the one the feet
  // reach out to, measured on the skinned vertices the feet carry (a rig need not have toe bones; this one may not)
  const across=P(legs[0][0]).sub(P(legs[1][0])); across.y=0; across.normalize();
  const fwd=V().crossVectors(across,new THREE.Vector3(0,1,0)).normalize();
  const feet=new Set(); legs.slice(0,2).forEach(c=>{ const f=c[Math.min(c.length-1,2)]; f.traverse(o=>{ if(o.isBone) feet.add(o); }); });
  let lean=0; const v=V(); model.traverse(o=>{ if(!o.isSkinnedMesh) return; const bs=o.skeleton.bones, si=o.geometry.attributes.skinIndex, sw=o.geometry.attributes.skinWeight, pa=o.geometry.attributes.position;
    for(let k=0;k<pa.count;k+=3){ let w=0; for(let j=0;j<4;j++){ const b=bs[si.getX?[si.getX(k),si.getY(k),si.getZ(k),si.getW(k)][j]:0]; if(b&&feet.has(b)) w+=[sw.getX(k),sw.getY(k),sw.getZ(k),sw.getW(k)][j]; }
      if(w<.6) continue; v.fromBufferAttribute(pa,k); o.boneTransform(k,v); v.applyMatrix4(o.matrixWorld);
      // relative to its own ankle: the nearer of the two
      const a0=P(legs[0][Math.min(legs[0].length-1,2)]), a1=P(legs[1][Math.min(legs[1].length-1,2)]), a=v.distanceTo(a0)<v.distanceTo(a1)?a0:a1; lean+=v.clone().sub(a).dot(fwd); } });
  if(lean<0) fwd.negate();
  const right=V().crossVectors(fwd,new THREE.Vector3(0,1,0)).normalize(), side=b=>P(b).sub(hp).dot(right)>0?'Right':'Left';
  // a rig whose root sits on the floor between the feet, legs and spine hung straight off it: the clips' hip sway would
  // swing the whole figure about its feet (knees flung up, arms out). A hip bone of its own goes in at the pelvis,
  // between the tops of the legs, and the legs and the spine hang off that instead (moved with attach(), so nothing
  // moves in the world and the skin stays as it was)
  let pelvis=hips;
  if(hp.y<floor+H*.25){ const top=P(legs[0][0]).add(P(legs[1][0])).multiplyScalar(.5);
    pelvis=new THREE.Bone(); hips.add(pelvis); pelvis.position.copy(hips.worldToLocal(top.clone())); pelvis.updateMatrixWorld(true);
    kids(hips).filter(b=>b!==pelvis).forEach(b=>pelvis.attach(b));   // the legs, the spine, and whatever else hangs there (a hem, a pocket)
    // skin the root itself held (the hoodie's hem) goes to the hip bone too, which joins the skeleton with the same rest
    model.updateMatrixWorld(true); const done=new Set();
    model.traverse(o=>{ if(!o.isSkinnedMesh) return; const sk=o.skeleton, ri=sk.bones.indexOf(hips); if(ri<0) return;
      if(!done.has(sk)){ done.add(sk); const rest=new THREE.Matrix4().multiplyMatrices(hips.matrixWorld,sk.boneInverses[ri]);
        sk.bones.push(pelvis); sk.boneInverses.push(new THREE.Matrix4().copy(pelvis.matrixWorld).invert().multiply(rest)); sk.boneMatrices=new Float32Array(sk.bones.length*16); }
      const pi=sk.bones.indexOf(pelvis), si=o.geometry.attributes.skinIndex, get=['getX','getY','getZ','getW'], set=['setX','setY','setZ','setW'];
      for(let k=0;k<si.count;k++) for(let j=0;j<4;j++) if(si[get[j]](k)===ri) si[set[j]](k,pi);
      si.needsUpdate=true; }); }
  const got={Hips:pelvis};
  legs.slice(0,2).forEach(c=>{ const s=side(c[0]); ['UpLeg','Leg','Foot','ToeBase'].forEach((n,i)=>{ if(c[i]) got[s+n]=c[i]; }); });
  const sp=spine.slice(0,spine.length).filter(b=>b!==hips&&b!==pelvis), names=['Spine','Spine1','Spine2'];
  if(sp.length<=3) sp.forEach((b,i)=>got[names[i]]=b); else [sp[0],sp[Math.floor(sp.length/2)],sp[sp.length-1]].forEach((b,i)=>got[names[i]]=b);
  const cp=P(chest);
  kids(chest).map(walk).forEach(c=>{ const end=P(c[c.length-1]), lat=end.clone().sub(cp).dot(right), up=end.y-cp.y;
    if(Math.abs(lat)>H*.12){ const s=lat>0?'Right':'Left', arm=c.slice(-4); const n=arm.length===4?['Shoulder','Arm','ForeArm','Hand']:['Arm','ForeArm','Hand'].slice(-arm.length); arm.forEach((b,i)=>got[s+n[i]]=b); }
    else if(up>0&&!got.Head){ if(c.length>=2){ got.Neck=c[0]; got.Head=c[1]; } else got.Head=c[0]; } });
  // the fingers, so the clips can curl them (and the cart grip wrap them): off each hand, the chain that starts nearest
  // the wrist is the thumb; the others in order of their distance from it, index to pinky
  ['Left','Right'].forEach(s=>{ const hand=got[s+'Hand']; if(!hand) return; const hp2=P(hand);
    const ch=kids(hand).map(walk).filter(c=>c.length>=2); if(ch.length<3) return;
    ch.sort((a,b)=>P(a[0]).distanceTo(hp2)-P(b[0]).distanceTo(hp2)); const thumb=ch.shift(), t0=P(thumb[0]);
    ch.sort((a,b)=>P(a[0]).distanceTo(t0)-P(b[0]).distanceTo(t0));
    [['Thumb',thumb]].concat(['Index','Middle','Ring','Pinky'].map((f,i)=>[f,ch[i]])).forEach(([f,c])=>{ if(c) c.slice(0,4).forEach((b,i)=>got[s+'Hand'+f+(i+1)]=b); }); });
  Object.entries(got).forEach(([n,b])=>{ b.name='mixamorig'+n; });
  return {missing:MIX.filter(n=>!got[n]),fwd};
}

// a height, standing on y = 0, centred: measured on the skinned vertices themselves (the geometry's own box ignores
// the skeleton's offsets)
function fitFigure(m,height){
  const skinBox=()=>{ m.updateMatrixWorld(true); const bb=new THREE.Box3(), v=V(); m.traverse(o=>{ if(!o.isSkinnedMesh) return; const pa=o.geometry.attributes.position;
      for(let k=0;k<pa.count;k+=7){ v.fromBufferAttribute(pa,k); o.boneTransform(k,v); v.applyMatrix4(o.matrixWorld); bb.expandByPoint(v); } }); return bb.isEmpty()?new THREE.Box3().setFromObject(m):bb; };
  const b=skinBox(); m.scale.multiplyScalar(height/Math.max(.01,b.max.y-b.min.y));
  const b2=skinBox(); m.position.y-=b2.min.y; m.position.x-=(b2.min.x+b2.max.x)/2; m.position.z-=(b2.min.z+b2.max.z)/2; m.updateMatrixWorld(true);
}
// for the mall (game.html): the same reading of the bones and the same fitting, for the player's own figure
/* The crowd's clips are made for a T-pose: arms straight out to the sides. A figure drawn with its arms angled down (an
   A-pose, as the image model often draws them) would take each turn from the wrong start: arms flung out, hands across
   the face. So its arms are lifted to straight out, and that pose made its rest: every vertex moved to where the bones
   now put it (normals too), and the bones' inverses taken anew. Run after mapByShape (it finds the arms by name). */
function tposeArms(model,fwd){
  model.updateMatrixWorld(true);
  const by=n=>{ let b=null; model.traverse(o=>{ if(!b&&o.isBone&&o.name==='mixamorig'+n) b=o; }); return b; };
  let moved=false; const q=new THREE.Quaternion(), pq=new THREE.Quaternion(), wq=new THREE.Quaternion();
  const aim=(bone,from,to,want)=>{ const d=P(to).sub(P(from)).normalize(); if(d.dot(want)>.995) return;
    q.setFromUnitVectors(d,want); bone.getWorldQuaternion(wq); bone.parent.getWorldQuaternion(pq);
    bone.quaternion.copy(pq.invert().multiply(q.multiply(wq))); bone.updateMatrixWorld(true); moved=true; };
  ['Left','Right'].forEach(s=>{ const arm=by(s+'Arm'), fore=by(s+'ForeArm'), hand=by(s+'Hand'); if(!arm||!fore||!hand) return;
    const d=P(fore).sub(P(arm)); d.y=0; if(d.lengthSq()<1e-8) return; d.normalize();   // out to the side, as it already leans
    aim(arm,arm,fore,d); aim(fore,fore,hand,d);
    const mid=by(s+'HandMiddle1'), ix=by(s+'HandIndex1'), pk=by(s+'HandPinky1'); if(!mid) return;
    aim(hand,hand,mid,d);
    if(ix&&pk&&fwd){ const a=P(ix).sub(P(pk)); a.addScaledVector(d,-a.dot(d)); const w=fwd.clone().setY(0); w.addScaledVector(d,-w.dot(d));
      if(a.lengthSq()>1e-10&&w.lengthSq()>1e-10){ a.normalize(); w.normalize(); if(a.dot(w)<.995){ q.setFromUnitVectors(a,w); hand.getWorldQuaternion(wq); hand.parent.getWorldQuaternion(pq);
        hand.quaternion.copy(pq.invert().multiply(q.multiply(wq))); hand.updateMatrixWorld(true); moved=true; } } } });
  if(!moved) return false;
  model.updateMatrixWorld(true);
  const meshes=[], M=new THREE.Matrix4(), B=new THREE.Matrix4(), T=new THREE.Matrix4(), N=new THREE.Matrix3(), v=V(), n=V();
  model.traverse(o=>{ if(!o.isSkinnedMesh) return; o.skeleton.update(); meshes.push(o);
    const g=o.geometry, pa=g.attributes.position, na=g.attributes.normal, si=g.attributes.skinIndex, sw=g.attributes.skinWeight, bm=o.skeleton.boneMatrices;
    for(let k=0;k<pa.count;k++){ M.set(0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0);
      // weights as stored may be whole numbers (a normalized byte or short reads 0..255 here): taken as shares of their sum
      const ws=[sw.getX(k),sw.getY(k),sw.getZ(k),sw.getW(k)], is=[si.getX(k),si.getY(k),si.getZ(k),si.getW(k)], sum=ws[0]+ws[1]+ws[2]+ws[3]; if(!sum) continue;
      for(let j=0;j<4;j++){ if(!ws[j]) continue; B.fromArray(bm,is[j]*16); for(let e=0;e<16;e++) M.elements[e]+=B.elements[e]*ws[j]/sum; }
      T.copy(o.bindMatrixInverse).multiply(M).multiply(o.bindMatrix);
      v.fromBufferAttribute(pa,k).applyMatrix4(T); pa.setXYZ(k,v.x,v.y,v.z);
      if(na){ N.setFromMatrix4(T); n.fromBufferAttribute(na,k).applyMatrix3(N).normalize(); na.setXYZ(k,n.x,n.y,n.z); } }
    pa.needsUpdate=true; if(na) na.needsUpdate=true; g.computeBoundingBox(); g.computeBoundingSphere(); });
  // bound again as it stands now: the bones' inverses and the mesh's bind matrix taken at the same moment (bind() with no
  // matrix does both), so the new rest is exactly this pose
  meshes.forEach(o=>o.bind(o.skeleton));
  return true;
}
window.scanMap=mapByShape; window.scanFit=fitFigure; window.scanTpose=tposeArms;

function retarget(clip,src,tgt){
  const W=root=>{ root.updateMatrixWorld(true); const m={}; root.traverse(o=>{ if(/^mixamorig/.test(o.name)) m[o.name]={b:o,w:o.getWorldQuaternion(new THREE.Quaternion())}; }); return m; };
  const S=W(src), T=W(tgt), R={}; Object.keys(T).forEach(n=>{ if(S[n]) R[n]=S[n].w.clone().invert().multiply(T[n].w); });
  const parentR=n=>{ const p=T[n].b.parent; if(p&&R[p.name]) return R[p.name]; return S[n].b.parent.getWorldQuaternion(new THREE.Quaternion()).invert().multiply(p.getWorldQuaternion(new THREE.Quaternion())); };
  const tracks=[], q=new THREE.Quaternion(), ip=new THREE.Quaternion(), ws=o=>o?o.getWorldScale(V()).y:1;
  // the hips' travel (a jump's lift), scaled from one figure's hip height to the other's, in each one's own units
  const sH=S.mixamorigHips, tH=T.mixamorigHips, sLeg=S.mixamorigLeftUpLeg||sH, tLeg=T.mixamorigLeftUpLeg||tH;
  const k=sH&&tH?P(tLeg.b).y/Math.max(1e-4,P(sLeg.b).y):1;   // both figures stand on y = 0
  clip.tracks.forEach(t=>{ const i=t.name.lastIndexOf('.'), bn=t.name.slice(0,i), prop=t.name.slice(i+1);
    if(prop==='position'&&bn==='mixamorigHips'&&sH&&tH){ const sr=sH.b.position, tr=tH.b.position, f=ws(sH.b.parent)*k/ws(tH.b.parent), v=t.values.slice();
      for(let j=0;j<v.length;j+=3){ v[j]=tr.x+(v[j]-sr.x)*f; v[j+1]=tr.y+(v[j+1]-sr.y)*f; v[j+2]=tr.z+(v[j+2]-sr.z)*f; }
      tracks.push(new THREE.VectorKeyframeTrack(tH.b.name+'.position',t.times.slice(),v)); return; }
    if(prop!=='quaternion'||!R[bn]) return; const v=t.values.slice(), Rb=R[bn]; ip.copy(parentR(bn)).invert();
    for(let j=0;j<v.length;j+=4){ q.fromArray(v,j); q.premultiply(ip).multiply(Rb); q.toArray(v,j); } tracks.push(new THREE.QuaternionKeyframeTrack(t.name,t.times.slice(),v)); });
  const t0=tracks.reduce((m,t)=>Math.min(m,t.times[0]),Infinity); if(t0>0&&t0<.5) tracks.forEach(t=>t.shift(-t0));
  return new THREE.AnimationClip(clip.name,-1,tracks);
}

function loader(){ const L=new THREE.GLTFLoader(); if(window.MeshoptDecoder) L.setMeshoptDecoder(MeshoptDecoder);
  if(THREE.DRACOLoader){ const d=new THREE.DRACOLoader(); d.setDecoderPath('https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/libs/draco/'); L.setDRACOLoader(d); } return L; }
const load=u=>new Promise((res,rej)=>loader().load(u,res,undefined,rej));
const LABEL={Idle:'עמידה',Walk:'הליכה',Wave:'שלום',Dance:'ריקוד',Dance2:'גנגנם',Jump:'קפיצה',Cheer:'הידד'};

/* box: the element to draw in; urls: where to try the model, in order; movesBox: where the move buttons go */
window.avatarViewer=async function(box,urls,movesBox,note,base,prep){   // prep: called with the model before its bones are read (tests)
  base=base||'';
  const W=box.clientWidth, Hh=box.clientHeight, r=new THREE.WebGLRenderer({antialias:true,alpha:true});
  r.setPixelRatio(Math.min(2,devicePixelRatio)); r.setSize(W,Hh); r.outputEncoding=THREE.sRGBEncoding; box.appendChild(r.domElement);
  const sc=new THREE.Scene(), cam=new THREE.PerspectiveCamera(32,W/Hh,.05,50); cam.position.set(0,1.05,4.8);
  sc.add(new THREE.HemisphereLight(0xffffff,0x6070a0,1.15)); const d=new THREE.DirectionalLight(0xffffff,1.1); d.position.set(1.5,3,3); sc.add(d);
  const shadow=new THREE.Mesh(new THREE.CircleGeometry(.75,48),new THREE.MeshBasicMaterial({color:0,transparent:true,opacity:.25})); shadow.rotation.x=-Math.PI/2; sc.add(shadow);
  let gl=null, err=null; for(const u of urls){ try{ gl=await load(u); break; }catch(e){ err=e; } }
  if(!gl){ note('הקובץ לא נטען ('+((err&&err.message)||err)+')'); return null; }
  const m=gl.scene, turn=new THREE.Group(); turn.add(m); sc.add(turn);
  m.traverse(o=>{ if(o.isMesh){ o.frustumCulled=false; const mt=o.material; if(mt&&mt.map) mt.map.anisotropy=4; } });
  if(prep) prep(m); const map=mapByShape(m); tposeArms(m,map.fwd);
  if(map.fwd) m.rotation.y=Math.atan2(-map.fwd.x,map.fwd.z);   // face the camera
  fitFigure(m,1.8);
  const mixer=new THREE.AnimationMixer(m), acts={};
  let yaw=0, drag=null; box.addEventListener('pointerdown',e=>{ drag=e.clientX; try{ box.setPointerCapture(e.pointerId); }catch(_){} });
  box.addEventListener('pointermove',e=>{ if(drag!==null){ yaw+=(e.clientX-drag)*.012; drag=e.clientX; } }); box.addEventListener('pointerup',()=>drag=null);
  let last=performance.now(); (function tick(now){ const dt=Math.min(.05,(now-last)/1000); last=now; mixer.update(dt); if(drag===null) yaw+=dt*.25; turn.rotation.y=yaw; cam.lookAt(0,.95,0); r.render(sc,cam); requestAnimationFrame(tick); })(last);
  if(map.missing.filter(n=>!/ToeBase|Shoulder|Spine2|Neck/.test(n)).length){ note('השלד של הדמות לא זוהה עד הסוף, אז היא עומדת בלי תנועה. (חסרים: '+map.missing.slice(0,6).join(', ')+')'); return {model:m,map}; }
  const [an,mv]=await Promise.all([load(base+'people/anims.glb'),load(base+'people/moves.glb').catch(()=>null)]);
  [['Idle',an],['Walk',an],['Wave',an]].concat(mv?[['Dance',mv],['Dance2',mv],['Jump',mv],['Cheer',mv]]:[]).forEach(([n,g])=>{ const c=g.animations.find(a=>a.name===n); if(c) acts[n]=mixer.clipAction(retarget(c,g.scene,m)); });
  const play=n=>{ const a=acts[n]; if(!a) return; Object.values(acts).forEach(x=>x!==a&&x.fadeOut(.25)); a.reset().fadeIn(.25).play(); if(movesBox) movesBox.querySelectorAll('button').forEach(x=>x.classList.toggle('on',x.dataset.n===n)); };
  if(movesBox) Object.keys(acts).forEach(n=>{ const bt=document.createElement('button'); bt.textContent=LABEL[n]||n; bt.dataset.n=n; bt.addEventListener('click',()=>play(n)); movesBox.appendChild(bt); });
  play(acts.Dance?'Dance':'Idle');
  return {model:m,map,play,acts};
};
})();
