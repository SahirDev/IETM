# Load Landing_Gear 1.glb: per-part triangles in the assembly frame at a given animation key.
import json,struct,numpy as np
def load(path, key=72):
    d=open(path,'rb').read()
    clen=struct.unpack('<I',d[12:16])[0]; j=json.loads(d[20:20+clen])
    off=20+clen; binb=d[off+8:]
    acc=j['accessors']; bv=j['bufferViews']
    def arr(i):
        a=acc[i]; v=bv[a['bufferView']]; n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
        dt={5126:np.float32,5125:np.uint32,5123:np.uint16,5121:np.uint8}[a['componentType']]
        o=v.get('byteOffset',0)+a.get('byteOffset',0)
        stride=v.get('byteStride',0); cnt=a['count']
        x=np.frombuffer(binb,dtype=dt,count=cnt*n,offset=o)
        return x.reshape(cnt,n) if n>1 else x
    nodes=j['nodes']
    pose={}
    for c in j['animations'][0]['channels']:
        s=j['animations'][0]['samplers'][c['sampler']]
        out=arr(s['output']); pose.setdefault(c['target']['node'],{})[c['target']['path']]=out[key]
    def qmat(q):
        x,y,z,w=q
        return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
    parts={}
    for i,n in enumerate(nodes):
        if 'mesh' not in n: continue
        p=pose.get(i,{})
        R=qmat(p.get('rotation',n.get('rotation',[0,0,0,1]))); T=np.array(p.get('translation',n.get('translation',[0,0,0])))
        tris=[]
        for prim in j['meshes'][n['mesh']]['primitives']:
            P=arr(prim['attributes']['POSITION']).astype(np.float64)
            I=arr(prim['indices']).astype(np.int64).reshape(-1,3)
            W=P@R.T+T
            tris.append(W[I])
        parts[n['name']]=np.concatenate(tris)*np.array([1.0,-1.0,-1.0])  # turn upright: mount at top, wheel down
    return parts
