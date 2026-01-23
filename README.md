# UniversalSurfaceColorPicker (Editor Tool)

`UniversalSurfaceColorPicker.cs` is an **Editor-only** SceneView tool that turns a “picker cube” into a live surface color sampler.

It has no window/menu item: it hooks `SceneView.duringSceneGui` via `[InitializeOnLoad]` and runs automatically while the editor is open.

When a GameObject whose name contains **`ColorPickerCube`** is selected, the tool shoots short raycasts (6 directions) from the cube’s collider bounds center. On the first nearby hit, it samples the hit material and applies the resulting color to the cube’s material.

## Files / Locations

- [Assets/Editor/UniversalSurfaceColorPicker.cs](Assets/Editor/UniversalSurfaceColorPicker.cs)
  - Editor script (runs in the Scene view via `SceneView.duringSceneGui`).
- [Assets/Scripts/ColorPickerCubeMaterial.cs](Assets/Scripts/ColorPickerCubeMaterial.cs)
  - Runtime folder script (Editor-gated via `#if UNITY_EDITOR`) that ensures the cube has a **unique material instance**, so picking doesn’t overwrite a shared material asset.

> Note: Keep `ColorPickerCubeMaterial.cs` in **Assets/Scripts** (not under **Assets/Editor**) as requested.

## Setup

### 1) Create the picker cube

1. Create a cube (or any renderer object you want to act as the picker).
2. **Name requirement:** the object name must contain **`ColorPickerCube`**.
   - Example: `ColorPickerCube`, `ColorPickerCube (1)`, `My_ColorPickerCube_A`.
   - If you duplicate the cube, **rename it so it still contains `ColorPickerCube`**.
3. Add a collider to the cube (required). Any `Collider` works for the picker cube itself (e.g., `BoxCollider`).
4. Ensure the cube has a `Renderer` with a material that exposes either:
   - `_BaseColor` (URP/Lit style), or
   - `_Color` (legacy/standard style)
5. Add `ColorPickerCubeMaterial` to the same GameObject as the cube’s `Renderer`.

### 2) Prepare target objects (the surfaces you want to sample)

For a surface to be “detected” and sampled, it must satisfy all of these:

- It must be hit by physics raycasts (so it needs a collider).
- It must have a **`MeshCollider`** (the tool ignores non-mesh colliders).
- The **`MeshCollider` must be on the same GameObject as the `MeshRenderer`**.
  - Do **not** put the MeshCollider only on a parent object.
  - The tool uses `hit.collider.GetComponent<Renderer>()`, so it expects the renderer on the collider object.
- The sampled material must have a base texture in one of these properties:
  - `_BaseMap` (URP)
  - `_MainTex` (fallback)
- If you want **true per-pixel sampling**, the base texture must be readable:
  - In the texture import settings, enable **Read/Write**.
  - If it’s not readable (or there is no texture), the tool falls back to using the material tint color only.

## How it works (behavior details)

- Activation: runs only when the currently selected GameObject name contains `ColorPickerCube`.
- Raycasts: casts in ±X, ±Y, ±Z from the cube collider’s bounds center.
- Range: `0.2` units per ray. Keep the cube **within ~0.2 units** of the surface you want to sample.
- Multi-material meshes: supported; the tool uses `RaycastHit.triangleIndex` + submesh triangles to pick the correct material.
- Sampling (when possible): uses `Texture2D.GetPixel()` at the hit UV.
- Color math: multiplies sampled texture color by the material tint in **linear** space, then converts back to gamma so it looks lighting-independent.
- Surface handling (URP-correct-ish):
  - **Cutout**: `_ALPHATEST_ON` → ignores pixels below `_Cutoff`.
  - **Transparent**: `_SURFACE_TYPE_TRANSPARENT` or `renderQueue >= 3000` → keeps alpha from texture × tint.
  - **Opaque**: forces output alpha to 1.
- Applying: writes to the cube renderer’s `sharedMaterial` color (`_BaseColor` or `_Color`).

## Troubleshooting

- **Nothing happens when I select the cube**
  - The selected object’s name must contain `ColorPickerCube`.
  - The cube must have both a `Collider` and a `Renderer`.

- **It hits the object but never changes color**
  - The hit collider must be a `MeshCollider` with a valid `sharedMesh`.
  - The `MeshCollider` must be on the same object as the `Renderer`.
  - If the surface is **cutout**, the ray may be hitting fully transparent pixels; move the cube slightly and try again.
  - If the material has no `_BaseMap`/`_MainTex`, the tool will use the material tint only.

- **Color is always “flat” (only tint) instead of matching the texture**
  - Enable **Read/Write** on the texture asset used by `_BaseMap`/`_MainTex` so per-pixel sampling works.

- **Changing the cube color changes other objects too**
  - Add `ColorPickerCubeMaterial` to the cube so it uses a unique material instance.

## Notes / Limitations

- This is Editor tooling (SceneView-driven). It’s intended for authoring / setup workflows.
- Material/shader support is best for URP/Lit-style properties (`_BaseMap`, `_BaseColor`). Custom shaders may use different property names and won’t be sampled.
- `MeshCollider` cannot sample `SkinnedMeshRenderer` directly unless you provide a baked mesh collider workflow.
