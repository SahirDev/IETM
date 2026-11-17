import numpy as np, cv2, sys
sys.path.insert(0,__import__('os').path.dirname(__import__('os').path.abspath(__file__)))
def look(yaw, pitch):
    yaw=np.radians(yaw); pitch=np.radians(pitch)
    f=np.array([np.cos(pitch)*np.sin(yaw), -np.sin(pitch), np.cos(pitch)*np.cos(yaw)])  # view dir
    up=np.array([0,1,0.]); r=np.cross(up,f); r/=np.linalg.norm(r); u=np.cross(f,r)
    return np.stack([r,u,f])
def render(parts, offsets, view, size=(1600,1600), margin=0.08, ghost=(), only=None, light=np.array([-0.4,0.7,-0.6])):
    """Line-art render. parts: name->(N,3,3). offsets: name->vec3. Returns (rgb, id image, names, project fn)."""
    names=[n for n in parts if only is None or n in only or n in ghost]
    allp=[]; tri_all=[]; pid=[]
    for k,n in enumerate(names):
        t=parts[n]+offsets.get(n,0)
        tri_all.append(t); pid.append(np.full(len(t),k))
    T=np.concatenate(tri_all); PID=np.concatenate(pid)
    V=T.reshape(-1,3)@view.T
    W,H=size
    mn=V[:,:2].min(0); mx=V[:,:2].max(0); span=(mx-mn).max()
    s=(1-2*margin)*min(W,H)/span; c=(mn+mx)/2
    def proj(p):
        q=np.asarray(p)@view.T
        return np.stack([(q[...,0]-c[0])*s+W/2, H/2-(q[...,1]-c[1])*s],-1), q[...,2]
    P2,Z=proj(T)
    # normals & shading
    n=np.cross(T[:,1]-T[:,0],T[:,2]-T[:,0]); ln=np.linalg.norm(n,axis=1,keepdims=True); n=n/np.maximum(ln,1e-12)
    nv=n@view.T
    flip=nv[:,2]>0; nv[flip]*=-1  # face the camera
    L=light/np.linalg.norm(light); Lv=L@view.T
    shade=np.clip(0.55+0.45*(nv@(-Lv)),0,1)
    depth=Z.mean(1); order=np.argsort(-depth)   # far first
    idimg=np.full((H,W),-1,np.int32); shimg=np.ones((H,W),np.float32); nimg=np.zeros((H,W,3),np.float32); 
    pts=np.round(P2*4).astype(np.int32)  # 2 bits subpixel
    for t in order:
        poly=pts[t]
        pid=int(PID[t]); sh=float(shade[t]); nn=tuple(float(x) for x in nv[t])
        # polylines first cover the hairline gaps between neighbouring triangles
        cv2.polylines(idimg,[poly],True,pid,1,cv2.LINE_8,2); cv2.fillConvexPoly(idimg,poly,pid,cv2.LINE_8,2)
        cv2.polylines(shimg,[poly],True,sh,1,cv2.LINE_8,2); cv2.fillConvexPoly(shimg,poly,sh,cv2.LINE_8,2)
        cv2.polylines(nimg,[poly],True,nn,1,cv2.LINE_8,2); cv2.fillConvexPoly(nimg,poly,nn,cv2.LINE_8,2)
    bg=idimg<0
    # edges: id change, normal crease, depth jump
    e=np.zeros((H,W),bool)
    for dy,dx in ((0,1),(1,0),(1,1)):
        a=idimg; b=np.roll(np.roll(idimg,dy,0),dx,1)
        e|=(a!=b)
        na=nimg; nb=np.roll(np.roll(nimg,dy,0),dx,1)
        crease=(na*nb).sum(-1)<0.72
        e|=crease&~bg
    e=cv2.dilate(e.astype(np.uint8),np.ones((2,2),np.uint8))>0
    gray=np.where(bg,1.0,0.80+0.20*shimg)
    ghostids=[k for k,nm in enumerate(names) if nm in ghost]
    gmask=np.isin(idimg,ghostids)
    gray=np.where(gmask,np.where(bg,1,0.96),gray)
    img=np.where(e,np.where(gmask,0.62,0.0),gray)
    img=(np.clip(img,0,1)*255).astype(np.uint8)
    return img, idimg, names, proj

def render_aa(parts, offsets, view, size=(1600,1600), ss=2, **kw):
    W,H=size
    img,idimg,names,proj=render(parts,offsets,view,size=(W*ss,H*ss),**kw)
    small=cv2.resize(img,(W,H),interpolation=cv2.INTER_AREA)
    def proj2(p):
        a,z=proj(p); return a/ss, z
    return small, idimg[::ss,::ss], names, proj2
