using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the Microsoft Rocketbox avatars (MIT, see Assets/Rocketbox/LICENSE.md) under Assets/Rocketbox.
/// Written for this project (not the library's own import script); it only touches files under Assets/Rocketbox.
///  - materials: flat colour white (the 3ds Max materials expect the texture alone) and Fade instead of Transparent for hair
///  - meshes: only the hi-poly mesh level is active
///  - textures: normal maps flagged as such, everything capped at 1024 px for VR memory
///  - rig: Generic, with the second biped root renamed to the first so animation clips share bone paths
/// </summary>
public class RocketboxImport : AssetPostprocessor
{
    bool IsRocketbox => assetPath.Replace('\\', '/').Contains("/Rocketbox/");

    void OnPreprocessTexture()
    {
        if (!IsRocketbox) return;
        var t = (TextureImporter)assetImporter;
        t.maxTextureSize = 1024;
        if (assetPath.ToLowerInvariant().Contains("normal"))
        {
            t.textureType = TextureImporterType.NormalMap;
            t.convertToNormalmap = false;
        }
    }

    void OnPostprocessMaterial(Material material)
    {
        if (!IsRocketbox) return;
        material.color = Color.white;
        if (material.HasProperty("_Mode") && material.GetFloat("_Mode") == 3f) material.SetFloat("_Mode", 2f);
    }

    void OnPostprocessMeshHierarchy(GameObject go)
    {
        if (!IsRocketbox) return;
        var n = go.name.ToLowerInvariant();
        if (n.Contains("poly") && !n.Contains("hipoly")) go.SetActive(false);
    }

    void OnPostprocessModel(GameObject g)
    {
        if (!IsRocketbox) return;
        if (g.transform.Find("Bip02") != null) Rename(g.transform);
        ((ModelImporter)assetImporter).animationType = ModelImporterAnimationType.Generic;
    }

    static void Rename(Transform t)
    {
        t.name = t.name.Replace("Bip02", "Bip01");
        for (int i = 0; i < t.childCount; i++) Rename(t.GetChild(i));
    }
}
