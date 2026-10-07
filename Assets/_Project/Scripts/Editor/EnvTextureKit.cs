#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Aplica las texturas del escenario v2 (Tools/blender/build_env_textures.py) a los materiales Env_*: color, normales,
    /// metal/suavidad (_ms, suavidad en el alfa) y oclusion (_ao). Crea Env_Column (hormigon visto) y se lo pone a las
    /// columnas y pilares de la escena abierta (antes usaban Env_Wall). Menu: Horror/Aplicar texturas del escenario. Repetible.
    /// </summary>
    public static class EnvTextureKit
    {
        const string T = "Assets/_Project/Art/Textures/";
        const string M = "Assets/_Project/Materials/";

        static readonly (string mat, string tex)[] Mats =
        {
            ("Env_Floor", "tex_floor"), ("Env_Wall", "tex_wall"), ("Env_Ceiling", "tex_ceiling"),
            ("Env_Wood", "tex_wood"), ("Env_Metal", "tex_metal"), ("Env_Column", "tex_concrete"),
        };

        static Texture2D Tex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(T + file + ".png");

        static void Importer(string file, bool normal, bool linear, bool alpha)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(T + file + ".png");
            if (ti == null) return;
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !(normal || linear);
            ti.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = false;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 8;                                    // suelo y techo se ven muy de lado
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }

        [MenuItem("Horror/Aplicar texturas del escenario")]
        public static void Menu() { Debug.Log("[Horror] " + Apply()); }

        public static string Apply()
        {
            var log = new List<string>();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var (mat, tex) in Mats)
            {
                Importer(tex, false, false, false);
                Importer(tex + "_n", true, true, false);
                Importer(tex + "_ms", false, true, true);
                Importer(tex + "_ao", false, true, false);

                string path = M + mat + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
                m.shader = lit;
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BaseMap", Tex(tex));
                m.SetTexture("_BumpMap", Tex(tex + "_n")); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f);
                m.SetTexture("_MetallicGlossMap", Tex(tex + "_ms")); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_SmoothnessTextureChannel", 0f);     // suavidad en el alfa del mapa de metal
                m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                m.SetFloat("_Smoothness", 1f);
                m.SetFloat("_Metallic", 1f);
                m.SetTexture("_OcclusionMap", Tex(tex + "_ao")); m.EnableKeyword("_OCCLUSIONMAP"); m.SetFloat("_OcclusionStrength", 1f);
                EditorUtility.SetDirty(m);
                log.Add(mat);
            }
            AssetDatabase.SaveAssets();

            // columnas y pilares de la escena abierta: hormigon en lugar de la pared pintada
            var column = AssetDatabase.LoadAssetAtPath<Material>(M + "Env_Column.mat");
            var wall = AssetDatabase.LoadAssetAtPath<Material>(M + "Env_Wall.mat");
            int cols = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!(r.name == "Column" || r.name == "Pillar") || r.sharedMaterial != wall) continue;
                Undo.RecordObject(r, "Columnas de hormigon");
                r.sharedMaterial = column;
                cols++;
            }
            if (cols > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            return "texturas del escenario aplicadas: " + string.Join(", ", log) + "; columnas de hormigon: " + cols;
        }
    }
}
#endif
