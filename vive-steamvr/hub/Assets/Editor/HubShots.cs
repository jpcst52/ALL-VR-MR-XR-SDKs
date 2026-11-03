using System.IO;
using UnityEngine;

/// <summary>Editor helper: renders a temporary camera to a PNG (desktop render, used for documentation screenshots).</summary>
public static class HubShots
{
    public static string Render(string pngPath, float px, float py, float pz, float lx, float ly, float lz, float fov = 70f, int w = 1600, int h = 900)
    {
        var go = new GameObject("ShotCam");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = new Vector3(px, py, pz);
        cam.transform.LookAt(new Vector3(lx, ly, lz));
        cam.fieldOfView = fov; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f;
        var main = Camera.main;
        if (main != null) { cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor; }
        cam.allowHDR = false;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        cam.targetTexture = null; rt.Release();
        Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
        return pngPath;
    }

    /// <summary>Rendered triangles and renderer count of what is currently in the scene (rough cost estimate).</summary>
    public static string Stats()
    {
        long tris = 0; int rend = 0, active = 0;
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null) continue;
            var r = mf.GetComponent<Renderer>(); if (r == null || !r.enabled || !mf.gameObject.activeInHierarchy) continue;
            for (int i = 0; i < mf.sharedMesh.subMeshCount; i++) tris += mf.sharedMesh.GetIndexCount(i) / 3;
            rend++;
        }
        foreach (var sr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
        {
            if (sr.sharedMesh == null || !sr.gameObject.activeInHierarchy) continue;
            for (int i = 0; i < sr.sharedMesh.subMeshCount; i++) tris += sr.sharedMesh.GetIndexCount(i) / 3;
            rend++;
        }
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.enabled) active++;
        return $"meshRenderers={rend} triangles={tris} lights={active}";
    }
}
