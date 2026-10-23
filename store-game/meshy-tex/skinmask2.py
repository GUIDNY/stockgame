# The skin in each outfit's mask, completed: on the head, neck and hands (found from the mesh, skinparts.py), every
# texel of skin-coloured paint, whatever its light or shade; elsewhere (bare arms) what the first pass found, plus
# texels close to the face's own tone. Keeps the hair (red) as it was.
import numpy as np, glob, os, sys
from PIL import Image, ImageFilter
T='../../public/store-game/people/tex/'; OUT=sys.argv[1] if len(sys.argv)>1 else T+'mask/'
for body in ('1','F1'):
    P=np.load(f'parts-{body}.npy')
    for f in sorted(glob.glob(T+body+'-*.jpg')):
        if f.endswith('-m.jpg'): continue
        name=os.path.basename(f)[:-4]
        a=np.asarray(Image.open(f).convert('RGB').resize((1024,1024),Image.LANCZOS),dtype=np.float32)/255
        old=np.asarray(Image.open(T+'mask/'+name+'.png').convert('RGB'),dtype=np.float32)/255
        hair=old[...,0]>.5
        r,g,b=a[...,0],a[...,1],a[...,2]; mx=a.max(2); mn=a.min(2); lum=.3*r+.59*g+.11*b; sat=(mx-mn)/np.maximum(mx,1e-3)
        warm=(r>=g*.97)&(g>=b*.9)&(sat>.08)&(sat<.75)&(lum>.1)&(lum<.95)
        ref=np.median(a[(P==1)&warm&(lum>.35)&~hair],0)
        dist=np.linalg.norm(a-ref,axis=-1)
        headhand=((P==1)|(P==2))&warm&~hair&(dist<.42)
        arms=(P==3)&warm&~hair&(dist<.16)
        skin=headhand|arms|(old[...,1]>.5)
        ar=lambda m:np.asarray(Image.fromarray((m*255).astype('uint8')).filter(ImageFilter.MaxFilter(3)))>127
        skin=np.asarray(Image.fromarray((skin*255).astype('uint8')).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3)))>127   # pin holes closed
        # out over the dark gutter round each island, a few texels: the texture is filtered across an island's edge,
        # and a mask that fades there left a pale line round every piece of dark skin
        # and over the island's own soft edge (paint blended toward the gutter, lighter or darker than the skin)
        edge=((P==1)|(P==2)|(P==3))&~hair&(dist<.7)&~((sat>.7)&(r>.45)&(g<.35))
        for _ in range(2): skin=skin|(ar(skin)&edge)
        gut=lum<.1
        for _ in range(4): skin=skin|(ar(skin)&gut)
        im=Image.fromarray((skin*255).astype('uint8'))
        out=(old*255).astype('uint8'); out[...,1]=np.asarray(im)
        Image.fromarray(out).save(OUT+name+'.png',optimize=True)
        print(name,'ref',np.round(ref,2),'skin old',round((old[...,1]>.5).mean(),3),'new',round(skin.mean(),3))
