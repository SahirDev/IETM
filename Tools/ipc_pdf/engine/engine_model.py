# engine.glb -> per-part triangles in the space of turbine_01_obj (where the IETM explode / removal offsets live),
# and the display transform used for the IPC figures.
#   raw glTF (turbine_01_obj): +x = aft (exhaust), -x = front (inlet), -z = up, y = depth
#   display: x = right (front on the right, as in the IETM), y = up, z = toward the viewer's far side
import json, struct, os, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
GLB = os.path.join(REPO, 'Assets/Model/engine.glb')
DATA = os.path.join(REPO, 'Assets/IETM/Resources/IETM/engine_ietm.json')
M = np.array([[-1, 0, 0], [0, 0, -1], [0, -1, 0.]])   # display = M @ raw (right-handed, det +1)

def load(path=GLB):
    d = open(path, 'rb').read()
    clen = struct.unpack('<I', d[12:16])[0]; j = json.loads(d[20:20 + clen])
    binb = d[20 + clen + 8:]
    acc, bv = j['accessors'], j['bufferViews']
    def arr(i):
        a = acc[i]; v = bv[a['bufferView']]; n = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4}[a['type']]
        dt = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8}[a['componentType']]
        o = v.get('byteOffset', 0) + a.get('byteOffset', 0)
        x = np.frombuffer(binb, dtype=dt, count=a['count'] * n, offset=o)
        return x.reshape(a['count'], n) if n > 1 else x
    parts = {}
    for n in j['nodes']:
        if 'mesh' not in n: continue
        T = np.array(n.get('translation', [0, 0, 0]), float)   # children of turbine_01_obj: translation only
        tris = []
        for prim in j['meshes'][n['mesh']]['primitives']:
            P = arr(prim['attributes']['POSITION']).astype(np.float64) + T
            I = arr(prim['indices']).astype(np.int64).reshape(-1, 3)
            tris.append(P[I])
        parts[n['name']] = np.concatenate(tris)
    return parts

def disp(parts): return {k: v @ M.T for k, v in parts.items()}
def doff(off): return {k: np.asarray(v, float) @ M.T for k, v in off.items()}
def mirror_x(o): return [-o[0], o[1], o[2]] if False else o   # offsets in the JSON are already raw glTF
