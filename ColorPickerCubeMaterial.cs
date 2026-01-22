#if UNITY_EDITOR
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ColorPickerCubeMaterial : MonoBehaviour
{
    Renderer rend;

    void OnEnable()
    {
        EnsureUniqueMaterial();
    }

    void OnValidate()
    {
        EnsureUniqueMaterial();
    }

    void EnsureUniqueMaterial()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        if (rend == null)
            return;

        if (rend.sharedMaterial == null)
            return;

        Material instance = Instantiate(rend.sharedMaterial);
        instance.name = rend.sharedMaterial.name + "_Instance";

        rend.sharedMaterial = instance;
    }
}
#endif