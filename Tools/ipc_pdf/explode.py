# Mechanically-sensible exploded layout for Landing_Gear 1.glb (extended pose).
# Each part slides along an engineering axis (pin axis / strut axis / screw axis) from its parent's
# exploded position until it (A) clears every already-placed part by GAP in 3D, and (B) is fully off any
# part it mates with (when their silhouettes along the motion axis overlap) - e.g. a collar is slid
# completely off the strut, a nut off its screw, a pin out of its clevis.
# Coordinates are RAW glTF node-parent space (the parts' parent "F-16 Front Landing Gear Assembly").
#   +z = down (strut / wheel),  -z = up,  +x = actuator side (outboard),  -x = other side,
#   +y = forward (toward strut / torque links),  -y = aft (housing far end, screw end)
import sys, numpy as np
sys.path.insert(0,__import__('os').path.dirname(__import__('os').path.abspath(__file__)))
from glbmesh import load
VOX=0.0015; GAP=4; DMIN=0.010; STEP=0.001
X=np.array([1,0,0.]); Y=np.array([0,1,0.]); Z=np.array([0,0,1.])
def n(v): v=np.asarray(v,float); return v/np.linalg.norm(v)
FIXED=['F-16 Front Landing Gear','Main Swivel Arm']
# (part, parent, direction, extra minimum travel)
PLAN=[
 ('Gear Actuator',            None,                      X,      0.020),
 ('Main Actuator Arm',        'Gear Actuator',           Y,      0.020),
 ('Gear Actuator Piston Rod', 'Gear Actuator',           -Y,     0.020),
 ('Actuator Nut',             'Gear Actuator Piston Rod',-Y,     0.020),
 ('Secondary Actuator Arm',   'Gear Actuator',           -Z,     0.020),
 ('Pivot for Inner Arm',      None,                      X,      0.020),
 ('Fork',                     'Pivot for Inner Arm',     -Z,     0.020),
 ('Steering Top Head',        None,                      -Z,     0.020),
 ('Steering Link for servo',  'Steering Top Head',       -Z,     0.020),
 ('Gear Rotation Link',       'Steering Top Head',       -X,     0.020),
 ('Collar',                   None,                      Z,      0.020),
 ('Steering Collar',          'Collar',                  Z,      0.020),
 ('Steering Link Upper',      'Steering Collar',         Y,      0.020),
 ('Main Gear',                'Steering Collar',         Z,      0.020),
 ('Steering Link Lower',      'Main Gear',               Y,      0.020),
 ('Wheel',                    'Main Gear',               -X,     0.080),
 ('Inner Folding Arm',        None,                      n([0,-0.6,1]), 0.020),
 ('Outer Folding Arm',        'Inner Folding Arm',       Z,      0.020),
]
def sample(tris, k=60000):
    a,b,c=tris[:,0],tris[:,1],tris[:,2]
    area=np.linalg.norm(np.cross(b-a,c-a),axis=1)/2
    m=max(k, int(area.sum()/(VOX*VOX/4)))
    idx=np.random.default_rng(0).choice(len(tris),m,p=area/area.sum())
    r1=np.sqrt(np.random.default_rng(1).random(m)); r2=np.random.default_rng(2).random(m)
    p=(1-r1)[:,None]*a[idx]+(r1*(1-r2))[:,None]*b[idx]+(r1*r2)[:,None]*c[idx]
    return np.concatenate([p,tris.reshape(-1,3)])
OFF=1<<20
def keys(v): v=v.astype(np.int64)+OFF//2; return (v[:,0]<<42)|(v[:,1]<<21)|v[:,2]
def vox(p): return np.unique(np.floor(p/VOX).astype(np.int64),axis=0)
def dilate(v,r):
    o=np.array([(i,j,k) for i in range(-r,r+1) for j in range(-r,r+1) for k in range(-r,r+1) if i*i+j*j+k*k<=r*r+1])
    return np.unique((v[:,None,:]+o[None]).reshape(-1,3),axis=0)
def silhouette(p,u):
    a=n(np.cross(u,[0.31,0.57,0.76])); b=np.cross(u,a)
    q=np.floor(np.stack([p@a,p@b],1)/VOX).astype(np.int64)
    return set(map(tuple,np.unique(q,axis=0))), a, b
def solve(key=72, verbose=False):
    parts=load('/home/claude/ietm/Assets/3DModels/Landing_Gear 1.glb',key)
    pts={k:sample(v) for k,v in parts.items()}
    V={k:vox(p) for k,p in pts.items()}
    # mates at rest: within ~1.5 mm
    Vd={k:dilate(v,2) for k,v in V.items()}
    K={k:keys(v) for k,v in V.items()}
    mates={k:set() for k in parts}
    names=list(parts)
    for i,a in enumerate(names):
        ka=np.sort(keys(Vd[a]))
        for b in names[i+1:]:
            if np.isin(K[b],ka).any(): mates[a].add(b); mates[b].add(a)
    off={k:np.zeros(3) for k in parts}
    placed=list(FIXED)
    occ=np.sort(np.concatenate([keys(dilate(V[k],GAP)) for k in FIXED]))
    for name,parent,u,dm in PLAN:
        u=n(u); base=off[parent].copy() if parent else np.zeros(3)
        p=pts[name]; proj=p@u
        msil=[]
        for q in mates[name]:
            if q not in placed: continue
            pq=pts[q]+off[q]
            # silhouettes along u
            sp,a,b=silhouette(p,u)
            qq=set(map(tuple,np.unique(np.floor(np.stack([pq@a,pq@b],1)/VOX).astype(np.int64),axis=0)))
            if len(sp & qq)>3: msil.append((q,(pq@u).min(),(pq@u).max()))
        d=dm
        while True:
            P=p+base+u*d
            ok=not np.isin(keys(vox(P)),occ).any()
            if ok:
                lo=proj.min()+base@u+d; hi=proj.max()+base@u+d
                for q,qlo,qhi in msil:
                    if not (lo>=qhi+GAP*VOX or hi<=qlo-GAP*VOX): ok=False; break
            if ok: break
            d+=STEP
            if d>0.6: print('!! no solution',name); break
        off[name]=base+u*d
        placed.append(name)
        occ=np.sort(np.concatenate([occ,keys(dilate(vox(p+off[name]),GAP))]))
        if verbose: print(f'{name:26s} d={d*1000:6.1f} mm  mates={sorted(q for q,_,_ in msil)}')
    return parts, off, mates
if __name__=='__main__':
    import json
    parts,off,mates=solve(verbose=True)
    json.dump({k:list(map(float,v)) for k,v in off.items()},open('/home/claude/ipc/explode.json','w'),indent=1)
