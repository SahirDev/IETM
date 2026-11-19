# IPC PDF + exploded-view generator

* `explode.py` - solves the **mechanically laid-out exploded view** of `Landing_Gear 1.glb` (extended pose).
  Each part slides along its own assembly axis (pin axis, strut axis, screw axis, wheel axle) from its parent's
  exploded position until it clears every placed part by 6 mm and is fully off the parts it mates with
  (collars off the strut, nut off the screw, wheel off the axle, pins out of their clevises). The resulting
  offsets live in the `"explode"` block of `landing_gear_ietm.json` and drive both the Unity explode slider
  and the PDF.
* `make_figs.py` + `build_pdf.py` - render the five figure sheets (same camera angle as the IETM viewer)
  and assemble `Assets/StreamingAssets/IETM/32-20-00_Fig1_IPC.pdf`.

Python 3 with numpy, opencv-python, pillow, reportlab. Paths to the repo files are at the top of the scripts
(`/home/claude/ietm/...`) - change them to your checkout. Run `python Tools/ipc_pdf/make.py`.
