# IETM inside the HELIOS app (one scene)

The Landing Gear IETM opens from the HELIOS side menu (**IETM**). There's only **one scene**, `Assets/HELIOS/Scenes/HELIOS.unity`. The IETM objects are inside it, under one parent named **IETM Module**, which is **inactive (hidden)** in the scene. At Play it's shown on the HELIOS IETM page and hidden on every other page.

## What changed in the HELIOS project

| Path | Change |
|---|---|
| `Assets/HELIOS/Scenes/HELIOS.unity` | **Only additions**: the `IETM Module` object tree and one entry in the scene root list. No existing HELIOS object or value was changed. |
| `Assets/IETM/` | The IETM module: scripts (namespace `IETM`), shaders, UI art, data (`Resources/IETM/*.json`), and the stand-alone scene `Scenes/LandingGear_IETM.unity` |
| `Assets/IETM/Integration/HeliosIetmBridge.cs` | Shows and hides `IETM Module` with the HELIOS IETM page |
| `Assets/IETM/Integration/Editor/HeliosIetmIntegrationMenu.cs` | Menu **IETM > HELIOS Integration** |
| `Assets/3DModels/Landing_Gear 1.glb` + `Materials/` | Animated F-16 nose landing gear |
| `Assets/StreamingAssets/IETM/32-20-00_Fig1_IPC.pdf` | Printable IPC |
| `Tools/`, `.gitignore` | PDF generator (optional), Unity ignore list |

No HELIOS script, prefab, project setting or package was changed. No new packages are needed.

## Hierarchy

```
HELIOS.unity
  EventSystem, HELIOS, Directional Light, Main Camera, HELIOS UI   (HELIOS, unchanged)
  IETM Module   (inactive)
    IETM Camera   (untagged, audio listener off: HELIOS keeps the main camera)
    Key Light
    Landing Gear
    IETM App      (IETMApp + HeliosShell)
    IETM Canvas   (IETM pages, panels, breadcrumb)
```
To edit the IETM in the HELIOS scene, tick **IETM Module** active, edit, then untick it again before saving. The bridge hides it at Play anyway.

## How to run

1. Open `Assets/HELIOS/Scenes/HELIOS.unity` and press **Play**. Only this scene appears in the Hierarchy.
2. Click **IETM** in the HELIOS side menu: aircraft systems → **32 Landing Gear** → sections → the Landing Gear IETM.
3. Click any other HELIOS menu item to go back. The IETM keeps its state. Clicking **IETM** again while it's open goes back to the IETM home.

## What the bridge does at Play

`HeliosIetmBridge` creates itself (`RuntimeInitializeOnLoadMethod`) and listens to `HeliosNavigator.onPageOpened`:

- **IETM page:** switches `IETM Module` on and fits the IETM pages, breadcrumb and frame into the HELIOS content area, right of the HELIOS menu and below the HELIOS top bar. It hides the IETM's own copy of the HELIOS top bar, menu and backdrop, puts the IETM canvas just below the HELIOS UI canvas (HELIOS popups stay on top), and pauses the HELIOS aircraft, lights and double-click isolate. It also uses the IETM ambient light.
- **Any other page:** switches `IETM Module` off and restores all of the above.
- IETM buttons that pointed to its own "3D View" open the HELIOS **3D View** page.

## Updating the IETM

Keep editing the IETM in `Assets/IETM/Scenes/LandingGear_IETM.unity` (stand-alone, as before), then run **IETM > HELIOS Integration > Merge IETM Into HELIOS Scene**. It replaces `IETM Module` in HELIOS with a fresh copy and saves the HELIOS scene. Content-only changes (JSON in `Resources/IETM`) need no merge.

## Removing the IETM

Delete `IETM Module` from the HELIOS scene, then delete `Assets/IETM`, `Assets/3DModels/Landing_Gear 1.glb` (+ `Materials`) and `Assets/StreamingAssets/IETM`.
