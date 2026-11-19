import numpy as np
M=np.array([[0,-1,0],[0,0,-1],[-1,0,0.]])   # display = M @ raw (matches the Unity view: up = -z raw)
VIEW=(-30,22)                                # yaw, pitch of the user's reference screenshot
def disp(parts): return {k:v@M.T for k,v in parts.items()}
def doff(off): return {k:np.asarray(v)@M.T for k,v in off.items()}
