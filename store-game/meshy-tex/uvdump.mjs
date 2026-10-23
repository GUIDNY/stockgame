// Run from store-game/meshy-tex with @gltf-transform/core, /extensions and meshoptimizer installed: writes uv-1.json, uv-F1.json
// (each Meshy body's UVs, skin joints and weights), which skinparts.py and padtex.py read. Order: uvdump.mjs,
// skinparts.py, skinmask2.py (on the atlases as Meshy made them: it grows the skin over their black gutter), then
// padtex.py <out dir> (the atlases padded over that gutter; copy them over people/tex/).
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions'; import { MeshoptDecoder } from 'meshoptimizer'; import fs from 'fs';
await MeshoptDecoder.ready;
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({'meshopt.decoder':MeshoptDecoder});
for(const body of ['1','F1']){
  const doc=await io.read(`../../public/store-game/people/MESHY-${body}.glb`), root=doc.getRoot();
  const skin=root.listSkins()[0], names=skin.listJoints().map(j=>j.getName());
  const out={names,prims:[]};
  for(const m of root.listMeshes()) for(const p of m.listPrimitives()){
    const g=a=>{ const A=p.getAttribute(a); if(!A) return null; const n=A.getCount(), s=A.getElementSize(), r=[]; const t=[]; for(let i=0;i<n;i++){ A.getElement(i,t); r.push(...t); } return r; };
    out.prims.push({uv:g('TEXCOORD_0'),j:g('JOINTS_0'),w:g('WEIGHTS_0'),idx:Array.from(p.getIndices().getArray()),tex:!!(p.getMaterial()&&p.getMaterial().getBaseColorTexture())});
  }
  fs.writeFileSync(`uv-${body}.json`,JSON.stringify(out)); console.log(body,names.length,out.prims.map(p=>[p.uv&&p.uv.length/2,p.idx.length/3,p.tex]));
}
