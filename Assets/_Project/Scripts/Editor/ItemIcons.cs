#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Horror.EditorTools
{
    /// <summary>
    /// Miniaturas del inventario: fotografia el modelo de cada objeto (fondo transparente, tres cuartos, luz de
    /// estudio) y la guarda como Sprite en Art/Icons, asignandola a ItemData.icon.
    /// </summary>
    public static class ItemIcons
    {
        const string Folder = "Assets/_Project/Art/Icons";
        const int Size = 256;

        public static void Generate(params ItemData[] items)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Icons");
            var studio = new GameObject("IconStudio");
            studio.transform.position = new Vector3(0f, -500f, 0f);
            try
            {
                var cam = new GameObject("Cam").AddComponent<Camera>();
                cam.transform.SetParent(studio.transform, false);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.orthographic = true;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 10f;
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = false;
                data.renderShadows = false;
                void L(Vector3 p, float i)
                {
                    var l = new GameObject("L").AddComponent<Light>();
                    l.transform.SetParent(studio.transform, false);
                    l.transform.localPosition = p;
                    l.type = LightType.Point; l.range = 6f; l.intensity = i; l.shadows = LightShadows.None;
                }
                L(new Vector3(-1f, 1.2f, -1.2f), 6f);
                L(new Vector3(1.2f, 0.3f, -0.8f), 2.5f);
                L(new Vector3(0.3f, 1f, 1.2f), 4f);

                var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
                cam.targetTexture = rt;
                foreach (var item in items)
                {
                    if (item == null || item.worldPrefab == null) continue;
                    var model = (GameObject)Object.Instantiate(item.worldPrefab, studio.transform);
                    // vista de tres cuartos, objeto tumbado como en una mesa
                    var holder = new GameObject("Holder").transform;
                    holder.SetParent(studio.transform, false);
                    model.transform.SetParent(holder, true);
                    holder.localRotation = Quaternion.Euler(-28f, 35f, 0f);
                    var b = Bounds(model);
                    float big = Mathf.Max(b.size.x, b.size.y);
                    cam.orthographicSize = big * 0.6f;
                    cam.transform.position = b.center - Vector3.forward * 3f;
                    cam.transform.rotation = Quaternion.identity;
                    cam.Render();
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;
                    string path = $"{Folder}/{item.name}.png";
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    Object.DestroyImmediate(holder.gameObject);

                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                    ti.textureType = TextureImporterType.Sprite;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                    item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    EditorUtility.SetDirty(item);
                }
                cam.targetTexture = null;
                rt.Release();
            }
            finally
            {
                Object.DestroyImmediate(studio);
            }
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
#endif
