# IETM inside the HELIOS app

The Landing Gear IETM opens from the HELIOS side menu (**IETM**). The HELIOS UI, its scripts, its prefab and its scene are not changed: everything is in new files.

## What was added (new files only)

| Path | What it is |
|---|---|
| `Assets/IETM/` | The IETM module: scripts (namespace `IETM`), shaders, UI art, data (`Resources/IETM/*.json`), scene `Scenes/LandingGear_IETM.unity` |
| `Assets/IETM/Integration/HeliosIetmBridge.cs` | Connects the IETM to the HELIOS IETM page (see below) |
| `Assets/IETM/Integration/Editor/HeliosIetmBuildHook.cs` | Adds the IETM scene to player builds, plus the **IETM > HELIOS Integration** menu |
| `Assets/3DModels/Landing_Gear 1.glb` + `Materials/` | The animated F-16 nose landing gear |
| `Assets/StreamingAssets/IETM/32-20-00_Fig1_IPC.pdf` | Printable IPC (PDF button in the IETM) |
| `Tools/` | Python tools that regenerate the IPC PDF and illustrations (optional) |
| `.gitignore` | Standard Unity ignore list (Library, Temp, Logs…) |

Packages: no new packages are needed. The project already has glTFast, URP 17.3, the Input System and uGUI.

## How to run

1. Open `Assets/HELIOS/Scenes/HELIOS.unity` (or use **IETM > HELIOS Integration > Open HELIOS Scene**).
2. Press **Play**.
3. Click **IETM** in the HELIOS side menu:
   aircraft systems → **32 Landing Gear** → sections → the Landing Gear IETM (publication tree, 3D, IPD, procedures).
4. Click any other HELIOS menu item (3D View, Sensors…) to go back. The IETM keeps its state. Clicking **IETM** again while it's open goes back to the IETM home.

## How the bridge works

`HeliosIetmBridge` creates itself at Play (`RuntimeInitializeOnLoadMethod`), so no object was added to the HELIOS scene. It does the following:

- Listens to `HeliosNavigator.onPageOpened`. The HELIOS **IETM** page already exists and is empty, with no model and no toolbar.
- The first time IETM is opened, it loads `LandingGear_IETM.unity` **additively**.
- Fits the IETM pages, breadcrumb and frame into the HELIOS content area: right of the HELIOS menu and below the HELIOS top bar, with a 16 px gap. The layout follows window resizing.
- Hides the IETM's own copy of the HELIOS top bar, side menu and backdrop. The real HELIOS ones stay visible and clickable on top.
- Shares the screen safely: keeps one EventSystem and one AudioListener (the HELIOS ones). The IETM camera is untagged, so `Camera.main` stays the HELIOS camera. The IETM canvas sits just below the HELIOS UI canvas, so HELIOS popups appear above it.
- While the IETM is open, it pauses the HELIOS aircraft (hidden), the HELIOS lights and `IsolateOnDoubleClick`, and swaps the global ambient light. All of these are restored when another HELIOS page opens.
- IETM buttons that pointed to its own "3D View" now open the HELIOS **3D View** page.

## Builds (WebGL / desktop)

When you build from the Build window, the IETM scene is appended to that build automatically. The Build Settings file isn't changed. If you build with a custom script, or prefer it listed permanently, run **IETM > HELIOS Integration > Add IETM Scene To Build** once. It's appended after the HELIOS scene, which stays first.

## Removing the IETM

Delete `Assets/IETM`, `Assets/3DModels/Landing_Gear 1.glb` (+ `Materials`) and `Assets/StreamingAssets/IETM`. HELIOS goes back to its empty IETM page.

## Editing the IETM

See `Assets/IETM/README.md`. Content is in `Assets/IETM/Resources/IETM/landing_gear_ietm.json` (IETM) and `helios_shell.json` (system and section cards).
