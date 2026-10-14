# hairmask.py : per outfit texture of the Meshy hero, a mask for recolouring in the game: red = hair, green = skin
import numpy as np, glob, os
from PIL import Image, ImageFilter
T='/home/user/stockgame/public/store-game/people/tex/'; OUT=T+'mask/'; os.makedirs(OUT,exist_ok=True)
for body in ('1','F1'):
    head=np.asarray(Image.open(f'meshy/mask-{body}.png').resize((1024,1024)),dtype=np.float32)/255
    for f in sorted(glob.glob(T+body+'-*.jpg')):
        if f.endswith('-m.jpg'): continue
        name=os.path.basename(f)[:-4]
        a=np.asarray(Image.open(f).convert('RGB').resize((1024,1024),Image.LANCZOS),dtype=np.float32)/255
        r,g,b=a[...,0],a[...,1],a[...,2]; mx=a.max(2); mn=a.min(2); lum=.3*r+.59*g+.11*b; sat=(mx-mn)/np.maximum(mx,1e-3)
        hair=(lum<.36)&(r>=b*.9)&(head>.5)
        skin=(r>g)&(g>b)&(lum>.36)&(lum<.93)&(sat>.12)&(sat<.6)&((r-g)<.36)&((g-b)>.04)&~hair
        def soft(m): return np.asarray(Image.fromarray((m*255).astype('uint8')).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(.8)))
        out=np.zeros((1024,1024,3),np.uint8); out[...,0]=soft(hair); out[...,1]=soft(skin)
        Image.fromarray(out).save(OUT+name+'.png',optimize=True); print(name,os.path.getsize(OUT+name+'.png')//1024,'KB hair',round(hair.mean(),3),'skin',round(skin.mean(),3))
