# which body part covers each texel of the Meshy atlas: from the mesh itself (UVs and skin weights), drawn per triangle
import json, numpy as np, sys
from PIL import Image, ImageDraw
S=1024
def parts(body):
    d=json.load(open(f'uv-{body}.json')); p=d['prims'][0]; names=[n.replace('mixamorig:','') for n in d['names']]
    uv=np.array(p['uv']).reshape(-1,2)*16.0034218+np.array([5e-6,4e-5]); J=np.array(p['j']).reshape(-1,4); W=np.array(p['w']).reshape(-1,4); I=np.array(p['idx']).reshape(-1,3)
    grp={'Neck':1,'Head':1,'head_end':1,'headfront':1,'LeftHand':2,'RightHand':2,'LeftForeArm':3,'RightForeArm':3,'LeftArm':3,'RightArm':3}
    img=Image.new('L',(S,S),0); dr=ImageDraw.Draw(img)
    for t in I:
        acc={}
        for v in t:
            for k in range(4): acc[J[v,k]]=acc.get(J[v,k],0)+W[v,k]
        b=names[max(acc,key=acc.get)]; g=grp.get(b,0)
        if g: dr.polygon([tuple(uv[v]*S) for v in t],fill=g)
    return np.asarray(img)
if __name__=='__main__':
    for b in ('1','F1'):
        P=parts(b); np.save(f'parts-{b}.npy',P); print(b,[(k,int((P==k).sum())) for k in (1,2,3)])
        Image.fromarray((P*80).astype('uint8')).save(f'parts-{b}.png')
