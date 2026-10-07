#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Texturas procedurales del acido del segundo jefe (sin IA): charco con borde irregular, burbujas y brillo en el
    /// borde, y bola de acido veteada. Crea los PNG y los materiales ToxicPuddle (transparente) y ToxicGlob (opaco, emisivo).
    /// Menu: Horror/Texturas de acido. Idempotente; lo llama tambien PxlZombieKit.BuildBoss.
    /// </summary>
    public static class ToxicTextures
    {
        const string Dir = "Assets/_Project/Materials/";
        public const string PuddleMatPath = Dir + "ToxicPuddle.mat";
        public const string GlobMatPath = Dir + "ToxicGlob.mat";
        public const string RingMatPath = Dir + "DangerRing.mat";

        [MenuItem("Horror/Texturas de acido")]
        public static void Menu() { EnsureMaterials(); Debug.Log("Texturas de acido creadas"); }

        // smoothstep clasico (Mathf.SmoothStep de Unity interpola, no es la funcion de borde)
        static float Smooth(float e0, float e1, float x) { float k = Mathf.Clamp01((x - e0) / (e1 - e0)); return k * k * (3f - 2f * k); }

        static float Fbm(float x, float y, float seed)
        {
            float a = 0.5f, s = 0f, f = 1f;
            for (int i = 0; i < 5; i++) { s += a * Mathf.PerlinNoise(x * f + seed, y * f + seed * 1.7f); a *= 0.5f; f *= 2f; }
            return s;   // ~0..0.97
        }

        static Texture2D Save(string name, Texture2D tex)
        {
            string path = Dir + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D BuildPuddle()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var rnd = new System.Random(7);
            var bx = new float[16]; var by = new float[16]; var br = new float[16];
            for (int i = 0; i < bx.Length; i++)
            {
                float ang = (float)rnd.NextDouble() * 6.283f, rad = Mathf.Sqrt((float)rnd.NextDouble()) * 0.55f;
                bx[i] = Mathf.Cos(ang) * rad; by[i] = Mathf.Sin(ang) * rad; br[i] = 0.025f + (float)rnd.NextDouble() * 0.06f;
            }
            for (int py = 0; py < N; py++)
                for (int px = 0; px < N; px++)
                {
                    float u = px / (N - 1f) * 2f - 1f, v = py / (N - 1f) * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v), th = Mathf.Atan2(v, u);
                    float edge = 0.7f + (Mathf.PerlinNoise(Mathf.Cos(th) * 1.6f + 5f, Mathf.Sin(th) * 1.6f + 5f) - 0.5f) * 0.34f;
                    float a = 1f - Smooth(edge - 0.07f, edge, r);
                    if (a <= 0f) { tex.SetPixel(px, py, new Color(0.1f, 0.4f, 0.06f, 0f)); continue; }
                    float n = Fbm(u * 2.2f, v * 2.2f, 3f);
                    Color col = Color.Lerp(new Color(0.04f, 0.22f, 0.04f), new Color(0.22f, 0.62f, 0.08f), Mathf.Clamp01((n - 0.3f) * 2.2f));
                    float rim = Smooth(edge - 0.18f, edge - 0.04f, r);                 // borde mas claro y brillante
                    col = Color.Lerp(col, new Color(0.62f, 1f, 0.22f), rim * 0.85f);
                    for (int i = 0; i < bx.Length; i++)                                          // burbujas: aro claro con centro oscuro
                    {
                        float d = Mathf.Sqrt((u - bx[i]) * (u - bx[i]) + (v - by[i]) * (v - by[i]));
                        if (d < br[i]) col = Color.Lerp(col, new Color(0.85f, 1f, 0.55f), Smooth(br[i] * 0.55f, br[i], d) * 0.9f);
                    }
                    tex.SetPixel(px, py, new Color(col.r, col.g, col.b, a * (0.8f + 0.2f * rim)));
                }
            tex.Apply();
            return tex;
        }

        static Texture2D BuildGlob()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGB24, false);
            for (int py = 0; py < N; py++)
                for (int px = 0; px < N; px++)
                {
                    float u = px / (float)N * 4f, v = py / (float)N * 4f;
                    float n = Fbm(u, v, 11f);
                    float vein = 1f - Mathf.Abs(Fbm(u * 1.5f, v * 1.5f, 29f) * 2f - 1f);        // venas finas
                    vein = Mathf.Pow(vein, 4f);
                    Color col = Color.Lerp(new Color(0.06f, 0.32f, 0.05f), new Color(0.35f, 0.85f, 0.12f), Mathf.Clamp01(n * 1.6f - 0.2f));
                    col = Color.Lerp(col, new Color(0.8f, 1f, 0.45f), vein * 0.8f);
                    tex.SetPixel(px, py, col);
                }
            tex.Apply();
            return tex;
        }

        // circulo de aviso de los ataques en area: borde rojo brillante, relleno tenue y marcas radiales
        static Texture2D BuildRing()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int py = 0; py < N; py++)
                for (int px = 0; px < N; px++)
                {
                    float u = px / (N - 1f) * 2f - 1f, v = py / (N - 1f) * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    if (r > 1f) { tex.SetPixel(px, py, new Color(1f, 0.1f, 0.06f, 0f)); continue; }
                    float edge = Smooth(0.86f, 0.93f, r) * (1f - Smooth(0.97f, 1.0f, r));              // aro exterior
                    float inner = Smooth(0.5f, 0.54f, r) * (1f - Smooth(0.56f, 0.6f, r)) * 0.55f;        // aro intermedio
                    float ang = Mathf.Atan2(v, u);
                    float spokes = (Mathf.Abs(Mathf.Sin(ang * 6f)) > 0.97f ? 0.35f : 0f) * (r > 0.2f && r < 0.86f ? 1f : 0f);
                    float fill = 0.22f * (1f - r * 0.4f);
                    float a = Mathf.Clamp01(Mathf.Max(Mathf.Max(edge, inner), Mathf.Max(spokes, fill)));
                    tex.SetPixel(px, py, new Color(1f, 0.1f + 0.12f * edge, 0.06f, a));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Crea (o reutiliza) los dos materiales con sus texturas.</summary>
        public static void EnsureMaterials()
        {
            Directory.CreateDirectory(Dir);
            var puddleTex = Save("ToxicPuddle_tex", BuildPuddle());
            var globTex = Save("ToxicGlob_tex", BuildGlob());
            var lit = Shader.Find("Universal Render Pipeline/Lit");

            var puddle = AssetDatabase.LoadAssetAtPath<Material>(PuddleMatPath);
            if (puddle == null) { puddle = new Material(lit); AssetDatabase.CreateAsset(puddle, PuddleMatPath); }
            puddle.shader = lit;
            puddle.SetTexture("_BaseMap", puddleTex);
            puddle.SetTexture("_EmissionMap", puddleTex);
            puddle.SetColor("_BaseColor", Color.white);
            puddle.SetColor("_EmissionColor", new Color(0.45f, 0.45f, 0.45f));
            puddle.EnableKeyword("_EMISSION");
            puddle.SetFloat("_Smoothness", 0.3f);
            puddle.SetFloat("_Cull", 0f);                                       // se ve por los dos lados
            puddle.SetFloat("_Surface", 1f); puddle.SetFloat("_Blend", 0f);     // transparente, mezcla normal
            puddle.SetFloat("_SrcBlend", 5f); puddle.SetFloat("_DstBlend", 10f); puddle.SetFloat("_ZWrite", 0f);   // alpha normal: SrcAlpha / OneMinusSrcAlpha
            puddle.SetOverrideTag("RenderType", "Transparent");
            puddle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            puddle.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            puddle.SetFloat("_SpecularHighlights", 0f); puddle.SetFloat("_EnvironmentReflections", 0f);
            puddle.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); puddle.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            puddle.SetFloat("_BlendModePreserveSpecular", 1f);
            puddle.renderQueue = 3000;
            puddle.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(puddle);

            var glob = AssetDatabase.LoadAssetAtPath<Material>(GlobMatPath);
            if (glob == null) { glob = new Material(lit); AssetDatabase.CreateAsset(glob, GlobMatPath); }
            glob.shader = lit;
            glob.SetTexture("_BaseMap", globTex);
            glob.SetTexture("_EmissionMap", globTex);
            glob.SetColor("_BaseColor", Color.white);
            glob.SetColor("_EmissionColor", new Color(0.9f, 0.9f, 0.9f));
            glob.EnableKeyword("_EMISSION");
            glob.SetFloat("_Smoothness", 0.9f);
            EditorUtility.SetDirty(glob);
            var ringTex = Save("DangerRing_tex", BuildRing());
            var rm = AssetDatabase.LoadAssetAtPath<Material>(RingMatPath);
            if (rm == null) { rm = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(rm, RingMatPath); }
            rm.shader = Shader.Find("Universal Render Pipeline/Unlit");
            rm.SetTexture("_BaseMap", ringTex); rm.SetColor("_BaseColor", Color.white);
            rm.SetFloat("_Cull", 0f); rm.SetFloat("_Surface", 1f); rm.SetFloat("_Blend", 0f);
            rm.SetFloat("_SrcBlend", 5f); rm.SetFloat("_DstBlend", 10f); rm.SetFloat("_ZWrite", 0f);
            rm.SetOverrideTag("RenderType", "Transparent");
            rm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); rm.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            rm.renderQueue = 3000;
            EditorUtility.SetDirty(rm);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
