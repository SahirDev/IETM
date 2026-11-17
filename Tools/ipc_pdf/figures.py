import sys, json, numpy as np, cv2
sys.path.insert(0,'/home/claude/tools')
from glbmesh import load; from render import render_aa, look
GLB='/home/claude/ietm/Assets/3DModels/Landing_Gear 1.glb'
DATA=json.load(open('/home/claude/ietm/Assets/IETM/Resources/IETM/landing_gear_ietm.json'))
items=DATA['items']
# hierarchy from indent
stack=[]; children={}
for it in items:
    while stack and stack[-1]['indent']>=it['indent']: stack.pop()
    it['_parent']=stack[-1]['itemNo'] if stack else None
    if stack: children.setdefault(stack[-1]['itemNo'],[]).append(it['itemNo'])
    stack.append(it)
byno={it['itemNo']:it for it in items}
def nodes_of(no):
    it=byno[no]
    if it['nodes']: return list(it['nodes'])
    out=[]
    for c in children.get(no,[]): out+=nodes_of(c)
    return out
def callno(no): return no.lstrip('0') or no

parts=load(GLB,key=72)
allnames=list(parts)
center=np.concatenate([p.reshape(-1,3) for p in parts.values()])
bmin,bmax=center.min(0),center.max(0); C=(bmin+bmax)/2; R=np.linalg.norm(bmax-bmin)/2

def explode(names, factor):
    off={}
    for n in names:
        pc=parts[n].reshape(-1,3).mean(0); d=pc-C; L=np.linalg.norm(d)
        off[n]=(d/L if L>1e-9 else np.array([0,1,0.]))*factor*R
    return off

def figure(callouts, view=(35,20), offsets=None, only=None, ghost=(), size=1400, out='fig.png'):
    """callouts: list of itemNo. Returns dict with image path and callout geometry (pixels)."""
    offsets=offsets or {}
    img,idimg,names,proj=render_aa(parts,offsets,look(*view),size=(size,size),only=only,ghost=ghost,margin=0.17)
    cv2.imwrite(out,img)
    H,W=idimg.shape
    ys,xs=np.nonzero(idimg>=0); dminx,dmaxx,dminy,dmaxy=xs.min(),xs.max(),ys.min(),ys.max()
    cx=(dminx+dmaxx)/2
    anchors=[]
    for no in callouts:
        ids=[names.index(n) for n in nodes_of(no) if n in names and n not in ghost]
        m=np.isin(idimg,ids)
        if m.sum()<5: continue
        # pick a well-inside visible pixel near the part's visible centroid
        dist=cv2.distanceTransform(m.astype(np.uint8),cv2.DIST_L2,3)
        yy,xx=np.nonzero(m); c=np.array([xx.mean(),yy.mean()])
        score=dist[yy,xx]-0.15*np.hypot(xx-c[0],yy-c[1])
        k=np.argmax(score); anchors.append((no,float(xx[k]),float(yy[k])))
    # labels in two columns left / right of the drawing, spaced vertically
    left=[a for a in anchors if a[1]<cx]; right=[a for a in anchors if a[1]>=cx]
    labels=[]
    for side,group in (('L',left),('R',right)):
        group.sort(key=lambda a:a[2])
        x = max(30, dminx-110) if side=='L' else min(W-30, dmaxx+110)
        ys_=[a[2] for a in group]; gap=44
        # spread to avoid overlaps, keep inside image
        for i in range(1,len(ys_)):
            ys_[i]=max(ys_[i],ys_[i-1]+gap)
        over=(ys_[-1]-(H-40)) if ys_ else 0
        if over>0: ys_=[y-over for y in ys_]
        for i in range(len(ys_)-2,-1,-1): ys_[i]=min(ys_[i],ys_[i+1]-gap)
        for (no,ax,ay),ly in zip(group,ys_):
            labels.append(dict(item=callno(no),ax=ax,ay=ay,lx=x,ly=ly,side=side))
    return dict(image=out,size=(W,H),labels=labels)
