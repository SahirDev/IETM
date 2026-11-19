import sys, json, numpy as np, cv2
sys.path.insert(0,__import__('os').path.dirname(__import__('os').path.abspath(__file__)))
from glbmesh import load; from render import render_aa, look
from view import disp, doff, VIEW
GLB='/home/claude/ietm/Assets/3DModels/Landing_Gear 1.glb'
DATA=json.load(open('/home/claude/ietm/Assets/IETM/Resources/IETM/landing_gear_ietm.json'))
items=DATA['items']
stack=[]; children={}
for it in items:
    while stack and stack[-1]['indent']>=it['indent']: stack.pop()
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
leaves=[it['itemNo'] for it in items if not children.get(it['itemNo'])]
parts=disp(load(GLB,key=72))
EXPL=doff({e['node']:e['offset'] for e in DATA.get('explode',[])})
allnames=list(parts)

def _seg_cross(p1,p2,p3,p4):
    def o(a,b,c): return np.sign((b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]))
    return o(p1,p2,p3)!=o(p1,p2,p4) and o(p3,p4,p1)!=o(p3,p4,p2)

def place_labels(anchors, drawn, W, H, lab=24):
    free=cv2.distanceTransform((~drawn).astype(np.uint8),cv2.DIST_L2,5)
    placed=[]; out=[]
    ys,xs=np.nonzero(drawn); cx,cy=xs.mean(),ys.mean()
    # outermost anchors first: they have the simplest choices
    order=sorted(anchors,key=lambda a:-np.hypot(a[1]-cx,a[2]-cy))
    for no,ax,ay in order:
        best=None
        for r in range(40,520,20):
            for ang in np.radians(np.arange(0,360,10)):
                lx=ax+r*np.cos(ang); ly=ay+r*np.sin(ang)
                if not (lab<lx<W-lab and lab<ly<H-lab): continue
                f=free[int(ly),int(lx)]
                if f<lab: continue
                cost=r
                for (px,py,qx,qy) in placed:
                    if np.hypot(lx-qx,ly-qy)<2.2*lab: cost+=5000
                    if _seg_cross((ax,ay),(lx,ly),(px,py),(qx,qy)): cost+=900
                    # leader passing over another label
                    t=np.clip(((qx-ax)*(lx-ax)+(qy-ay)*(ly-ay))/max(1,(lx-ax)**2+(ly-ay)**2),0,1)
                    if np.hypot(ax+t*(lx-ax)-qx,ay+t*(ly-ay)-qy)<lab*0.9: cost+=1500
                # prefer labels away from the drawing centre (cleaner leaders)
                if np.hypot(lx-cx,ly-cy)<np.hypot(ax-cx,ay-cy): cost+=60
                if best is None or cost<best[0]: best=(cost,lx,ly)
            if best is not None and best[0]<r+200: break
        if best is None: continue
        _,lx,ly=best; placed.append((ax,ay,lx,ly))
        out.append(dict(item=callno(no),ax=ax,ay=ay,lx=lx,ly=ly,side='L' if lx<ax else 'R'))
    return out

def figure(callouts, exploded=True, focus=None, size=1800, out='fig.png', view=VIEW, margin=0.04):
    off=EXPL if exploded else {}
    ghost=[] if focus is None else [n for n in allnames if n not in focus]
    img,idimg,names,proj=render_aa(parts,off,look(*view),size=(size,size),ghost=ghost,margin=margin)
    H,W=idimg.shape
    if focus is not None:   # crop to the focus group
        ids=[names.index(n) for n in focus]
        ys,xs=np.nonzero(np.isin(idimg,ids)); pad=int(0.10*size)
        x0,x1=max(0,xs.min()-pad),min(W,xs.max()+pad); y0,y1=max(0,ys.min()-pad),min(H,ys.max()+pad)
        img=img[y0:y1,x0:x1]; idimg=idimg[y0:y1,x0:x1]; H,W=idimg.shape
    anchors=[]
    for no in callouts:
        ids=[names.index(n) for n in nodes_of(no) if n in names and n not in ghost]
        m=np.isin(idimg,ids)
        if m.sum()<5: continue
        dist=cv2.distanceTransform(m.astype(np.uint8),cv2.DIST_L2,3)
        yy,xx=np.nonzero(m); c=np.array([xx.mean(),yy.mean()])
        k=np.argmax(dist[yy,xx]-0.15*np.hypot(xx-c[0],yy-c[1])); anchors.append((no,float(xx[k]),float(yy[k])))
    # pad the canvas so labels can sit outside the drawing
    P=160
    canvas=np.full((H+2*P,W+2*P),255,np.uint8); canvas[P:P+H,P:P+W]=img
    anchors=[(n,x+P,y+P) for n,x,y in anchors]
    labels=place_labels(anchors,canvas<245,W+2*P,H+2*P)
    cv2.imwrite(out,canvas)
    return dict(image=out,size=(W+2*P,H+2*P),labels=labels)
