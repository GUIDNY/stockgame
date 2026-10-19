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
        rule=(r>g)&(g>b)&(lum>.36)&(lum<.93)&(sat>.12)&(sat<.6)&((r-g)<.36)&((g-b)>.04)&~hair
        # the body's own skin tone, then every pixel near it in hue (lighter and darker paint alike): the rule alone
        # left the painted highlights and the edges of each small island out, and they showed as pale blotches
        # the reference tone comes from the face only (khaki trousers pass the rule too), and the match is on the
        # whole colour, not just its hue
        ref=np.median(a[rule&(head>.5)],0); dist=np.linalg.norm(a-ref,axis=-1)/2.2
        im=lambda m:Image.fromarray((m*255).astype('uint8')); ar=lambda i:np.asarray(i)>127
        seed=rule&(dist<.075)
        skin=ar(im(seed).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3)))&~hair   # close the pin holes
        # grown out from there, a few pixels at a time, over skin-coloured paint and the black gutter between islands:
        # never a jump to a separate island (a beige sweater is skin-coloured too)
        room=((dist<.09)|(lum<.08))&~hair
        for _ in range(8): skin=skin|(ar(im(skin).filter(ImageFilter.MaxFilter(3)))&room)
        def soft(m,er=True): i=im(m); i=i.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3)) if er else i; return np.asarray(i.filter(ImageFilter.GaussianBlur(.6)))
        out=np.zeros((1024,1024,3),np.uint8); out[...,0]=soft(hair); out[...,1]=soft(skin,False)
        Image.fromarray(out).save(OUT+name+'.png',optimize=True); print(name,os.path.getsize(OUT+name+'.png')//1024,'KB hair',round(hair.mean(),3),'skin',round(skin.mean(),3))
