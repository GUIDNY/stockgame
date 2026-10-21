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
/* The crowd's clips are made for a T-pose: arms straight out to the sides, hands palm down. A figure drawn with its arms
   angled down (an A-pose, as the image model often draws them) would take each turn from the wrong start: arms flung
   out, hands across the face. So for as long as the clips are being fitted to it, its arms are lifted straight out
   (and its hands set straight on and palm down); then they go back. The skin is never moved: arms that hang from the
   start bend far less at the shoulder to come down than arms made straight out would (those crush the sleeve into
   the chest). Run after mapByShape (it finds the arms by name); returns a function that puts the arms back. */
function tposeArms(model,fwd){
  model.updateMatrixWorld(true);
  const by=n=>{ let b=null; model.traverse(o=>{ if(!b&&o.isBone&&o.name==='mixamorig'+n) b=o; }); return b; };
  const kept=[], q=new THREE.Quaternion(), pq=new THREE.Quaternion(), wq=new THREE.Quaternion();
  const turn=(bone,qw)=>{ kept.push([bone,bone.quaternion.clone()]); bone.getWorldQuaternion(wq); bone.parent.getWorldQuaternion(pq);
    bone.quaternion.copy(pq.invert().multiply(qw.multiply(wq))); bone.updateMatrixWorld(true); };
  const aim=(bone,from,to,want)=>{ const d=P(to).sub(P(from)).normalize(); if(d.dot(want)<.995) turn(bone,q.setFromUnitVectors(d,want)); };
  ['Left','Right'].forEach(s=>{ const arm=by(s+'Arm'), fore=by(s+'ForeArm'), hand=by(s+'Hand'); if(!arm||!fore||!hand) return;
    const d=P(fore).sub(P(arm)); d.y=0; if(d.lengthSq()<1e-8) return; d.normalize();   // out to the side, as it already leans
    aim(arm,arm,fore,d); aim(fore,fore,hand,d);
    const mid=by(s+'HandMiddle1'), ix=by(s+'HandIndex1'), pk=by(s+'HandPinky1'); if(!mid) return;
    aim(hand,hand,mid,d);
    if(ix&&pk&&fwd){ const a=P(ix).sub(P(pk)); a.addScaledVector(d,-a.dot(d)); const w=fwd.clone().setY(0); w.addScaledVector(d,-w.dot(d));
      if(a.lengthSq()>1e-10&&w.lengthSq()>1e-10){ a.normalize(); w.normalize(); if(a.dot(w)<.995) turn(hand,q.setFromUnitVectors(a,w)); } } });
  return ()=>{ for(let i=kept.length-1;i>=0;i--) kept[i][0].quaternion.copy(kept[i][1]); model.updateMatrixWorld(true); };
}
/* The figure given the Meshy figures' own skeleton, so it moves exactly as they do. The rig Tripo makes is another
   shape (its root on the floor, a short spine, extra bones at the hem), and no translation from one to the other is
   quite right. So a Meshy figure (the donor, a fresh copy) is posed into this one: its joints put where this figure's
   are (hips, the spine spread between hips and neck as on the donor, shoulders, elbows, wrists, knees, ankles), each
   bone turned so it points at its next joint, then its body, carried along, lies over this one's. Every vertex here
   takes its bones and weights from the donor's skin nearest it; the donor's bones move into this figure, the mesh is
   bound to them as it stands, and Tripo's bones go. Run after mapByShape, with the figure turned to face +z. */
const deq=a=>{ if(!a||!a.normalized||a.array instanceof Float32Array) return a; const d=a.array instanceof Int8Array?127:a.array instanceof Uint8Array?255:a.array instanceof Int16Array?32767:a.array instanceof Uint16Array?65535:1;
  const f=new Float32Array(a.array.length); for(let i=0;i<f.length;i++) f[i]=Math.max(a.array[i]/d,-1); return new THREE.BufferAttribute(f,a.itemSize); };
function reskin(model,donor){
  model.updateMatrixWorld(true);
  const named=(root,n)=>{ let b=null; root.traverse(o=>{ if(!b&&o.isBone&&o.name==='mixamorig'+n) b=o; }); return b; };
  const sp=n=>{ const b=named(model,n); return b?P(b):null; };
  const hipsS=sp('Hips'), neckS=sp('Neck')||sp('Head'), headS=sp('Head');
  if(!hipsS||!neckS||!headS) return false;
  // the donor as big as this figure, standing where it stands
  const dH=named(donor,'Hips'), dNeck=named(donor,'Neck'), dHead=named(donor,'Head'); if(!dH||!dNeck||!dHead) return false;
  donor.updateMatrixWorld(true);
  const dl0=named(donor,'LeftUpLeg'), dr0=named(donor,'RightUpLeg'), sl0=sp('LeftUpLeg'), sr0=sp('RightUpLeg');
  if(dl0&&dr0&&sl0&&sr0){ const a1=P(dl0).sub(P(dr0)), a2=sl0.clone().sub(sr0); donor.rotation.y+=Math.atan2(a1.z,a1.x)-Math.atan2(a2.z,a2.x); donor.updateMatrixWorld(true); }
  const k=(headS.y-hipsS.y)/Math.max(1e-4,P(dHead).y-P(dH).y);
  donor.scale.multiplyScalar(k); donor.updateMatrixWorld(true);
  // where each donor joint goes: the limbs and head on this figure's own joints, the spine along its hips-neck line
  const T={}; ['Hips','Head','LeftShoulder','LeftArm','LeftForeArm','LeftHand','RightShoulder','RightArm','RightForeArm','RightHand',
    'LeftUpLeg','LeftLeg','LeftFoot','LeftToeBase','RightUpLeg','RightLeg','RightFoot','RightToeBase'].forEach(n=>{ const v=sp(n); if(v) T[n]=v; });
  const d0=P(dH), d1=P(dNeck), dl=d1.clone().sub(d0); T.Neck=neckS;
  ['Spine','Spine1','Spine2'].forEach(n=>{ const b=named(donor,n); if(!b) return; const f=P(b).sub(d0).dot(dl)/dl.lengthSq(); T[n]=hipsS.clone().lerp(neckS,f); });
  const next={Hips:null,LeftShoulder:'LeftArm',LeftArm:'LeftForeArm',LeftForeArm:'LeftHand',RightShoulder:'RightArm',RightArm:'RightForeArm',RightForeArm:'RightHand',
    LeftUpLeg:'LeftLeg',LeftLeg:'LeftFoot',LeftFoot:'LeftToeBase',RightUpLeg:'RightLeg',RightLeg:'RightFoot',RightFoot:'RightToeBase',Neck:'Head'};
  // the donor's spine goes Hips > Spine2 > Spine1 > Spine > Neck (its own names): each points at the next one up
  const up=[]; for(let b=named(donor,'Hips');b;){ const c=b.children.find(x=>x.isBone&&/Spine|Neck|Head$/.test(x.name)); if(!c) break; up.push([b,c]); b=c; }
  const q=new THREE.Quaternion(), wq=new THREE.Quaternion(), pq=new THREE.Quaternion();
  const place=(b,v)=>{ b.parent.updateMatrixWorld(true); b.position.copy(b.parent.worldToLocal(v.clone())); b.updateMatrixWorld(true); };
  const aim=(b,c,v0,v1)=>{ const d=P(c).sub(P(b)).normalize(), w=v1.clone().sub(v0).normalize(); if(d.lengthSq()<1e-8||w.lengthSq()<1e-8) return;
    q.setFromUnitVectors(d,w); b.getWorldQuaternion(wq); b.parent.getWorldQuaternion(pq); b.quaternion.copy(pq.invert().multiply(q.multiply(wq))); b.updateMatrixWorld(true); };
  const nameOf=b=>b.name.replace(/^mixamorig/,'');
  const order=[]; named(donor,'Hips').traverse(o=>{ if(o.isBone) order.push(o); });   // parents before children
  order.forEach(b=>{ const n=nameOf(b); if(T[n]) place(b,T[n]);
    const sc=up.find(([x])=>x===b); if(sc&&T[n]&&T[nameOf(sc[1])]) aim(b,sc[1],T[n],T[nameOf(sc[1])]);
    const nx=next[n]; if(nx&&T[n]&&T[nx]){ const c=named(donor,nx); if(c) aim(b,c,T[n],T[nx]); } });
  donor.updateMatrixWorld(true);
  // the donor's skin where its bones now put it, in the world, with each vertex's bones
  let dm=null; donor.traverse(o=>{ if(o.isSkinnedMesh&&(!dm||o.geometry.attributes.position.count>dm.geometry.attributes.position.count)) dm=o; }); if(!dm) return false;
  const dg=dm.geometry; dg.setAttribute('position',deq(dg.attributes.position)); dg.setAttribute('skinWeight',deq(dg.attributes.skinWeight));
  const dpa=dg.attributes.position, dsi=dg.attributes.skinIndex, dsw=dg.attributes.skinWeight, N=dpa.count, DP=new Float32Array(N*3), v=V();
  for(let i=0;i<N;i++){ v.fromBufferAttribute(dpa,i); dm.boneTransform(i,v); v.applyMatrix4(dm.matrixWorld); DP[i*3]=v.x; DP[i*3+1]=v.y; DP[i*3+2]=v.z; }
  // which body part each point belongs to, on both figures: on the donor, its heaviest bone; here, the heaviest of
  // Tripo's bones, as named by mapByShape (the fingers are the hand, an unnamed bone is the part it hangs from). The
  // donor names its spine the other way up (Spine2 lowest), so the two are matched by place, not by name.
  const dbones=dm.skeleton.bones, dn=b=>b.name.replace(/^mixamorig/,'');
  const heavy=(si,sw,i)=>{ let bi=si.getX(i), bw=sw.getX(i); [['getY'],['getZ'],['getW']].forEach(([g])=>{ if(sw[g](i)>bw){ bw=sw[g](i); bi=si[g](i); } }); return bi; };
  const SPINE={Spine:'Spine2',Spine1:'Spine1',Spine2:'Spine'};
  const part=b=>{ while(b&&b.isBone&&!/^mixamorig/.test(b.name)) b=b.parent; if(!b||!b.isBone) return 'Hips'; let n=b.name.replace(/^mixamorig/,'');
    if(/Hand(Thumb|Index|Middle|Ring|Pinky)/.test(n)) n=n.replace(/Hand.*/,'Hand'); return SPINE[n]||n; };
  const near=new Map(); dbones.forEach(b=>{ const n=dn(b), set=new Set([n]); if(b.parent&&b.parent.isBone) set.add(dn(b.parent)); b.children.forEach(c=>{ if(c.isBone) set.add(dn(c)); }); near.set(n,set); });
  const byPart=new Map(); for(let i=0;i<N;i++){ const n=dn(dbones[heavy(dsi,dsw,i)]); let L=byPart.get(n); if(!L) byPart.set(n,L=[]); L.push(i); }
  const pool=new Map();   // the donor points a part may take from: its own, and the parts either side of its joints
  const cands=n=>{ if(pool.has(n)) return pool.get(n); const L=[]; (near.get(n)||new Set(['Hips'])).forEach(m=>{ (byPart.get(m)||[]).forEach(i=>L.push(i)); }); pool.set(n,L); return L; };
  const meshes=[]; model.traverse(o=>{ if(o.isSkinnedMesh) meshes.push(o); });
  meshes.forEach(o=>{ const g=o.geometry, pa=g.attributes.position, osi=g.attributes.skinIndex, osw=g.attributes.skinWeight, ob=o.skeleton.bones, n=pa.count, SI=new Uint16Array(n*4), SW=new Float32Array(n*4), best=[];
    for(let i=0;i<n;i++){ v.fromBufferAttribute(pa,i); o.boneTransform(i,v); v.applyMatrix4(o.matrixWorld);
      const L=cands(part(ob[heavy(osi,osw,i)])); best.length=0;
      for(const j of L){ const dx=DP[j*3]-v.x, dy=DP[j*3+1]-v.y, dz=DP[j*3+2]-v.z, d2=dx*dx+dy*dy+dz*dz;
        if(best.length<4||d2<best[best.length-1][0]){ best.push([d2,j]); best.sort((p1,p2)=>p1[0]-p2[0]); if(best.length>4) best.pop(); } }
      const acc=new Map();
      best.forEach(([d2,j])=>{ const wgt=1/(Math.sqrt(d2)+1e-4); [['getX'],['getY'],['getZ'],['getW']].forEach(([gg])=>{ const w=dsw[gg](j); if(w>0){ const bi=dsi[gg](j); acc.set(bi,(acc.get(bi)||0)+w*wgt); } }); });
      const top=[...acc.entries()].sort((x,y)=>y[1]-x[1]).slice(0,4), sum=top.reduce((t,x)=>t+x[1],0)||1;
      if(!top.length) top.push([dbones.indexOf(named(donor,'Hips')),1]);
      top.forEach(([bi,w],e)=>{ SI[i*4+e]=bi; SW[i*4+e]=w/sum; }); }
    // the skin as Tripo's bones show it, made its own shape: positions (and normals) were in that rig's bind space,
    // which means nothing to the new one
    const M=new THREE.Matrix4(), B=new THREE.Matrix4(), Tm=new THREE.Matrix4(), Nm=new THREE.Matrix3(), na=g.attributes.normal, nn=V(), bm=(o.skeleton.update(),o.skeleton.boneMatrices);
    const NP=new Float32Array(n*3), NN=na?new Float32Array(n*3):null;
    for(let i=0;i<n;i++){ M.set(0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0); let tot=0;
      [['getX'],['getY'],['getZ'],['getW']].forEach(([gg])=>{ const w=osw[gg](i); if(w>0){ tot+=w; B.fromArray(bm,osi[gg](i)*16); for(let e=0;e<16;e++) M.elements[e]+=B.elements[e]*w; } });
      if(tot>0&&tot!==1) for(let e=0;e<16;e++) M.elements[e]/=tot;
      Tm.copy(o.bindMatrixInverse).multiply(M).multiply(o.bindMatrix);
      v.fromBufferAttribute(pa,i).applyMatrix4(Tm); NP[i*3]=v.x; NP[i*3+1]=v.y; NP[i*3+2]=v.z;
      if(na){ Nm.setFromMatrix4(Tm); nn.fromBufferAttribute(na,i).applyMatrix3(Nm).normalize(); NN[i*3]=nn.x; NN[i*3+1]=nn.y; NN[i*3+2]=nn.z; } }
    g.setAttribute('position',new THREE.Float32BufferAttribute(NP,3)); if(NN) g.setAttribute('normal',new THREE.Float32BufferAttribute(NN,3));
    g.computeBoundingBox(); g.computeBoundingSphere();
    g.setAttribute('skinIndex',new THREE.Uint16BufferAttribute(SI,4)); g.setAttribute('skinWeight',new THREE.Float32BufferAttribute(SW,4)); });
  // the donor's bones into this figure (Tripo's go), and the skin bound to them as they stand
  const old=[]; model.traverse(o=>{ if(o.isBone&&!(o.parent&&o.parent.isBone)) old.push(o); });
  old.forEach(b=>{ b.traverse(x=>{ if(x.isBone) x.name='old_'+x.name; }); });
  const dh=named(donor,'Hips'); model.attach(dh); model.updateMatrixWorld(true);
  old.forEach(b=>b.parent&&b.parent.remove(b));
  const skel=new THREE.Skeleton(dbones); meshes.forEach(o=>{ o.bind(skel); });
  return true;
}
window.scanMap=mapByShape; window.scanFit=fitFigure; window.scanTpose=tposeArms; window.scanReskin=reskin;

function retarget(clip,src,tgt){
  const W=root=>{ root.updateMatrixWorld(true); const m={}; root.traverse(o=>{ if(/^mixamorig/.test(o.name)) m[o.name]={b:o,w:o.getWorldQuaternion(new THREE.Quaternion())}; }); return m; };
  const S=W(src), T=W(tgt), R={}; Object.keys(T).forEach(n=>{ if(S[n]) R[n]=S[n].w.clone().invert().multiply(T[n].w); });
  const parentR=n=>{ const p=T[n].b.parent; if(p&&R[p.name]) return R[p.name]; return S[n].b.parent.getWorldQuaternion(new THREE.Quaternion()).invert().multiply(p.getWorldQuaternion(new THREE.Quaternion())); };
  const tracks=[], q=new THREE.Quaternion(), ip=new THREE.Quaternion(), ws=o=>o?o.getWorldScale(V()).y:1;
  // the hips' travel (a jump's lift), scaled from one figure's hip height to the other's, in each one's own units
  const sH=S.mixamorigHips, tH=T.mixamorigHips, sLeg=S.mixamorigLeftUpLeg||sH, tLeg=T.mixamorigLeftUpLeg||tH;
  const k=sH&&tH?P(tLeg.b).y/Math.max(1e-4,P(sLeg.b).y):1;   // both figures stand on y = 0
  clip.tracks.forEach(t=>{ const i=t.name.lastIndexOf('.'), bn=t.name.slice(0,i), prop=t.name.slice(i+1);
    if(prop==='position'&&bn==='mixamorigHips'&&sH&&tH){ const sr=sH.b.position, tr=tH.b.position, v=t.values.slice();
      // each key's move from the source's rest, taken into the world (its parent may lie on its side), sized to this
      // figure, and back into this figure's own hip space
      const sq=sH.b.parent.getWorldQuaternion(new THREE.Quaternion()), tq=sH.b.parent?tH.b.parent.getWorldQuaternion(new THREE.Quaternion()).invert():new THREE.Quaternion(), f=ws(sH.b.parent)*k/ws(tH.b.parent), d=V();
      for(let j=0;j<v.length;j+=3){ d.set(v[j]-sr.x,v[j+1]-sr.y,v[j+2]-sr.z).applyQuaternion(sq).applyQuaternion(tq).multiplyScalar(f); v[j]=tr.x+d.x; v[j+1]=tr.y+d.y; v[j+2]=tr.z+d.z; }
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
  if(prep) prep(m); const map=mapByShape(m); m.visible=false;
  if(map.fwd) m.rotation.y=Math.atan2(-map.fwd.x,map.fwd.z);   // face the camera
  m.updateMatrixWorld(true);
  if(!map.missing.filter(n=>!/ToeBase|Shoulder|Spine2|Neck/.test(n)).length){ try{ const dn=await load(base+'people/MESHY-1.glb?v=y4'); reskin(m,dn.scene); }catch(e){ console.warn('reskin',e); } }
  const untpose=tposeArms(m,null);
  fitFigure(m,1.8);
  const mixer=new THREE.AnimationMixer(m), acts={};
  let yaw=0, drag=null; box.addEventListener('pointerdown',e=>{ drag=e.clientX; try{ box.setPointerCapture(e.pointerId); }catch(_){} });
  box.addEventListener('pointermove',e=>{ if(drag!==null){ yaw+=(e.clientX-drag)*.012; drag=e.clientX; } }); box.addEventListener('pointerup',()=>drag=null);
  let last=performance.now(); (function tick(now){ const dt=Math.min(.05,(now-last)/1000); last=now; mixer.update(dt); if(drag===null) yaw+=dt*.25; turn.rotation.y=yaw; cam.lookAt(0,.95,0); r.render(sc,cam); requestAnimationFrame(tick); })(last);
  if(map.missing.filter(n=>!/ToeBase|Shoulder|Spine2|Neck/.test(n)).length){ untpose(); m.visible=true; note('השלד של הדמות לא זוהה עד הסוף, אז היא עומדת בלי תנועה. (חסרים: '+map.missing.slice(0,6).join(', ')+')'); return {model:m,map}; }
  const [an,mv]=await Promise.all([load(base+'people/anims.glb'),load(base+'people/moves.glb').catch(()=>null)]);
  // fitted while the arms are out; then they go back, and only then does the mixer take the bones' rest
  const clips=[['Idle',an],['Walk',an],['Wave',an]].concat(mv?[['Dance',mv],['Dance2',mv],['Jump',mv],['Cheer',mv]]:[]).map(([n,g])=>{ const c=g.animations.find(a=>a.name===n); return c&&[n,retarget(c,g.scene,m)]; }).filter(Boolean);
  untpose(); m.visible=true; clips.forEach(([n,c])=>{ acts[n]=mixer.clipAction(c); });
  const play=n=>{ const a=acts[n]; if(!a) return; Object.values(acts).forEach(x=>x!==a&&x.fadeOut(.25)); a.reset().fadeIn(.25).play(); if(movesBox) movesBox.querySelectorAll('button').forEach(x=>x.classList.toggle('on',x.dataset.n===n)); };
  if(movesBox) Object.keys(acts).forEach(n=>{ const bt=document.createElement('button'); bt.textContent=LABEL[n]||n; bt.dataset.n=n; bt.addEventListener('click',()=>play(n)); movesBox.appendChild(bt); });
  play(acts.Dance?'Dance':'Idle');
  return {model:m,map,play,acts};
};
})();
