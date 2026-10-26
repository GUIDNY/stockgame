# skinmask3.py : the skin in each outfit's mask, the last pass (after skinmask2.py and eyemask.py). Review 26.10 item 8:
# with a dark skin, stripes and pale patches on the neck and hands, and dark blotches on some tops.
# - Skin painted just past the neck and hands (the V of a hood, the wrist under a cuff) sits on triangles the part map
#   gives to the chest and forearms, where skinmask2.py only took paint very close to the face's tone: it is taken
#   within 3% of the figure's height of the neck, head and hands, and paint close to this figure's own skin within 7%.
# - Cloth that earlier passes took for skin (a yellow or orange top, white cuffs, a print) is let go.
# Reads uv-<body>.json (uvdump.mjs), parts-<body>.npy (skinparts.py) and eyes-<body>.png; checked by rendering each
# outfit with the darkest tone. Usage: python3 skinmask3.py [mask dir] --write
import json, numpy as np, glob, os, sys
from PIL import Image, ImageDraw, ImageFilter
from scipy.spatial import cKDTree
T='../../public/store-game/people/tex/'; args=[a for a in sys.argv[1:] if not a.startswith('--')]; MASK=args[0] if args else T+'mask/'; EYES='.'
for body in ('1','F1'):
    d=json.load(open(f'uv-{body}.json')); p=d['prims'][0]; names=[n.replace('mixamorig:','') for n in d['names']]
    P=np.array(p['pos'],np.float32).reshape(-1,3); UV=np.array(p['uv'],np.float32).reshape(-1,2); UV/=UV.max()
    I=np.array(p['idx']).reshape(-1,3); J=np.array(p['j']).reshape(-1,4); W=np.array(p['w'],np.float32).reshape(-1,4)
    bare=[names.index(n) for n in ('Neck','Head','LeftHand','RightHand')]   # the neck shares its weight with the head: on its own it held almost no vertex
    wb=sum(((J==k)*W).sum(1) for k in bare); core=wb>.6
    H=P[:,1].max()-P[:,1].min(); R=.03*H
    dd,_=cKDTree(P[core]).query(P[I].mean(1))
    def zone(sel):
        z=Image.new('L',(1024,1024),0); dr=ImageDraw.Draw(z)
        for t in I[sel]: dr.polygon([(u*1024,v*1024) for u,v in UV[t]],fill=255)
        return np.asarray(z)>127
    Z=zone(dd<R); Z2=zone(dd<R*2.4)   # Z2: the V of a hood and a low neckline, down the chest
    E=np.asarray(Image.open(f'{EYES}/eyes-{body}.png'))>60
    for f in sorted(glob.glob(T+body+'-*.jpg')):
        if f.endswith('-m.jpg'): continue
        name=os.path.basename(f)[:-4]
        a=np.asarray(Image.open(f).convert('RGB').resize((1024,1024),Image.LANCZOS),np.float32)/255
        m=np.asarray(Image.open(MASK+name+'.png').convert('RGB')).copy(); hair=m[...,0]>127; sk=m[...,1]>127
        r,g,b=a[...,0],a[...,1],a[...,2]; mx=a.max(2); mn=a.min(2); lum=.3*r+.59*g+.11*b; sat=(mx-mn)/np.maximum(mx,1e-3)
        ref=np.median(a[sk&Z],0); dist=np.linalg.norm(a-ref,axis=-1)
        warm=~sk&~hair&~E&(r>=g*.97)&(g>=b*.9)&(sat>.06)&(sat<.6)
        add=(Z&warm&(lum>.25)&(dist<.3))|(Z2&warm&(lum>.5)&(dist<.16))   # further out, only paint close to this figure's own skin
        # an island's soft edge, paint blending to the gutter, next to skin: grow two texels over it
        new=sk|add; ar=lambda x:np.asarray(Image.fromarray((x*255).astype('uint8')).filter(ImageFilter.MaxFilter(3)))>127
        for _ in range(2): new=new|(ar(new)&Z2&~hair&~E&(dist<.45))
        # and the other way: cloth that earlier passes took for skin (a yellow or orange top, a white cuff, a printed logo)
        # turned dark with a dark skin. Off the head and hands, paint skin never is: very saturated, near white or grey
        Pt=np.load(f'parts-{body}.npy'); bare=(Pt==1)|(Pt==2)
        cloth=~bare&(lum>=.1)&((sat>.62)|(sat<.1)|(lum>.93))
        g=np.maximum(m[...,1],(new*255).astype('uint8')); g[cloth]=0
        print('   cloth taken out',int((cloth&(m[...,1]>127)).sum()),end='  ')
        m[...,1]=g
        if '--write' in sys.argv: Image.fromarray(m).save(MASK+name+'.png',optimize=True)
        else: Image.fromarray(m).save(f'se-{name}.png')
        print(name,'ref',np.round(ref,2),'added',int((new&~sk).sum()))
