# IETM inside the HELIOS app (one scene)

The Landing Gear IETM opens from the HELIOS side menu (**IETM**). There's only **one scene**, `Assets/HELIOS/Scenes/HELIOS.unity`. The IETM objects are inside it, under one parent named **IETM Module**, which is **inactive (hidden)** in the scene. At Play it's shown on the HELIOS IETM page and hidden on every other page.

## What changed in the HELIOS project

| Path | Change |
|---|---|
| `Assets/HELIOS/Scenes/HELIOS.unity` | **Only additions**: the `IETM Module` object tree and one entry in the scene root list. No existing HELIOS object or value was changed. |
| `Assets/HELIOS/Resources/HELIOS UI.prefab` | **Only an addition**: `Backdrop > Blue Background` (Image, full screen, `Resources/IETM/ViewportBackground.png`). Note: if the HELIOS UI is regenerated from the HELIOS builder menu, add it again. |
| `Assets/HELIOS/Resources/HELIOS UI.prefab` → `Top Bar` | **Top bar refined to the Figma header** (node 956:6585, scaled 0.8125 to the 36 px bar): bigger logo (preserve aspect), new `Title` text "HELIOS – Asset Integrity Digital Twin" (bold 14, 320 wide), bell 30, Light pill 66×29, divider 24, user chip 130×36 with 28 px avatar + left-aligned name/role, sign-out 29. Same icons/sprites, bar height unchanged so Menu/Cards/3D view don't move. Regenerating from the HELIOS builder will undo this. |
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

- **Light** button in the top bar switches the whole application (HELIOS pages, menu, cards and the IETM) to a white / grey theme; it then reads **Dark** to switch back. The app always starts in **Dark** mode (tick *Remember Last Mode* on *IETM Integration* to start in the mode used last time).
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

## 3D View page: Figma View Tools + Telemetry Strip

Added in `HELIOS UI.prefab` (page **3D View**), visible in the Editor:

- **View Tools** (right edge, Figma 56 x 402): Select, Fit View, Zoom, Rotate, Pan | Opaque, Wireframe, Transparent | **Cross Section**.
  Icons are white and follow the theme (HeliosThemed, Text role); Zoom / Rotate / Pan light up while left-drag does that.
  Buttons call HeliosActions (CameraTool 0 rotate, 1 zoom, 2 pan, 3 fit; ShowOriginalModel, ToggleWireframe, ShowHologram).
- **Cross Section** (`Assets/HELIOS/Scripts/HeliosSection.cs`, shader `Assets/HELIOS/Shaders/HeliosSection.shader`): the last button
  cuts the aircraft with a plane. *Cross Section Controls* (next to it): **X / Y / Z** = wing-to-wing, top-to-bottom, nose-to-tail
  plane; the slider moves the plane through the aircraft and the cut follows it; **Flip** keeps the other half. The faces the cut
  exposes are drawn in *Section Colour* (cyan), a faint plane shows the cut. Turning it off (or leaving the page) restores the model.
- **Telemetry Strip** (bottom, Figma 953 x 70): Aircraft Health, Asset Integrity, Maintenance, Open Findings; each opens its page.
- Sizes, positions and colours measured on the Figma frame and converted x 744/1300 (same share of the screen as in Figma).
  Icons cut from the Figma frame: `Assets/HELIOS/Art/Icons/figma_*.png`.
- The bottom camera toolbar is off on this page (Figma has the strip there); the Total Flight Hours card moved left of the View Tools.

## Engine IETM (System 71-80 > 72-00 Engine - turbine module)

The IETM App now holds several **publications**. The landing gear (Data File + model on IETM App) is the default; the
engine is listed under **IETM App > Publications** (`engine`, `Resources/IETM/engine_ietm.json`, the scene object
`IETM Module/.../Engine` made from `Assets/Model/engine.glb`). Opening any data module of a publication switches to it:
the other model is hidden, and the parts list, structure tree, callouts, publication tree and order list are rebuilt
from its data. Gear features (Extended / Retracted switch, gear animation, "Gear:" in the status bar) only exist in
the landing gear. Resetting the session (leaving the IETM page) goes back to the landing gear.

Shell: the Aircraft page shows **Engine** as available; its system page lists 71-00 ... 77-00 with **72-00 Engine -
turbine module** available ("Open IETM" opens `DM-ENG-TITLE`).

Data modules (same kinds as the landing gear):

| Group | Module | Type |
|---|---|---|
| 00 Introduction | DM-ENG-TITLE Title page | description |
| 72-00 Description and operation | DM-ENG-DESC Description (paragraphs isolate their parts) | description |
| | DM-ENG-OPER Operation (gas path), rotors spin (`animate: "spin"`) | procedure |
| 72-00 Maintenance | DM-ENG-DISASSY Disassembly (11 parts laid out, no overlap) | procedure |
| | DM-ENG-ASSY Assembly (reverse order) | procedure |
| | DM-ENG-BSI Borescope inspection (isolate per section) | procedure |
| 72-00 Illustrated parts data | DM-ENG-IPD IPD (14 items: case, cone, mount, lines, bolts, compressor, hot section) | ipd |

All data is **SAMPLE DATA**. `Tools/make_engine_ietm.py` regenerates the JSON (items, steps and the explode / removal
offsets, glTF space of `turbine_01_obj`: +X aft, -Z up), so edit it there and run `python3 Tools/make_engine_ietm.py`.
A printable IPC can be set per publication (`Ipd Pdf File`, empty = none yet).

**Edit-time set-up (nothing at Play):** `HeliosEngineSetup` runs once by itself when HELIOS.unity is open and the
engine is not registered: places the engine (sized like the gear, inactive, mesh colliders baked), registers the
publication, rebuilds the shell pages from `helios_shell.json` (no theme / layout change) and saves the scene. Run it
again from **IETM > HELIOS Integration > Set Up Engine IETM** after changing `helios_shell.json`. At start-up the
engine's data and parts are bound while the IETM prewarms, so opening it later is instant.

### Engine exploded view and disassembly layout (Layout Editor)

**IETM > Layout Editor** has a **Publication** picker (Landing gear / engine). Pick *engine*, the tab (*Explode layout*
= Explode slider at 100 %, *Disassembly layout* = where parts are laid out, *L-path corner*), press **Edit**: the engine
is shown (the landing gear hidden) with every part at its saved position. Move parts with the Move tool (W), **Save**
writes them into `engine_ietm.json`; **Cancel** restores everything (the engine goes hidden again). At Play the
Explode slider moves every part from assembled to its saved position. `Tools/make_engine_ietm.py` keeps the saved
layouts when it is re-run (`--reset-layout` to go back to the generated ones).

**Engine layout = explode view.** The engine's exploded view (Explode slider at 100 %) uses the same positions as the
disassembly layout: saving the *Disassembly layout* for the engine in the Layout Editor writes both. Parts move
straight (no L-path corners). Disassembly order: exhaust cone (hull_turbine), bolts (screws_turbine_001), compressor
disks (plates_turbine_016), external lines (pipes_turbine_009), support ring (mount_turbine_014), liner
(grid_turbine_007), combustor case (canister_turbine_011), turbine rotor (blades_turbine_003.001), fan
(blades_turbine_003), inlet guide vanes (blades_dividers_turbine_017), outer case (hull_turbine_004, stays in place);
assembly is the reverse. The Explode slider is reset when a disassembly / assembly step runs.

**Engine printable IPC (PDF).** `Assets/StreamingAssets/IETM/72-00-00_Fig1_IPC.pdf`, laid out like the landing gear
IPC (7 pages): title page, Figure 1 sheets 1-5 (assembly, exploded view = the IETM explode / disassembly layout,
Detail A compressor, Detail B combustion and turbine, Detail C case, mounts and external lines; line-art rendered from
`engine.glb`, callouts placed automatically) and the parts list. Opened from the engine title page card, the IPD's
printable-IPC button (IETM App > Publications > engine > Ipd Pdf File) and the shell's Related list. Rebuild with
`python Tools/ipc_pdf/engine/build_engine_pdf.py` (numpy, opencv-python, pillow, reportlab) - re-run it after moving
parts in the Layout Editor so the exploded sheet follows. Detail layouts and views are at the end of `engine_figs.py`.
After editing `helios_shell.json`, run IETM > HELIOS Integration > Set Up Engine IETM to rebuild the shell pages.

**"Part Catalog" wording.** The printable IPC files stay PDF; the UI calls them "Part Catalog": the IPD header button
(126 px; button row 304 px, figure title / ICN inset -324), the title page card button "Download Part Catalog", the
shell tag chip and Related links ("Part Catalog · 32-20-00 Fig 1" / "72-00-00 Fig 1") and the engine's publication tree
entry ("Part Catalog - 72-00-00 Fig 1", tag IPC). The IPD header (figure title and ICN) now follows the open publication.

## 3D View page: Aircraft Hierarchy, card and tool strip (Figma 956:5442)

All sizes are Figma x 0.8125 (the HELIOS canvas scale, same as the top bar and the menu). Built in
`HELIOS UI.prefab` by `Tools/helios_ui/hierarchy_panel.py` (edit time, nothing created at Play):
* **Menu > Shadow off**: its 9-slice seam drew a thin line on both sides of the menu at the 3D View item.
* **Flight Hours card** (Total Flight Hours / Next Service Due): Figma 200 x 131 -> 162.5 wide, rows 35.75, tile 30,
  label 10 / value 15. Cards moved to 61.75 from the right edge.
* **View Tools** strip: Figma 56 x 402, 38 px buttons -> 45.5 x 326.6, buttons 30.9 (icons scaled with them),
  8.1 from the right edge; the Cross Section controls follow the Cross Section button.
* **Aircraft Hierarchy** panel right of the menu (Figma 266 wide -> 216), built from the real F-16 model by
  `Tools/helios_ui/hierarchy_from_model.py`. Part names in `F_16_Updated_Metadata 4.glb` are
  `F16_<Region>_Region_<Left|Center|Right>_Part_NNNN`, so the tree is F-16 (2112 parts) > Fuselage (Nose, Forward /
  Mid / Aft Fuselage, Underside) / Wing (Left, Right) / Empennage (Vertical, Horizontal Tail), regions split Left /
  Center / Right, with the real part counts.

### Editing the Aircraft Hierarchy (Editor)
The rows are normal objects in `HELIOS UI.prefab` (UI > Pages > 3D View > Aircraft Hierarchy > Tree > Content):
rename a row with its **Label** text, reorder / duplicate rows, and set which parts a row stands for on its
**Row > HeliosTreeNode > Part Names** (part name prefixes, e.g. `F16_Wing_Region_Left_`; empty = its child rows'
parts). **Expanded** = children shown when the page opens (switch the **Children** object on to see them while
editing). The count badges are updated at Play with the number of parts each row found.

### Using it (Play)
* click a row: its parts are outlined and the camera frames them; click it again (or the F-16 row) = whole aircraft
* chevron: expand / collapse; eye: hide / show the row's parts (hidden parts cannot be clicked; their wireframe and
  hologram hide too)
* click a part in the 3D view: its row is selected in the tree (parents expand)

## Nothing loaded at Play: aircraft, background camera, sensor focus in the scene

`Assets/HELIOS/Editor/HeliosSceneSetup.cs` runs once by itself when HELIOS.unity is open (and from
**Tools > HELIOS > Place Aircraft In Scene**):
* the F-16 (`F_16_Updated_Metadata 4.glb`, HELIOS App > Aircraft Model) is placed in the scene under
  **HELIOS > HELIOS Model**, centred and scaled to `HeliosModel.Size` exactly as the app did at Play, and Aircraft Model
  points to that scene object. `HeliosModel.Build` uses a placed model as it is (no instantiate / move / scale).
* **HELIOS Background Camera** (backdrop behind the model) is a scene object, set on HELIOS App > Background Camera.
* **Sensor Focus** is on the UI object (was added at Play).
The HELIOS UI, the IETM (landing gear and engine models) were already scene objects. Holograms and wireframe overlays
are still made the first time they are switched on (only then, not at start).

### Old UI in the Game view? (HELIOS UI.prefab regenerated)
`HeliosMenu` used to rebuild the default UI when it could not load `HELIOS UI.prefab` at editor start - which happens
right after a pull, before the prefab is imported - and saved it over the project's prefab (old menu, old card, the
Rotate / Zoom / Pan bar, no Aircraft Hierarchy / View Tools). It now never replaces an existing prefab file.
`HeliosSceneSetup` checks the prefab when HELIOS.unity opens and offers to restore it from git
(**Tools > HELIOS > Restore UI Prefab From Git** = `git checkout HEAD -- "Assets/HELIOS/Resources/HELIOS UI.prefab"`).
The setup also puts the Main Camera at the app's home view (Game view = what Play shows;
**Tools > HELIOS > Frame Aircraft In Game View**) and removes Unity's default Directional Light, which doubled the
HELIOS light rig (the aircraft was over-lit).

## Live UI prefab and the F16_V01 model (current set-up)
* The project's UI is **`Assets/HELIOS/Prefabs/HELIOS UI.prefab`** (own GUID). `Assets/HELIOS/Resources/HELIOS UI.prefab`
  is the default UI that Tools > HELIOS > Build UI writes; it is no longer used by the scene, so regenerating it can
  never replace the project's UI again. HeliosMenu no longer builds anything automatically.
* `HeliosSceneSetup` (runs when HELIOS.unity opens; **Tools > HELIOS > Set Up Scene**) makes the scene's UI an instance
  of that prefab (removes any other HELIOS UI, sets HELIOS App > UI), places **`Assets/3DModels/F16_V01.glb`** under
  HELIOS > HELIOS Model (removes the previously placed F-16), keeps the background camera / Sensor Focus, frames the
  Main Camera, removes the duplicate Directional Light, and saves the scene.
* The Aircraft Hierarchy is built from F16_V01 (1813 parts: Cockpit + `F16_<Region>_Region_<Side>_Part_NNNN`):
  F-16 > Fuselage (Cockpit, Nose, Forward / Mid / Aft Fuselage, Underside), Wing (Left / Right), Empennage (Vertical /
  Horizontal Tail). Rebuild with `python Tools/helios_ui/hierarchy_from_model.py` (edits the live prefab).

### IETM in the Editor (no Play)
The HELIOS navigator's **Edit Page** (HELIOS UI > UI > HeliosNavigator) is the page shown while editing. With Edit Page
= **IETM**, the IETM Module is shown and fitted into the HELIOS content area (right of the menu, below the top bar)
exactly as at Play, and the HELIOS aircraft is hidden; on any other Edit Page the IETM Module is hidden (it used to
stay active and draw its own HELIOS shell - menu, card, dock - faintly behind the HELIOS UI).
`HeliosIetmEditorPreview` (Editor) + `HeliosIetmBridge.EditorPreview`; the scene setup re-applies the Edit Page
when the scene opens.

### Double-sided aircraft in every mode
The F-16 is a surface model (single panels, normals facing either way). `HeliosSceneSetup` gives every part a
double-sided copy of its material (shader **HELIOS/Double Sided Lit (Built-in)**: both sides drawn, lit from the side
facing the camera; colour, texture, normal map, metallic, smoothness, emission and alpha cut-off copied from the glTF
material), saved in `Assets/HELIOS/Materials/<model>/` and assigned in the scene at edit time. The normal view, the
Wireframe overlay (drawn over these materials) and the Cross Section (copies them) all use it. The hologram /
Transparent look draws both sides alike (the "Inside Surfaces" dimming of back faces is gone).

## Isometric start view (Editor Game view and Play)
* HeliosApp `startYaw` / `startPitch` = **135 / 30**: an isometric view of the F-16 from the front-left quarter (nose +Z).
  Scenes still on the old 120 / 18 are moved to 135 / 30 once when HELIOS.unity opens; a value you set yourself is kept.
* When the scene opens, the setup first shows the Edit Page (so an IETM preview never leaves the aircraft hidden), then
  frames the Main Camera on the aircraft at that angle if its position, rotation or field of view differ, and saves.
  Play's ResetView starts from the same angle, so the Game view before Play = the first frame at Play.
* **Tools > HELIOS > Isometric Camera View (Editor + Play)** sets 135 / 30 and frames the camera again at any time.
  Change the angle on HELIOS App (Camera > Start Yaw / Start Pitch) and run **Frame Aircraft In Game View** to use another.

## Backdrop behind the aircraft in the Editor
* The Backdrop canvas (HELIOS UI > Backdrop) is saved as Screen Space - Overlay; overlay canvases draw over every camera,
  so before Play it covered the aircraft (sort order -100 does not change that). At Play HeliosApp attached it to the
  background camera. The scene setup now does the same at edit time: Render Mode = Screen Space - Camera, Render Camera =
  HELIOS Background Camera, Plane Distance = Backdrop.distance (50), layer UI. The Game view before Play = Play.

## Transparent look (View Tools > Transparent) and scene brightness
* Transparent is no longer the cyan hologram. Each part keeps its own colour/texture and is drawn with
  `HELIOS/Ghost` (Assets/HELIOS/Shaders/Resources/HELIOS Ghost.shader): soft see-through glass, faint where a face looks
  at you and stronger at the edges, so the cockpit, engine and stores show through; matte (low shine); no wireframe;
  both sides drawn. The see-through copies of the ~13 F-16 materials are made the first time Transparent is pressed and
  re-used; the parts' own materials are put back on Solid, Hologram, another page, or a second press of Transparent.
* Tune it on HELIOS App > **Transparent**: Tint, Keep Colour (0 = grey), Face Opacity, Edge Opacity, Edge Sharpness,
  Brightness, Shine.
* Brightness: the scene setup makes the aircraft light rig x1.35 and the ambient light x1.4 once
  (HELIOS App `sceneLookVersion` = 1 marks it done). Fine-tune in Window > Rendering > Lighting > Environment and on
  HELIOS Aircraft Lights.
* **Scene Brightness** (replaces the one-off x1.35): component `HeliosBrightness` on **HELIOS Aircraft Lights**
  (Tools > HELIOS > Scene Brightness selects it). Brightness slider = every directional light x value; Ambient slider =
  the ambient light x value. Default 1.6 / 1.6. Live in the Editor Game view and at Play.

## Aircraft Hierarchy v2, part picking and hover tooltip
* No hide / show (eye) column any more; the row labels use the space. Rows: click = select, chevron = expand.
* Two parts are named in the model under another region than where they sit; the rows follow where they sit
  (`Tools/helios_ui/hierarchy_from_model.py` MOVES, written into the rows' Part Names, `!name` = left out):
  `..._Forward_Fuselage_Region_Left_Part_0331` = the radome's left half -> Nose (its right half is Nose_Right_Part_0333);
  `..._Forward_Fuselage_Region_Right_Part_0142` = side skin from -10 to -4.7 m -> Mid Fuselage (its mirror is Mid_Left_0149).
  Selecting Nose now outlines the whole radome. Counts: Nose 17, Forward Fuselage 54, Mid Fuselage 270.
* 3D view, every page that shows the model: click a part = it is outlined (wireframe, even with the wireframe off), the
  camera zooms to it and its row is selected in the hierarchy; click it again or empty space = whole aircraft.
  Mouse over a part = amber outline + tooltip "Part 0331   Nose · Left   Fuselage" (like the IETM hover tip).
  HELIOS App: Click To Select Parts, Hover Parts, Hover Colour. Tooltip object: HELIOS UI > UI > Part Tooltip (Box
  inactive until a part is under the mouse; style it in the prefab).

## One engine: inside the F-16 and in the engine IETM
* The engine you placed where the F-16's engine is (IETM Module > Landing Gear > Animated Model Pivot > Engine) is moved
  once by the scene setup into the F-16 (child of the F16_V01 instance, same place in the world). It is part of the aircraft:
  3D view, Transparent, wireframe, click / hover, Aircraft Hierarchy.
* Its IETM place is kept on IETM App > Publications > engine: **Ietm Parent** (Animated Model Pivot) + Ietm Local
  Position / Rotation / Scale (the pose you set). Opening an engine data module moves this same object there (no copy,
  nothing loaded); going back to the landing gear, or leaving the IETM page, puts it back in the F-16 (parts reassembled).
  It is no longer hidden while another publication is open.
* To change its IETM pose: edit the Ietm Local values. To change where it sits in the F-16: move it under the F-16.
* Double-sided materials are now kept per source model: Assets/HELIOS/Materials/F16_V01/ and .../engine/.
* Aircraft Hierarchy (no Left / Center / Right sub-rows; only Left Wing / Right Wing): top level Fuselage (Nose, Forward, Mid, Aft, Underside), **Cockpit**, **Engine** (Case & Mount,
  Compressor Section, Hot Section, 11 parts named after the engine IETM items), Wing, Empennage. 1824 parts.

## Sensor & Health (one page, UI only)
* `Tools/helios_ui/sensor_health_page.py` (run once) changed HELIOS UI.prefab as in Figma 1021:12072:
  menu 3D View, IETM, **Sensor Health**, Asset Integrity, Maintenance, Settings (Health item off, menu 36 shorter);
  the Sensors page (page id still "Sensors") shows Sensor Readings (card with header, plain rows with icons, no
  sparklines), System Health (the 4 rings moved from the Health page + Overall Health) and Sensor Trends. Live bar and the
  old Sensor Health card are off (not deleted). The Health page object stays in the prefab, unused.
* `Tools/helios_ui/sensor_scroll.py` (run once): Recent Events (from the Health page) is the last card; the right column
  scrolls - Sensors > Cards Scroll (RectMask2D + ScrollRect, mouse wheel / drag) with a slim auto-hiding Scrollbar in the
  8 px gap on the right; Cards is its content (ContentSizeFitter, height from the cards).
  Helpers for such edits: `Tools/helios_ui/prefab_edit.py`.

## Asset Integrity (UI only, Figma)
* `Tools/helios_ui/asset_integrity_page.py` (run once): right column = Overall Integrity (64 Healthy 89 %, 5 At Risk 7 %,
  3 Critical 4 %), Integrity Metrics tiles (Structural 95 % +2 %, Corrosion 2.4 % -0.3 %, Fatigue 4,320 hrs +6 %),
  Component Risk (72) stacked bar + legend + View All, Integrity Trend (30 days; the Health page's HeliosChart card with new
  data 89.6 -> 95.1 %, 87-96 % axis, dates 15 Oct - 12 Nov, end dot). Critical Integrity and Key Findings are off (not deleted).

## Maintenance (UI only, Figma)
* `Tools/helios_ui/maintenance_page.py` (run once; building blocks in `ui_kit.py`): Maintenance Overview = four status
  tiles (12 Completed, 6 In Progress, 4 Due (30d), 2 Overdue; old gauge off), Next Major Service (A-Check, due 15 Mar 2027,
  12 days, "In 120 days"), Overdue Maintenance (4 days overdue), Upcoming Maintenance (3: work orders WO-8842 / 8871 / 8890,
  the third row a copy of the second), Recommended Actions, Maintenance History (6 months, planned vs completed bars with
  values, 0-12 axis). The column scrolls (Cards Scroll + slim scrollbar).

## Model centred between the panels (3D View page only)
* HeliosPage has **Model Area** (RectTransform, optional). The 3D View page's is `Pages/3D View/Model Area` (between the
  Aircraft Hierarchy and the view tools, below the top bar); other pages have none and keep the whole 3D view.
* `HeliosLensShift` on the Main Camera (added by the scene setup and at Play) shifts the camera's picture so the model's
  centre sits in the middle of that rectangle (off-centre projection: picking, labels, orbit and zoom stay exact), Editor
  Game view and Play. The home distance fits the model inside the rectangle; it is refitted on every page change.
* To move or resize the area: select Model Area in the prefab and drag its edges.
* Before Play the Game view = the first frame at Play: HeliosLensShift (Edit mode, **Editor Matches Play** on) gives the
  Main Camera the 3D View Area rectangle (below the top bar, as HeliosApp does at Play) and the home pose of the aircraft
  for the shown page (start angle + zoom, fitted in that page's Model Area), updated whenever the Game view redraws.

## Sensor callouts and title
* Sensor & Health: the seven reading rows are clickable (their Fill is the invisible raycast target) and each flies to its
  sensor and shows its own callout - seven callouts in the prefab (`Tools/helios_ui/sensor_callouts.py`): Humidity,
  Structural Strain, Vibration, Engine RPM, Oil Pressure, Fuel Flow, Hydraulic (title, value, status). Click again = whole aircraft.
* Top bar title: "Digital Twin Intelligent Asset Integrity" (prefab, IETM shell data and both scenes).

## WebGL performance
* Surface shaders (HELIOS Double Sided, HELIOS Section, IETM Part Lit): `noforwardadd` + no lightmap / fog / deferred /
  meta variants: each part is drawn once per frame (the extra lights reach it per-vertex / as SH) instead of up to three
  times; fewer shader variants to compile and download.
* Quality level "Mobile" (WebGL's default): 1 pixel light, realtime shadows off, 1 cascade. The Editor (PC level) still
  shows shadows. Turn shadows back on for WebGL in Project Settings > Quality > Mobile if you want them (costs ~3x draw calls).
* Cameras: HDR off on the Main Camera and the background camera (no post effects).
* Build size: SampleScene off in the build list; the old default UI prefab moved out of Resources
  (Assets/HELIOS/Legacy/...), 4.3 MB less in the build. Everything in a Resources folder is always built.
* Light mode recolouring scans the UI every 4th frame (and right after a click) instead of every frame.
