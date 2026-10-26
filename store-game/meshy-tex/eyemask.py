# eyemask.py : where each Meshy body's eyes sit in its texture atlas (eyes-1.png, eyes-F1.png), so the hair colour
# leaves them alone. hairmask.py takes "hair" to be the dark paint on the head, and the pupils, the lashes and the
# lash line are dark paint too: a blue or blond hair choice turned them blue or blond (review 26.10 item 7).
# Reads uv-<body>.json (uvdump.mjs). The eyes are found on the model, not in the paint: every face triangle (mostly)
# inside an ellipse round each eye, seen from the front, in fractions of the head's box (checked by rendering the
# face with the mask over it). The brows stay outside the ellipses: they follow the hair colour.
import json, numpy as np
from PIL import Image, ImageDraw, ImageFilter
EYES={'1':[(.295,.40,.095,.075),(.555,.40,.095,.075)],'F1':[(.37,.525,.11,.068),(.63,.525,.11,.068)]}   # (x, y, rx, ry)
for body,ell in EYES.items():
    p=json.load(open(f'uv-{body}.json')); d=p['prims'][0]
    P=np.array(d['pos'],np.float32).reshape(-1,3); UV=np.array(d['uv'],np.float32).reshape(-1,2); UV/=UV.max()   # both quantized in the file
    I=np.array(d['idx']).reshape(-1,3); J=np.array(d['j']).reshape(-1,4); W=np.array(d['w'],np.float32).reshape(-1,4)
    hi=[i for i,n in enumerate(p['names']) if n.lower().endswith('head')][0]; head=((J==hi)*W).sum(1)>.6
    mn,mx=P[head].min(0),P[head].max(0); S=max(mx[0]-mn[0],mx[1]-mn[1]); X=(P[:,0]-mn[0])/S; Y=(P[:,1]-mn[1])/S
    ins=np.zeros(len(P),bool)
    for cx,cy,rx,ry in ell: ins|=((X-cx)/rx)**2+((Y-cy)/ry)**2<1
    ins&=head&(P[:,2]>mx[2]-(mx[2]-mn[2])*.4)   # the front of the head only
    m=Image.new('L',(1024,1024),0); dr=ImageDraw.Draw(m)
    for t in I[ins[I].sum(1)>=2]: dr.polygon([(u*1024,v*1024) for u,v in UV[t]],fill=255)
    m.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(.8)).save(f'eyes-{body}.png'); print(body,'eye triangles',int((ins[I].sum(1)>=2).sum()))
