# HELIOS + IETM: WebGL build guide

## Already set in the project (WebGL only, Editor and desktop unchanged)

| Setting | Value | Why |
|---|---|---|
| Compression | Brotli + **Decompression Fallback ON** | Smallest download. The fallback means it works on any web server without special headers. |
| Name Files As Hashes | ON | Each build gets new file names, so browsers never load stale cached files |
| Data Caching | ON | Second visit loads from the browser cache |
| Initial Memory | 128 MB (grows to 2 GB) | Fewer heap grows (stutters) while the F-16 and landing gear load |
| IL2CPP Code Generation | Optimize Size | Smaller `.wasm` |
| Managed Stripping | Low + `Assets/IETM/WebGL/link.xml` | Removes unused .NET code. `link.xml` keeps HELIOS/IETM scripts and the Input System UI module, which `HeliosApp` creates by reflection. |
| Dynamic / static batching | ON | Fewer draw calls for the many small parts |
| IETM card images | GPU-compressed (photos crunched), sizes divisible by 4 | About 13 MB less texture data than uncompressed |
| IETM 3D background | 512×256 | Same look as before (smooth gradient) at about 1/7 of the memory |

## Set in the Build Profile window (stored per machine, not in the project)

*File > Build Profiles > Web*:
- **Texture Compression:** `DXT` (desktop browsers). Use `ASTC` only for a mobile-browser build.
- **Code Optimization:**
  - `Shorter Build Time` while testing
  - `Disk Size with LTO` for the release build (smallest `.wasm`; the build takes longer)
- **Development Build:** OFF for release. Development builds are much bigger and slower.
- Scene list: `Assets/HELIOS/Scenes/HELIOS.unity`. The IETM is inside it, so no other scene is needed. (`Assets/Scenes/SampleScene.unity` is listed but doesn't exist; Unity skips it.)

## Build and test locally

```bash
# after File > Build Profiles > Web > Build, into e.g. Builds/WebGL
cd Builds/WebGL
python -m http.server 8080      # WebGL does not run from file://
# open http://localhost:8080
```

## Hosting (optional, faster start)

With Decompression Fallback ON it works everywhere. For the fastest start, serve the `.br` files with `Content-Encoding: br` and `Content-Type: application/wasm` for `.wasm.br`, then turn the fallback OFF and rebuild. Unity's docs have IIS, Nginx and Apache examples under *Server configuration code samples*.

## Test checklist after the first WebGL build

- [ ] HELIOS 3D View: aircraft, hologram/wireframe looks, sensors and health pages
- [ ] **Double-click isolate on the aircraft:** the other parts fade. If they stay opaque in WebGL only, the build stripped the transparent variant of the model's shader. Tell me and I'll add a shader-variant keeper.
- [ ] IETM tab: cards → landing gear IETM, gear extend/retract, explode, x-ray, isolate, procedures, IPD table
- [ ] Download PDF: opens in a new browser tab (allow pop-ups for the site)
- [ ] Snapshot: not available in WebGL (the IETM shows a message). That's expected.
- [ ] Frame rate: open the browser console. Below ~30 fps on the target laptops, see the next section.

## Biggest remaining win (HELIOS aircraft model)

`Assets/Model/F_16_Updated_Metadata 4.glb` has **2,055 separate meshes** (505k triangles, 7 materials). In WebGL each separate mesh is roughly one draw call, plus another for shadows. That's the main cost on lower-end machines. These options don't touch any code, from simplest:

1. **Shadows:** if the HELIOS look doesn't need them, turn shadows off on the Directional Light, or set *Quality > Mobile* (the level WebGL uses) to 1 shadow cascade. This roughly halves the draw calls.
2. **Merge small parts** that are never selected on their own (bolts, rivet groups, panel details) per material, in Blender or with `gltf-transform` (`join` / `flatten`), keeping the named parts HELIOS uses for sensors and isolate. Going from 2,055 to a few hundred meshes gives the largest speed-up.
3. **Simplify:** `gltf-transform simplify` on dense parts, and `meshopt` compression for a smaller download (needs the `com.unity.meshopt.decompress` package for glTFast).

These change the aircraft model, so they're for the HELIOS team to decide.
