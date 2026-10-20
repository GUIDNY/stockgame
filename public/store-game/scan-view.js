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
  // which way the figure faces: from the ankle to the toes
  const fwd=V(); legs.slice(0,2).forEach(c=>{ const f=c[Math.min(c.length-1,3)], a=c[Math.min(c.length-2,2)]; fwd.add(P(f).sub(P(a))); }); fwd.y=0;
  if(fwd.lengthSq()<1e-8) fwd.set(0,0,1); fwd.normalize();
  const right=V().crossVectors(fwd,new THREE.Vector3(0,1,0)).normalize(), side=b=>P(b).sub(hp).dot(right)>0?'Right':'Left';
  const got={Hips:hips};
  legs.slice(0,2).forEach(c=>{ const s=side(c[0]); ['UpLeg','Leg','Foot','ToeBase'].forEach((n,i)=>{ if(c[i]) got[s+n]=c[i]; }); });
  const sp=spine.slice(0,spine.length).filter(b=>b!==hips), names=['Spine','Spine1','Spine2'];
  if(sp.length<=3) sp.forEach((b,i)=>got[names[i]]=b); else [sp[0],sp[Math.floor(sp.length/2)],sp[sp.length-1]].forEach((b,i)=>got[names[i]]=b);
  const cp=P(chest);
  kids(chest).map(walk).forEach(c=>{ const end=P(c[c.length-1]), lat=end.clone().sub(cp).dot(right), up=end.y-cp.y;
    if(Math.abs(lat)>H*.12){ const s=lat>0?'Right':'Left', arm=c.slice(-4); const n=arm.length===4?['Shoulder','Arm','ForeArm','Hand']:['Arm','ForeArm','Hand'].slice(-arm.length); arm.forEach((b,i)=>got[s+n[i]]=b); }
    else if(up>0&&!got.Head){ if(c.length>=2){ got.Neck=c[0]; got.Head=c[1]; } else got.Head=c[0]; } });
  Object.entries(got).forEach(([n,b])=>{ b.name='mixamorig'+n; });
  return {missing:MIX.filter(n=>!got[n]),fwd};
}

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
window.avatarViewer=async function(box,urls,movesBox,note,base){
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
  const map=mapByShape(m);
  if(map.fwd) m.rotation.y=Math.atan2(-map.fwd.x,map.fwd.z);   // face the camera
  // measured on the skinned vertices themselves: the geometry's own box ignores the skeleton's offsets
  const skinBox=()=>{ m.updateMatrixWorld(true); const bb=new THREE.Box3(), v=V(); m.traverse(o=>{ if(!o.isSkinnedMesh) return; const pa=o.geometry.attributes.position;
      for(let k=0;k<pa.count;k+=7){ v.fromBufferAttribute(pa,k); o.boneTransform(k,v); v.applyMatrix4(o.matrixWorld); bb.expandByPoint(v); } }); return bb.isEmpty()?new THREE.Box3().setFromObject(m):bb; };
  const b=skinBox(); m.scale.multiplyScalar(1.8/Math.max(.01,b.max.y-b.min.y));
  const b2=skinBox(); m.position.y-=b2.min.y; m.position.x-=(b2.min.x+b2.max.x)/2; m.position.z-=(b2.min.z+b2.max.z)/2;
  const mixer=new THREE.AnimationMixer(m), acts={};
  let yaw=0, drag=null; box.addEventListener('pointerdown',e=>{ drag=e.clientX; try{ box.setPointerCapture(e.pointerId); }catch(_){} });
  box.addEventListener('pointermove',e=>{ if(drag!==null){ yaw+=(e.clientX-drag)*.012; drag=e.clientX; } }); box.addEventListener('pointerup',()=>drag=null);
  let last=performance.now(); (function tick(now){ const dt=Math.min(.05,(now-last)/1000); last=now; mixer.update(dt); if(drag===null) yaw+=dt*.25; turn.rotation.y=yaw; cam.lookAt(0,.95,0); r.render(sc,cam); requestAnimationFrame(tick); })(last);
  if(map.missing.length>4){ note('השלד של הדמות לא זוהה עד הסוף, אז היא עומדת בלי תנועה. (חסרים: '+map.missing.slice(0,6).join(', ')+')'); return {model:m,map}; }
  const [an,mv]=await Promise.all([load(base+'people/anims.glb'),load(base+'people/moves.glb').catch(()=>null)]);
  [['Idle',an],['Walk',an],['Wave',an]].concat(mv?[['Dance',mv],['Dance2',mv],['Jump',mv],['Cheer',mv]]:[]).forEach(([n,g])=>{ const c=g.animations.find(a=>a.name===n); if(c) acts[n]=mixer.clipAction(retarget(c,g.scene,m)); });
  const play=n=>{ const a=acts[n]; if(!a) return; Object.values(acts).forEach(x=>x!==a&&x.fadeOut(.25)); a.reset().fadeIn(.25).play(); if(movesBox) movesBox.querySelectorAll('button').forEach(x=>x.classList.toggle('on',x.dataset.n===n)); };
  if(movesBox) Object.keys(acts).forEach(n=>{ const bt=document.createElement('button'); bt.textContent=LABEL[n]||n; bt.dataset.n=n; bt.addEventListener('click',()=>play(n)); movesBox.appendChild(bt); });
  play(acts.Dance?'Dance':'Idle');
  return {model:m,map,play,acts};
};
})();
