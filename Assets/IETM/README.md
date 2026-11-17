# Landing Gear IETM for Unity (POC)

This is an Interactive Electronic Technical Manual for the **F-16 nose landing gear** model. Its layout follows the Cortona3D Solo IPD viewer (publication tree, 3D view and parts list), with a modern dark UI.

Tested target: **Unity 6000.3, URP, Input System**. That is the setup in your project. The legacy Input Manager is also supported.

---

## 1. Set up: UI and model pre-built in the Editor

1. Open the project in Unity 6000.3 and wait for it to compile.
2. The first time, Unity asks **"Build the Landing Gear IETM scene now?"**. Click **Build scene**. You can also run **IETM ▸ Build Landing Gear IETM Scene** from the menu at any time.
3. This creates `Assets/IETM/Scenes/LandingGear_IETM.unity` with:
   * **IETM Canvas**: the complete UI as normal GameObjects (top bar, panels, toolbar, part card, dialogs). It's already filled with the parts list and trees so it looks finished in the **Game view** before you press Play.
   * **Landing Gear**: both models, already oriented upright and aligned with each other. Each part has an **IETM Part** component showing its IPD item number. Colliders are added for picking.
   * **IETM Camera**, lights, **EventSystem**, and **IETM App** with all references set.
4. Press **Play** to run the IETM.

> If you open a scene where IETM App has no UI assigned, the UI is generated at Play instead, as in the first version.

---

## 1b. Editing in the Editor (no code)

| I want to… | Do this |
|---|---|
| Move, resize or recolour a panel, button or text | Select it under **IETM Canvas** and edit its RectTransform, Image or Text in the Inspector. The Game view shows the result. |
| Change what a button does | Select the button. In **IETM Button**, pick a **Command** (Isolate, X-Ray, Next step…). For **OpenModule** set **Argument** to a module id (e.g. `DM-WHEEL-RI`); for **SelectItem** set it to an item number (e.g. `230`). |
| Add a new button | Duplicate an existing button (Ctrl+D), move it, set its Command and Argument. It's wired automatically at Play. |
| Change a button's colours | **IETM Button**: Normal / Hover / Active (toggled on) / Label colours |
| Restyle the generated rows (parts table, trees, steps, order list, callouts, toasts) | IETM App Inspector ▸ **Show Row Templates**, then edit the objects named `Template …`. Every row created at Play is a copy. Keep the keys in each template's **UI Refs** list. |
| Change selection, hover and row colours | Select **IETM Canvas** ▸ **IETM View** ▸ **Theme** |
| Remap a model part to another IPD item | Select the part node under **Landing Gear** ▸ **IETM Part** ▸ change **Item No** |
| Edit part numbers, descriptions, modules or procedure steps | Edit `Resources/IETM/landing_gear_ietm.json`, then IETM App ▸ **Refresh Preview Content** (Editor preview only; Play always reads the JSON) |
| Start in another module | IETM App ▸ **Start Module Id** |
| Start from a fresh UI | IETM App ▸ **Rebuild UI**. This replaces your UI edits. |

The IETM App Inspector has buttons for **Rebuild UI**, **Refresh Preview Content**, **Select UI**, **Re-tag Model Parts From Data**, **Open Data (JSON)** and **Show Row Templates**. The same actions are also in the **IETM** menu.

**What stays live (Play mode only):** callout positions, hover highlighting, the camera, the explode and removal animations, toasts, search results, and the procedure player. These use the objects and templates you edited.

---

## 2. What you get

| Area | Features |
|---|---|
| **Top bar** | Publication title and PMC, back/forward history, title-page button, global part search (part number, item number, description, CAGE) with a results dropdown, panel toggles, order list, help |
| **Left panel: Publication** | S1000D-style data module tree, grouped by system (32-20 nose landing gear, 32-30 extension and retraction, 32-50 steering). Each module has a type tag (DESC / IPD / PROC). Hover shows the DMC. |
| **Left panel: Structure** | Product breakdown from the IPD indent levels, with expand/collapse and a show/hide toggle per part or assembly |
| **3D viewport** | Orbit, pan and zoom with smooth camera moves. Hover highlight with a tooltip. Click to select, double-click to zoom, Ctrl+click to select the parent assembly. Isolate, hide, show all, X-ray, explode slider, auto-spin. View presets and snapshots. |
| **Callouts** | IPC-style item-number balloons with leader lines that follow the parts. Click a balloon to select the part. |
| **Gear state** | **Extended / Retracted** toggle (Space). The collapsed model is aligned to the extended one automatically. |
| **Part card** | Item, nomenclature, P/N, CAGE, ISN, UPA, SMR, a functional description, maintenance notes, and Zoom / Isolate / Hide / +Order buttons |
| **Right panel: IPD** | DMC header, applicability, a figure card, and the parts list (Item, ISN, CAGE, Part number, indented description, UPA). The list filters as you type and copies to the clipboard. **Two-way sync** between list rows and the 3D view. |
| **Right panel: Description** | Paragraph cards with **Show in 3D** and buttons that link to parts |
| **Right panel: Procedure** | Step player: step N of M, a progress bar, WARNING and CAUTION boxes, Previous/Next (← →), and a clickable step list. Steps can isolate parts, **animate part removal**, switch gear state and set the camera. |
| **Order list** | Add parts from any row or from the card. Change quantities and copy the list as tab-separated text. |
| **Status bar** | Selection, gear state, hidden count, order lines, FPS, and a "SAMPLE DATA" flag |

### Keyboard shortcuts
`F` fit selection · `Home` home view · `I` isolate · `H` hide · `X` X-ray · `E` explode · `L` callouts · `R` spin · `Space` extend/retract · `← →` procedure steps · `Esc` clear/close · `/` help

---

## 3. Editing the manual content

All content is in **`Assets/IETM/Resources/IETM/landing_gear_ietm.json`**. No code changes are needed.

* **`items`** is the IPD. `indent` builds the hierarchy (1 = top assembly, 2 = sub-assembly, 3 = part).
  * `nodes` are the **GameObject names** in the model that belong to that item. A part with several bodies can list several names.
  * Assemblies leave `nodes` empty and get their parts from their children.
* **`modules`** are the data modules. `type` is `description`, `ipd` or `procedure`.
  * Procedure steps take:
    * `focus` (item numbers)
    * `action`: `highlight`, `isolate`, `remove` or `restore`
    * `view`: `iso`, `front`, `side` or `top`
    * `gear`: `extended` or `retracted`
    * optional `warning` and `caution`

### Part mapping used (model node → IPD item)

| Item | Nomenclature | Model node |
|---|---|---|
| 010 | Housing - strut, nose gear | `F-16 Front Landing Gear` |
| 020 | Arm - swivel, main | `Main Swivel Arm` |
| 030 | Collar - strut | `Collar` |
| 050–090 | Steering assy (collar, top head, upper/lower links, servo link) | `Steering Collar`, `Steering Top Head`, `Steering Link Upper`, `Steering Link Lower`, `Steering Link for servo` |
| 110–150 | Retraction mechanism (actuator, main/secondary arms, nut, rotation link) | `Gear Actuator`, `Main Actuator Arm`, `Secondary Actuator Arm`, `Actuator Nut`, `Gear Rotation Link` |
| 170–190 | Drag brace (inner/outer folding arm, pivot) | `Inner Folding Arm`, `Outer Folding Arm`, `Pivot for Inner Arm` |
| 210–230 | Fork, hub, wheel | `Fork`, `Main Gear`, `Wheel` |

> ⚠️ **The part numbers, CAGE codes and procedures are sample data** for the POC. Replace them with OEM data before showing them as real. The Cortona sample (`32-10-02-12A`) is a different gear (MLG), so its part numbers do not map to this model.

---

## 4. Files

```
IETM/
  Resources/IETM/landing_gear_ietm.json   manual content (IPD, DMs, procedures)
  Shaders/IETM_Highlight.shader           selection / hover (lit + fresnel rim)
  Shaders/IETM_Ghost.shader               X-ray / isolate context (transparent fresnel)
  Scripts/Runtime/
    IETMApp.cs            controller: commands, selection, viewer, procedures, history, order list
    IETMApp.UI.cs         drives the pre-built UI: wiring, lists from templates, dialogs, toasts, search
    IETMApp.Callouts.cs   item-number balloons with leader lines (from the callout template)
    IETMUIBuilder.cs      builds the UI hierarchy + row templates (Editor menu, or runtime fallback)
    IETMView.cs           references to every UI element + theme colours (on IETM Canvas)
    IETMButton.cs         styled button with an Inspector-selectable Command
    UIRefs.cs             named references inside a row template
    IETMPart.cs           marks a model node as an IPD item
    IETMModelSetup.cs     orient / align / colliders / part tagging helpers
    IETMModel.cs          model binding, highlight/ghost/hide, explode, removal animation, picking, gear states
    IETMData.cs           JSON data model (S1000D / ATA iSpec 2200 style)
    ViewerCamera.cs       orbit / pan / zoom camera with smooth framing
    UIKit.cs              code-only UI toolkit (theme, rounded sprites, widgets)
    PointerHooks.cs, UITooltip.cs, InputCompat.cs
  Scripts/Editor/IETMSceneBuilder.cs      IETM menu: build scene, rebuild UI, refresh preview, re-tag parts
  Scripts/Editor/IETMAppEditor.cs         IETM App inspector buttons
  UI/Sprites/                             rounded-corner sprites (generated on first build)
```

The default look comes from `Theme` (`UIKit.cs`) and `IETMUIBuilder.cs`. After the scene is built, edit everything in the Inspector instead. Panel sizes are read from the scene, so resizing the left or right panel in the Editor works, including the collapse animation.

## 5. Notes

* **WebGL:** works as is. Snapshot is disabled there, and clipboard copy depends on the browser.
* **Model orientation:** the extended model imports lying on its side, so it is auto-rotated with the wheel down. Use `downReferenceNode` and `extraRotation` on `IETM App` to change this.
* **Retracted alignment:** the collapsed CAD export uses a different root rotation. It is aligned to the extended model using the shared CAD assembly frame (the parent of `F-16 Front Landing Gear`).
* **View preset names** (Front/Side) depend on the CAD axes. Swap the yaw values in `ViewerCamera.SetPreset` if they are mirrored.

---

## Full project package

In the full project zip, the IETM is already wired into **`Assets/Scenes/SampleScene.unity`**, as the **IETM App** object. Open the project in Unity 6000.3, open SampleScene and press **Play**.

The two gear models that were placed in the scene by hand have been removed. IETM App loads both prefabs itself (Extended as the main model, Collapsed as Retracted). The menu command **IETM ▸ Build Landing Gear IETM Scene** is still available if you want a fresh scene.
