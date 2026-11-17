# IPC PDF generator

Builds `Assets/StreamingAssets/IETM/32-20-00_Fig1_IPC.pdf` (ATA 2200 style Illustrated Parts Catalog)
from `Assets/3DModels/Landing_Gear 1.glb` (line-art renders, extended pose) and
`Assets/IETM/Resources/IETM/landing_gear_ietm.json` (parts list).

Needs Python 3 with numpy, opencv-python, pillow, reportlab. Paths at the top of the scripts point to the
repo; adjust if needed. Run `figures.py` sheet generation (see `make.py`) then `build_pdf.py <out.pdf>`.
