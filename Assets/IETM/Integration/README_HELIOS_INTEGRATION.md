# IETM inside the HELIOS app (one scene)

The Landing Gear IETM opens from the HELIOS side menu (**IETM**). There's only **one scene**, `Assets/HELIOS/Scenes/HELIOS.unity`. The IETM objects are inside it, under one parent named **IETM Module**, which is **inactive (hidden)** in the scene. At Play it's shown on the HELIOS IETM page and hidden on every other page.

## What changed in the HELIOS project

| Path | Change |
|---|---|
| `Assets/HELIOS/Scenes/HELIOS.unity` | **Only additions**: the `IETM Module` object tree and one entry in the scene root list. No existing HELIOS object or value was changed. |
| `Assets/HELIOS/Resources/HELIOS UI.prefab` | **Only an addition**: `Backdrop > Blue Background` (Image, full screen, `Resources/IETM/ViewportBackground.png`). Note: if the HELIOS UI is regenerated from the HELIOS builder menu, add it again. |
| `Assets/HELIOS/Resources/HELIOS UI.prefab` → `Top Bar` | **Top bar refined to the Figma header** (node 956:6585, scaled 0.8125 to the 36 px bar): bigger logo (preserve aspect), new `Title` text "F-16 Digital Twin" (bold 14), bell 30, Light pill 66×29, divider 24, user chip 130×36 with 28 px avatar + left-aligned name/role, sign-out 29. Same icons/sprites, bar height unchanged so Menu/Cards/3D view don't move. Regenerating from the HELIOS builder will undo this. |
| `Assets/HELIOS/Scripts/HeliosInput.cs` | One line: mouse-wheel scroll normalised (the 3D View zoom was about 120× too slow with Input System 1.20). |
| `Assets/IETM/` | The IETM module: scripts (namespace `IETM`), shaders, UI art, data (`Resources/IETM/*.json`), and the stand-alone scene `Scenes/LandingGear_IETM.unity` |
| `Assets/IETM/Integration/HeliosIetmBridge.cs` | Shows and hides `IETM Module` with the HELIOS IETM page |
| `Assets/IETM/Integration/Editor/HeliosIetmIntegrationMenu.cs` | Menu **IETM > HELIOS Integration** |
| `Assets/3DModels/Landing_Gear 1.glb` + `Materials/` | Animated F-16 nose landing gear |
| `Assets/StreamingAssets/IETM/32-20-00_Fig1_IPC.pdf` | Printable IPC |
| `Tools/`, `.gitignore` | PDF generator (optional), Unity ignore list |

Apart from the rows above (and WebGL-only player settings, see `Assets/IETM/WebGL/WEBGL_BUILD.md`), no HELIOS script, prefab, project setting or package was changed. No new packages are needed.

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
3. Click any other HELIOS menu item to go back. **Every click on IETM starts the IETM workflow from the top**: the aircraft systems cards, with the IETM reset to its start state (title page, all parts back, gear extended, no explode, isolate, x-ray or selection, home camera). It doesn't resume where you left it.

## What the bridge does at Play

`HeliosIetmBridge` creates itself (`RuntimeInitializeOnLoadMethod`) and listens to `HeliosNavigator.onPageOpened`:

- **IETM page:** switches `IETM Module` on and fits the IETM pages, breadcrumb and frame into the HELIOS content area, right of the HELIOS menu and below the HELIOS top bar. It hides the IETM's own copy of the HELIOS top bar, menu and backdrop, puts the IETM canvas just below the HELIOS UI canvas (HELIOS popups stay on top), and pauses the HELIOS aircraft, lights and double-click isolate. It also uses the IETM ambient light.
- **IETM publication open** (the last breadcrumb level, e.g. *IETM > F-16C > 32 Landing Gear > 32-20 Nose landing gear*): the HELIOS side menu shows **icons only**, so the IETM gets the extra width. On the IETM aircraft and system card pages, and on every other HELIOS page, the menu is shown in full as designed. This is runtime only (the HELIOS prefab isn't changed). To turn it off, untick **Icon Menu In Ietm** on the `IETM - HELIOS Bridge` object at Play, or set `iconMenuInIetm = false` in `HeliosIetmBridge.cs`.
- **Any other page:** switches `IETM Module` off and restores all of the above.
- **Whole screen, every page:** the blue background is the **Blue Background** image in the HELIOS UI prefab (`Backdrop > Blue Background`, last child, over Base / Glow). It's visible in the Editor. Untick it to see the original HELIOS glow.
- IETM buttons that pointed to its own "3D View" open the HELIOS **3D View** page.

## Updating the IETM

Keep editing the IETM in `Assets/IETM/Scenes/LandingGear_IETM.unity` (stand-alone, as before), then run **IETM > HELIOS Integration > Merge IETM Into HELIOS Scene**. It replaces `IETM Module` in HELIOS with a fresh copy and saves the HELIOS scene. Content-only changes (JSON in `Resources/IETM`) need no merge.

## Removing the IETM

Delete `IETM Module` from the HELIOS scene, then delete `Assets/IETM`, `Assets/3DModels/Landing_Gear 1.glb` (+ `Materials`) and `Assets/StreamingAssets/IETM`.
