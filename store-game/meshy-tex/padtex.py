# Pads each island of a Meshy outfit atlas out over the gutter round it, with its own edge colour: the texture is
# filtered (and made smaller, mipmapped) across island edges, and the black between them showed as dark and coloured
# specks along every seam. Coverage comes from the mesh's own UVs, so dark cloth is never taken for gutter.
import json, numpy as np, glob, os, sys
from PIL import Image, ImageDraw
T='../../public/store-game/people/tex/'; OUT=sys.argv[1]
def cover(body,S):
    d=json.load(open(f'uv-{body}.json')); p=d['prims'][0]
    uv=np.array(p['uv']).reshape(-1,2)*16.0034218+np.array([5e-6,4e-5]); I=np.array(p['idx']).reshape(-1,3)
    img=Image.new('L',(S,S),0); dr=ImageDraw.Draw(img)
    for t in I: dr.polygon([tuple(uv[v]*S) for v in t],fill=255)
    return np.asarray(img)>127
def pad(a,cov,n):
    a=a.astype(np.float32); have=cov.copy(); out=a*have[...,None]
    for _ in range(n):
        acc=np.zeros_like(out); cnt=np.zeros(have.shape,np.float32)
        for dy,dx in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)):
            acc+=np.roll(out,(dy,dx),(0,1)); cnt+=np.roll(have,(dy,dx),(0,1))
        new=(~have)&(cnt>0); out[new]=acc[new]/cnt[new][:,None]; have=have|new
    out[~have]=a[~have]; return out.clip(0,255).astype(np.uint8)
covs={}
for f in sorted(glob.glob(T+'*.jpg')):
    name=os.path.basename(f); body=name.split('-')[0]; im=Image.open(f).convert('RGB'); S=im.size[0]
    if (body,S) not in covs: covs[(body,S)]=cover(body,S)
    out=pad(np.asarray(im),covs[(body,S)],12 if S>=2048 else 6)
    Image.fromarray(out).save(OUT+name,quality=90,optimize=True)
    print(name,S,os.path.getsize(f)//1024,'->',os.path.getsize(OUT+name)//1024,'KB','cover',round(covs[(body,S)].mean(),3))
