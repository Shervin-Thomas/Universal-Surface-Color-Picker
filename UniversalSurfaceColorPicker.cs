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

            Material mat = ResolveMaterial(hit, targetRenderer);
            if (mat == null)
                continue;

            Texture2D tex = GetBaseMapTexture(mat);
            if (tex == null || !tex.isReadable)
                continue;

            Color picked = SampleTexture(tex, hit.textureCoord);
            ApplyColor(cubeRenderer, picked);

            SceneView.RepaintAll();
            break;
        }
    }

    // ---------------- HELPERS ----------------

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

    static Texture2D GetBaseMapTexture(Material mat)
    {
        if (mat.HasProperty("_BaseMap"))
            return mat.GetTexture("_BaseMap") as Texture2D;

        if (mat.HasProperty("_MainTex"))
            return mat.GetTexture("_MainTex") as Texture2D;

        return null;
    }

    static Color SampleTexture(Texture2D tex, Vector2 uv)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * tex.width), 0, tex.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * tex.height), 0, tex.height - 1);
        return tex.GetPixel(x, y);
    }

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
