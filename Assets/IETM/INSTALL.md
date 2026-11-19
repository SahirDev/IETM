# Installing the HELIOS IETM (F-16 Nose Landing Gear) in another Unity project

## 1. Requirements in the target project
| Item | Value |
|---|---|
| Unity | **6000.3.x** (made with 6000.3.22f1) |
| Render pipeline | **URP 17.3** (`com.unity.render-pipelines.universal`), a URP asset assigned in Graphics + Quality settings |
| Input | **Input System** package (`com.unity.inputsystem` 1.20+); *Player > Active Input Handling* = **Input System Package (New)** or **Both** |
| glTF import | **glTFast 6.18.0** (`com.unity.cloud.gltfast`) - imports `Landing_Gear 1.glb` and its animation |
| UI | uGUI (`com.unity.ugui`, default) |

Add the packages in *Window > Package Manager > + > Add package by name*, or add to `Packages/manifest.json`:
```json
"com.unity.cloud.gltfast": "6.18.0",
"com.unity.inputsystem": "1.20.0",
"com.unity.render-pipelines.universal": "17.3.0"
```
**Install the packages BEFORE importing the .unitypackage**, otherwise the GLB imports as an unknown file.

## 2. Import
*Assets > Import Package > Custom Package...* > `HELIOS_IETM_LandingGear.unitypackage` > Import all.
It brings:
- `Assets/IETM/` - scripts (namespace `IETM`), shaders, UI images and icons, data (`Resources/IETM/*.json`), scene `Scenes/LandingGear_IETM.unity`
- `Assets/3DModels/Landing_Gear 1.glb`
- `Assets/StreamingAssets/IETM/32-20-00_Fig1_IPC.pdf`

## 3. Open and check
1. Open `Assets/IETM/Scenes/LandingGear_IETM.unity`, press Play.
2. If the UI is missing or wrong: *IETM > Rebuild UI*, then *IETM > HELIOS Shell > Build Shell*.
3. If the model is missing: *IETM > Use Animated GLB Model*.
4. Pink materials = the project has no URP asset assigned (step 1).

## 4. Using it inside the HELIOS app
- Run it as its own scene: add `LandingGear_IETM` to *Build Settings* and load it from the HELIOS **IETM** menu button:
  `SceneManager.LoadSceneAsync("LandingGear_IETM", LoadSceneMode.Additive)` (unload it to close).
- The scene has its own Camera, EventSystem and Canvas. If the HELIOS scene already has an EventSystem, disable the one in the IETM scene when loading it additively.
- The HELIOS top bar / side menu inside the IETM scene can be removed with *IETM > HELIOS Shell > Remove Shell* if the HELIOS app shows its own.

## 5. Editing content
- Text, parts list, procedures, layouts: `Assets/IETM/Resources/IETM/landing_gear_ietm.json`
- HELIOS pages (systems, sections, cards): `Assets/IETM/Resources/IETM/helios_shell.json`
- Part positions (explode / disassembly / L-path corners): *IETM > Layout Editor*
- Full documentation: `Assets/IETM/README.md`
