#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, calidad AA, fase B (menu Horror/Comisaria grande/19 Materiales escaneados): sustituye las texturas procedurales de los materiales
    /// de arquitectura (Env_*) por materiales PBR escaneados de Poly Haven (CC0) guardados en Art/Textures/PBR: color, normales (OpenGL), metal y suavidad
    /// empaquetados (_ms: R metal, A suavidad; hecho en Blender a partir de la rugosidad) y oclusion. Cada material conserva su escala de UV (las cajas del
    /// nivel ya llevan las UV en metros / baldosa), con un factor por material para que la textura tenga el tamano real. Repetible.
    /// </summary>
    public static class ComisariaGrandePBR
    {
        const string Dir = "Assets/_Project/Art/Textures/PBR/";
        const string Mats = "Assets/_Project/Materials/";

        // material, textura PBR, tinte, escala de UV, intensidad de normal
        static readonly (string mat, string tex, Color tint, float scale, float bump)[] Map =
        {
            ("Env_Floor", "worn_tile_floor", new Color(0.82f, 0.84f, 0.84f), 2.0f, 1.0f),        // a escala 1 las baldosas median ~1,5 m
            ("Env_Wall", "painted_plaster_wall", new Color(0.78f, 0.8f, 0.74f), 1.0f, 1.0f),
            ("Env_Lambrin", "painted_plaster_wall", new Color(0.36f, 0.44f, 0.4f), 1.0f, 1.0f),
            ("Env_Cornisa", "plaster_grey_04", new Color(0.85f, 0.85f, 0.82f), 1.0f, 0.8f),
            // Env_Ceiling se queda con su textura propia de placas de techo con perfileria (ceiling_interior es gotele y el techo parecia una cueva)
            ("Env_Column", "dirty_concrete", new Color(0.85f, 0.85f, 0.85f), 1.0f, 1.0f),
            ("Env_Wood", "dark_wood", new Color(1f, 1f, 1f), 1.0f, 1.0f),
            ("Env_Metal", "painted_metal_shutter", new Color(0.7f, 0.72f, 0.72f), 1.0f, 1.0f),
            ("Env_Asphalt", "asphalt_02", new Color(0.8f, 0.8f, 0.8f), 1.0f, 1.0f),
            ("Env_Sidewalk", "concrete_floor_worn_001", new Color(0.85f, 0.85f, 0.85f), 1.0f, 1.0f),
        };

        static Texture2D Import(string file, bool normal, bool srgb)
        {
            string p = Dir + file;
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) return null;
            bool changed = false;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ti.textureType != type) { ti.textureType = type; changed = true; }
            if (ti.sRGBTexture != srgb) { ti.sRGBTexture = srgb; changed = true; }
            if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.CompressedHQ) { ti.textureCompression = TextureImporterCompression.CompressedHQ; changed = true; }
            if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; changed = true; }
            if (ti.anisoLevel != 8) { ti.anisoLevel = 8; changed = true; }
            if (file.EndsWith("_ms.png") && ti.alphaSource != TextureImporterAlphaSource.FromInput) { ti.alphaSource = TextureImporterAlphaSource.FromInput; changed = true; }
            if (changed) ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        [MenuItem("Horror/Comisaria grande/19 Materiales escaneados")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            AssetDatabase.Refresh();
            int n = 0; var missing = "";
            foreach (var e in Map)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Mats + e.mat + ".mat");
                if (m == null) { missing += e.mat + " "; continue; }
                var col = Import(e.tex + "_diff.jpg", false, true);
                var nor = Import(e.tex + "_nor.jpg", true, false);
                var ms = Import(e.tex + "_ms.png", false, false);
                var ao = Import(e.tex + "_ao.jpg", false, false);
                if (col == null) { missing += e.tex + " "; continue; }
                m.SetTexture("_BaseMap", col); m.SetTexture("_MainTex", col); m.SetColor("_BaseColor", e.tint);
                m.SetTextureScale("_BaseMap", Vector2.one * e.scale);
                if (nor != null) { m.SetTexture("_BumpMap", nor); m.SetFloat("_BumpScale", e.bump); m.EnableKeyword("_NORMALMAP"); }
                if (ms != null) { m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness", 1f); m.SetFloat("_Metallic", 1f); m.SetFloat("_SmoothnessTextureChannel", 0f); }
                if (ao != null) { m.SetTexture("_OcclusionMap", ao); m.SetFloat("_OcclusionStrength", 1f); m.EnableKeyword("_OCCLUSIONMAP"); }
                EditorUtility.SetDirty(m); n++;
            }
            AssetDatabase.SaveAssets();
            return "materiales escaneados aplicados: " + n + (missing.Length > 0 ? " (faltan " + missing + ")" : "");
        }
    }
}
#endif
