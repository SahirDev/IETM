# Rebuild the IPC PDF.  Run from anywhere:  python Tools/ipc_pdf/make.py [--explode]
#   --explode  also re-solves the collision-free exploded layout and writes it into landing_gear_ietm.json
import os, sys, subprocess
here=os.path.dirname(os.path.abspath(__file__)); py=sys.executable
work='/home/claude/ipc'; os.makedirs(work,exist_ok=True); os.chdir(work)
if '--explode' in sys.argv:
    subprocess.check_call([py, os.path.join(here,'explode.py')])
    print('explode.json written - copy the offsets into the "explode" block of landing_gear_ietm.json')
subprocess.check_call([py, os.path.join(here,'make_figs.py')])
subprocess.check_call([py, os.path.join(here,'build_pdf.py'), 'out.pdf'])
