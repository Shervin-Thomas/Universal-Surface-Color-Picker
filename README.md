# UniversalSurfaceColorPicker (Editor Tool)

`UniversalSurfaceColorPicker.cs` is an **Editor-only** SceneView tool that turns a “picker cube” into a live surface color sampler.

When a GameObject whose name contains **`ColorPickerCube`** is selected, the tool shoots short raycasts (6 directions) from the cube’s collider bounds center. On the first nearby hit, it samples the hit material’s base texture at the hit UV and applies that color to the cube’s material.

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
- The base texture must be readable:
  - In the texture import settings, enable **Read/Write**.

## How it works (behavior details)

- Activation: runs only when the currently selected GameObject name contains `ColorPickerCube`.
- Raycasts: casts in ±X, ±Y, ±Z from the cube collider’s bounds center.
- Range: `0.2` units per ray. Keep the cube **within ~0.2 units** of the surface you want to sample.
- Multi-material meshes: supported; the tool uses `RaycastHit.triangleIndex` + submesh triangles to pick the correct material.
- Sampling: uses `Texture2D.GetPixel()` at the hit UV.
- Applying: writes to the cube renderer’s `sharedMaterial` color (`_BaseColor` or `_Color`).

## Troubleshooting

- **Nothing happens when I select the cube**
  - The selected object’s name must contain `ColorPickerCube`.
  - The cube must have both a `Collider` and a `Renderer`.

- **It hits the object but never changes color**
  - The hit collider must be a `MeshCollider` with a valid `sharedMesh`.
  - The `MeshCollider` must be on the same object as the `Renderer`.
  - The sampled material needs `_BaseMap` or `_MainTex` assigned.

- **Console shows texture not readable / color doesn’t change**
  - Enable **Read/Write** on the texture asset used by `_BaseMap`/`_MainTex`.

- **Changing the cube color changes other objects too**
  - Add `ColorPickerCubeMaterial` to the cube so it uses a unique material instance.

## Notes / Limitations

- This is Editor tooling (SceneView-driven). It’s intended for authoring / setup workflows.
- `MeshCollider` cannot sample `SkinnedMeshRenderer` directly unless you provide a baked mesh collider workflow.
