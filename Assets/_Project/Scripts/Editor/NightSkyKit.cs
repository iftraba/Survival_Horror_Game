#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Horror.EditorTools
{
    /// <summary>
    /// Cielo nocturno (2026-10-14): importa el panorama <c>Art/Textures/Sky/NightSky.png</c> (hecho por <c>Tools/blender/build_night_sky.py</c>) como textura 2D
    /// latitud-longitud, crea el material <c>Materials/NightSky.mat</c> (Skybox/Panoramic), lo pone como skybox de la escena abierta, hace que la camara
    /// principal dibuje el cielo en vez de un color plano y ajusta el color de la niebla al horizonte. Repetible.
    /// </summary>
    public static class NightSkyKit
    {
        const string TexPath = "Assets/_Project/Art/Textures/Sky/NightSky.png";
        const string MatPath = "Assets/_Project/Materials/NightSky.mat";

        [MenuItem("Horror/Atmosfera/Cielo nocturno")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Horror] Sal de Play antes de aplicar el cielo."); return; }
            var ti = (TextureImporter)AssetImporter.GetAtPath(TexPath);
            if (ti == null) { Debug.LogError("[Horror] Falta " + TexPath + " (ejecuta Tools/blender/build_night_sky.py)."); return; }
            ti.textureShape = TextureImporterShape.Texture2D;        // panorama 2D: Skybox/Panoramic lo lee como latitud-longitud a resolucion completa (las estrellas miden 1 o 2 pixeles)
            ti.sRGBTexture = true;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 4096;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null) { mat = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(mat, MatPath); }
            mat.shader = Shader.Find("Skybox/Panoramic");
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Mapping", 1f);                    // latitud-longitud
            mat.SetFloat("_ImageType", 0f);                  // 360 grados
            mat.SetFloat("_MirrorOnBack", 0f);
            mat.SetFloat("_Layout", 0f);
            mat.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            mat.SetFloat("_Exposure", 1.0f);
            mat.SetFloat("_Rotation", 0f);
            EditorUtility.SetDirty(mat);

            RenderSettings.skybox = mat;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.fogColor = new Color(0.05f, 0.055f, 0.07f);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (cam.CompareTag("MainCamera") || cam.GetComponent<ThirdPersonCamera>() != null)
                { cam.clearFlags = CameraClearFlags.Skybox; EditorUtility.SetDirty(cam); }
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { l.transform.rotation = Quaternion.Euler(23f, 270f, 0f); EditorUtility.SetDirty(l.transform); }      // la luz viene de la luna (al este, 23 grados)
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[Horror] Cielo nocturno aplicado.");
        }
    }
}
#endif
