# python Tools/ipc_pdf/make.py   (run from repo root)
import os, sys, json, subprocess
here=os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0,here)
from figures import *
os.makedirs('/home/claude/ipc',exist_ok=True); os.chdir('/home/claude/ipc')
leaves=[it['itemNo'] for it in items if not children.get(it['itemNo'])]
figs=[('Sheet 1', figure(['010','020','030','040','100','160','200'], view=(35,20), out='sheet1.png')),
      ('Sheet 2', figure(leaves, view=(35,20), offsets=explode(allnames,0.55), out='sheet2.png'))]
for nos,grp,view,k,out in [(['050','060','070','080','090'],'040',(-35,15),0.35,'sheet3.png'),
                           (['110','115','120','130','140','150','170','180','190'],['100','160'],(125,20),0.30,'sheet4.png'),
                           (['210','220','230'],'200',(-60,15),0.45,'sheet5.png')]:
    sel=sum((nodes_of(g) for g in (grp if isinstance(grp,list) else [grp])),[])
    figs.append((out, figure(nos, view=view, offsets=explode(sel,k), only=sel, ghost=[n for n in allnames if n not in sel], out=out)))
json.dump([[n,f] for n,f in figs],open('figs.json','w'),indent=1,default=float)
subprocess.check_call([sys.executable, os.path.join(here,'build_pdf.py'), 'out.pdf'])
