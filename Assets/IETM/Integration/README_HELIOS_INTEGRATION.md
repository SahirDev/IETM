# IETM inside the HELIOS app (one scene)

The Landing Gear IETM opens from the HELIOS side menu (**IETM**). There's only **one scene**, `Assets/HELIOS/Scenes/HELIOS.unity`. The IETM objects are inside it, under one parent named **IETM Module**, which is **inactive (hidden)** in the scene. At Play it's shown on the HELIOS IETM page and hidden on every other page.

## What changed in the HELIOS project

| Path | Change |
|---|---|
| `Assets/HELIOS/Scenes/HELIOS.unity` | **Only additions**: the `IETM Module` object tree and one entry in the scene root list. No existing HELIOS object or value was changed. |
| `Assets/HELIOS/Resources/HELIOS UI.prefab` | **Only an addition**: `Backdrop > Blue Background` (Image, full screen, `Resources/IETM/ViewportBackground.png`). Note: if the HELIOS UI is regenerated from the HELIOS builder menu, add it again. |
| `Assets/HELIOS/Resources/HELIOS UI.prefab` → `Top Bar` | **Top bar refined to the Figma header** (node 956:6585, scaled 0.8125 to the 36 px bar): bigger logo (preserve aspect), new `Title` text "F-16 Digital Twin" (bold 14), bell 30, Light pill 66×29, divider 24, user chip 130×36 with 28 px avatar + left-aligned name/role, sign-out 29. Same icons/sprites, bar height unchanged so Menu/Cards/3D view don't move. Regenerating from the HELIOS builder will undo this. |
| `HELIOS UI.prefab` → `Page Asset Integrity / Integrity Status` | Legend re-anchored to the **top-right**, 12 px left of the card column (anchor/pivot 1,1, pos -338,-46). It used a fixed X from the left, so on screens narrower than 1568 it slid under the Integrity card. |
| `Assets/HELIOS/Scenes/HELIOS.unity` lighting | Additions: `Main Camera > Camera Key Light` (0.85, follows the orbit so the side you look at is lit) and root `HELIOS Aircraft Lights` (Top Light 0.45, cool Rim Light 0.6). Ambient switched Skybox → **Trilight** (brighter sky/equator/ground). Existing Directional Light untouched. Tweak intensities in the Inspector; the IETM bridge switches these off inside IETM and restores the HELIOS ambient. |
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

## Procedure player (Operation / Disassembly / Assembly)

`Assets/IETM/Scripts/Runtime/IETMApp.Player.cs` (icons: `Assets/IETM/Resources/IETM/Player`).

- Opening **Operation**, **Disassembly** or **Assembly** collapses both IETM side panels (the arrows still reopen them); leaving the module restores them.
- A music-player bar at the **bottom-right** of the 3D view (shown only while the right panel is collapsed; opening that panel hides it, since the panel shows the same steps): **Previous · Pause/Continue · Replay step · Next**, with the step's title, warning/caution and instruction text above the buttons, plus a progress line.
- Keys: ← / → steps, **Space** = pause / continue inside these modules (gear toggle elsewhere).
- While a part moves in **Disassembly / Assembly**, its name label (same style as mouse-over) follows it and fades out shortly after it stops. In **Operation** names show on mouse-over only.
- While the player is shown, the 3D toolbar (Fit, Isolate, Hide, Explode, Spin) moves to the bottom-left and the player sits bottom-right (above the toolbar on narrow screens). **Disassembly / Assembly** hide the Extended / Retracted switch. All restored when you leave.
- Settings on **IETM App → Procedure player**: collapse panels on/off, player width, label linger time.
- The bar is created from code when the IETM starts, so it appears in Play mode only.

## Light / Dark mode and notifications

`Assets/IETM/Integration/HeliosThemeMode.cs` (created automatically in the HELIOS scene, no setup).

- **Light** button in the top bar switches the whole application (HELIOS pages, menu, cards and the IETM) to a white / grey theme; it then reads **Dark** to switch back. The choice is remembered for the next start.
- Colours are converted from the ones already used (dark surfaces → white / light grey, light text → dark grey, accents keep their hue), so no HELIOS theme or prefab colour was changed. Dark mode restores the exact original colours. Photos, logos and white glyphs on coloured buttons stay as they are; the blue backgrounds become light grey (**Light View Background** on the *HELIOS Theme Mode* object). Buttons tinted by their colour block (IETM buttons) get light fills with dark labels; the full-screen HELIOS **Vignette** is hidden (it would veil the screen). The **top bar** (and the bell popup) keeps its dark design in both modes; only the area below it changes.
- **Bell**: the red badge is switched off in the HELIOS UI prefab (`Top Bar > Notifications > Badge` inactive). Clicking the bell shows "No new notifications" under it for 3 seconds.

## Built in the Editor (nothing created at Play)

Everything this integration adds is a real object in the scene / prefab, visible and editable before Play:

| Object | Where |
|---|---|
| **IETM Integration** (HeliosIetmBridge + HeliosThemeMode) | HELIOS scene, root |
| **Procedure Player** (instruction + Previous / Pause / Replay / Next) | IETM Module > IETM Canvas > … > viewport overlay (inactive until a procedure opens) |
| **Moving Part Label 1-4 / Moving Part Dot 1-4** | same overlay (inactive) |
| **IETM Viewport Background** (camera > canvas > blue image) | IETM Module |
| RectMask2D on the IETM frame | IETM Module > HELIOS Shell > IETM frame |
| **Notifications Popup** | HELIOS UI prefab > UI (inactive) |

At Play the scripts only find and connect them (button listeners). The code still creates any of them if missing
(stand-alone IETM scene, or after *Merge IETM Into HELIOS Scene* rebuilds the module).

**Warm-up**: the IETM starts hidden while HELIOS loads (`IETM Integration > Helios Ietm Bridge > Prewarm`), so its data and
lists are ready before the first click on IETM — no wait on the first open in WebGL. The landing gear model, its data
file and all textures are scene references (loaded with the scene, nothing loaded from Resources at Play).
