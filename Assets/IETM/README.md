# Landing Gear IETM for Unity (POC)

This is an Interactive Electronic Technical Manual for the **F-16 nose landing gear** model. Its layout follows the Cortona3D Solo IPD viewer (publication tree, 3D view and parts list), with a modern dark UI.

Tested target: **Unity 6000.3, URP, Input System**. That is the setup in your project. The legacy Input Manager is also supported.

---

## 1. Install (2 minutes)

1. Copy the `IETM` folder into your project's `Assets/` folder. You should get `Assets/IETM/Scripts`, `Assets/IETM/Shaders` and `Assets/IETM/Resources`.
2. Wait for Unity to compile.
3. In the menu, choose **IETM ▸ Build Landing Gear IETM Scene**.
   * This creates `Assets/IETM/Scenes/LandingGear_IETM.unity`.
   * It places both models: *Extended* (main) and *Collapsed* (used as the "Retracted" state).
   * It adds the camera, the light and the `IETM App` object, with all references set.
   * It adds the scene to Build Settings.
4. Press **Play**.

The builder finds the models by name (`F-16 Front Landing Gear Assembly Extended` and `... Collapsed`), wherever they are in the project.

> Manual setup: create an empty GameObject, add `IETMApp` and drag the Extended prefab into **Extended Model**. The Collapsed prefab in **Retracted Model** is optional. Everything else (camera, lights, UI, EventSystem) is created at runtime.

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
    IETMApp.cs            controller: selection, viewer commands, procedures, history, order list
    IETMApp.Layout.cs     UI shell: top bar, panels, viewport overlays, modals, toasts, search
    IETMApp.Content.cs    publication tree, structure tree, IPD table, description, procedure views
    IETMApp.Callouts.cs   item-number balloons with leader lines
    IETMModel.cs          model binding, highlight/ghost/hide, explode, removal animation, picking, gear states
    IETMData.cs           JSON data model (S1000D / ATA iSpec 2200 style)
    ViewerCamera.cs       orbit / pan / zoom camera with smooth framing
    UIKit.cs              code-only UI toolkit (theme, rounded sprites, widgets)
    PointerHooks.cs, UITooltip.cs, InputCompat.cs
  Scripts/Editor/IETMSceneBuilder.cs      menu: IETM > Build Landing Gear IETM Scene
```

The whole UI is generated in code: there are no prefabs, sprites or fonts to wire up. To restyle it, change the colours in `Theme` (`UIKit.cs`). Layout sizes are in `IETMApp.Layout.cs` (`TOP`, `LEFT`, `RIGHT`, `STATUS`).

## 5. Notes

* **WebGL:** works as is. Snapshot is disabled there, and clipboard copy depends on the browser.
* **Model orientation:** the extended model imports lying on its side, so it is auto-rotated with the wheel down. Use `downReferenceNode` and `extraRotation` on `IETM App` to change this.
* **Retracted alignment:** the collapsed CAD export uses a different root rotation. It is aligned to the extended model using the shared CAD assembly frame (the parent of `F-16 Front Landing Gear`).
* **View preset names** (Front/Side) depend on the CAD axes. Swap the yaw values in `ViewerCamera.SetPreset` if they are mirrored.

---

## Full project package

In the full project zip, the IETM is already wired into **`Assets/Scenes/SampleScene.unity`**, as the **IETM App** object. Open the project in Unity 6000.3, open SampleScene and press **Play**.

The two gear models that were placed in the scene by hand have been removed. IETM App loads both prefabs itself (Extended as the main model, Collapsed as Retracted). The menu command **IETM ▸ Build Landing Gear IETM Scene** is still available if you want a fresh scene.
