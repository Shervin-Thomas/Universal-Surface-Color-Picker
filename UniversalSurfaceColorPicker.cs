

// ####### Done By Shervin Thomas #######



using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UniversalSurfaceColorPicker
{
    static readonly Vector3[] RayDirections =
    {
        Vector3.right,
        Vector3.left,
        Vector3.up,
        Vector3.down,
        Vector3.forward,
        Vector3.back
    };

    enum SurfaceType
    {
        Opaque,
        Transparent,
        Cutout
    }

    static UniversalSurfaceColorPicker()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        GameObject cube = Selection.activeGameObject;
        if (cube == null || !cube.name.Contains("ColorPickerCube"))
            return;

        Collider cubeCollider = cube.GetComponent<Collider>();
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeCollider == null || cubeRenderer == null)
            return;

        Vector3 origin = cubeCollider.bounds.center;

        foreach (Vector3 dir in RayDirections)
        {
            if (!Physics.Raycast(origin, dir, out RaycastHit hit, 0.2f))
                continue;

            MeshCollider meshCol = hit.collider as MeshCollider;
            if (meshCol == null || meshCol.sharedMesh == null)
                continue;

            Renderer targetRenderer = hit.collider.GetComponent<Renderer>();
            if (targetRenderer == null)
                continue;

            Material sourceMat = ResolveMaterial(hit, targetRenderer);
            if (sourceMat == null)
                continue;

            SurfaceType surfaceType = GetSurfaceType(sourceMat);
            float cutoff = GetAlphaCutoff(sourceMat);

            Color tint = GetMaterialTint(sourceMat);
            Color picked;

            Texture2D baseMap = GetBaseMapTexture(sourceMat);

            // ---------- COLOR PICKING ----------
            if (baseMap != null && baseMap.isReadable)
            {
                Color texColor = SampleTexture(baseMap, hit.textureCoord);

                // Cutout: ignore invisible pixels
                if (surfaceType == SurfaceType.Cutout && texColor.a < cutoff)
                    continue;

                // Linear-space multiply (lighting-independent)
                Color rgb = texColor.linear * tint.linear;

                float alpha = surfaceType == SurfaceType.Opaque
                    ? 1f
                    : texColor.a * tint.a;

                picked = rgb.gamma;
                picked.a = alpha;
            }
            else
            {
                // Material-only color (no texture)
                picked = tint;
                if (surfaceType == SurfaceType.Opaque)
                    picked.a = 1f;
            }

            ApplyColor(cubeRenderer, picked);
            SceneView.RepaintAll();
            break;
        }
    }

    // ---------------- SURFACE TYPE (URP-CORRECT) ----------------

    static SurfaceType GetSurfaceType(Material mat)
    {
        // Cutout (Alpha Clipping)
        if (mat.IsKeywordEnabled("_ALPHATEST_ON"))
            return SurfaceType.Cutout;

        // Transparent (keyword first, renderQueue fallback)
        if (mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"))
            return SurfaceType.Transparent;

        if (mat.renderQueue >= 3000)
            return SurfaceType.Transparent;

        return SurfaceType.Opaque;
    }

    static float GetAlphaCutoff(Material mat)
    {
        if (mat.HasProperty("_Cutoff"))
            return mat.GetFloat("_Cutoff");

        return 0.5f;
    }

    // ---------------- MATERIAL RESOLUTION ----------------

    static Material ResolveMaterial(RaycastHit hit, Renderer renderer)
    {
        if (renderer.sharedMaterials.Length == 1)
            return renderer.sharedMaterial;

        MeshCollider mc = hit.collider as MeshCollider;
        if (mc == null)
            return renderer.sharedMaterial;

        Mesh mesh = mc.sharedMesh;
        int triIndex = hit.triangleIndex;
        if (triIndex < 0)
            return renderer.sharedMaterial;

        int subMesh = GetSubMeshIndex(mesh, triIndex);
        return renderer.sharedMaterials[subMesh];
    }

    static int GetSubMeshIndex(Mesh mesh, int triangleIndex)
    {
        int offset = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int triCount = mesh.GetTriangles(i).Length / 3;
            if (triangleIndex < offset + triCount)
                return i;
            offset += triCount;
        }
        return 0;
    }

    // ---------------- COLOR SOURCES ----------------

    static Texture2D GetBaseMapTexture(Material mat)
    {
        if (mat.HasProperty("_BaseMap"))
            return mat.GetTexture("_BaseMap") as Texture2D;

        if (mat.HasProperty("_MainTex"))
            return mat.GetTexture("_MainTex") as Texture2D;

        return null;
    }

    static Color GetMaterialTint(Material mat)
    {
        if (mat.HasProperty("_BaseColor"))
            return mat.GetColor("_BaseColor");

        if (mat.HasProperty("_Color"))
            return mat.color;

        return Color.white;
    }

    static Color SampleTexture(Texture2D tex, Vector2 uv)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * tex.width), 0, tex.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * tex.height), 0, tex.height - 1);
        return tex.GetPixel(x, y);
    }

    // ---------------- APPLY ----------------

    static void ApplyColor(Renderer cubeRenderer, Color color)
    {
        Material mat = cubeRenderer.sharedMaterial;
        if (mat == null)
            return;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.color = color;
    }
}
