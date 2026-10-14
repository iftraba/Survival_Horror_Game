#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Horror.EditorTools
{
    /// <summary>
    /// Capa de suelo de la calle (fase 2 del lavado de cara del exterior): quads planos con las texturas de <c>Art/Textures/Street</c>
    /// (Tools/blender/build_street_decals.py) sobre el asfalto y las aceras: grietas, parches, manchas de aceite, charcos mojados, alcantarillas,
    /// rejillas de desague, pasos de cebra, papeles y marcas de frenada. Los llama <see cref="ComisariaGrandeStreet"/>; todo cuelga de su raiz.
    /// </summary>
    public static class ComisariaGrandeStreetDecals
    {
        const string Tex = "Assets/_Project/Art/Textures/Street/";
        const string Mats = "Assets/_Project/Materials/";
        const float Road = -0.036f, Walk = 0.012f;           // altura de un decal sobre el asfalto (cara de arriba en y -0,05) y sobre las aceras (cara de arriba en y 0)

        static Material Mat(string name, string tex, double smoothness, float tint = 1f)
        {
            string path = Mats + "Calle_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + tex + ".png");
            m.SetTexture("_BaseMap", t);
            m.SetColor("_BaseColor", new Color(tint, tint, tint, 1f));
            m.SetFloat("_Surface", 1f);                       // transparente
            m.SetFloat("_Blend", 0f);                         // mezcla alfa normal...
            m.SetFloat("_BlendModePreserveSpecular", 0f);     // ...sin conservar el brillo especular: si no, el reflejo del cielo se ve en las zonas transparentes (cuadrados grises)
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Smoothness", (float)smoothness);
            m.SetFloat("_Metallic", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Importers()
        {
            foreach (var f in System.IO.Directory.GetFiles(Tex, "*.png"))
            {
                var p = f.Replace('\\', '/');
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true;
                ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = true;
                ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.SaveAndReimport();
            }
        }

        static Transform parent;
        static int order;

        static void Quad(string name, Material m, float x, float z, float w, float h, float yaw, bool sidewalk)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name; go.transform.SetParent(parent);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(x, (sidewalk ? Walk : Road) + (order++ % 40) * 0.0004f, z);     // cada decal un poco mas alto que el anterior: sin parpadeo entre ellos
            go.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
            go.isStatic = true;
        }

        public static void Build(Transform root)
        {
            Importers();
            var g = new GameObject("Suelo_Detalle"); g.transform.SetParent(root); parent = g.transform; order = 0;
            var rng = new System.Random(33);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var crack = new[] { Mat("Grieta_A", "street_crack_a", 0.2), Mat("Grieta_B", "street_crack_b", 0.2), Mat("Grieta_C", "street_crack_c", 0.2) };
            var patch = Mat("Parche", "street_patch", 0.25);
            var oil = Mat("Aceite", "street_oil", 0.75);
            var puddle = Mat("Charco", "street_puddle", 0.4, 0.3f);
            var manhole = Mat("Alcantarilla", "street_manhole", 0.3);
            var drain = Mat("Desague", "street_drain", 0.3);
            var zebra = Mat("PasoCebra", "street_zebra", 0.2);
            var litter = Mat("Papeles", "street_litter", 0.1);
            var skid = Mat("Frenada", "street_skid", 0.3);

            for (int i = 0; i < 26; i++) Quad("Grieta", crack[i % 3], R(-82f, 82f), R(-17.6f, -10.4f), R(3f, 5.5f), R(3f, 5.5f), R(0f, 360f), false);
            for (int i = 0; i < 6; i++) Quad("Grieta_Acera", crack[(i + 1) % 3], R(-60f, 60f), i % 2 == 0 ? R(-9.7f, -8.3f) : R(-19.7f, -18.3f), R(2.5f, 4f), R(2.5f, 4f), R(0f, 360f), true);
            foreach (var p in new[] { (-70f, -15f), (-42f, -12.5f), (-22f, -16.5f), (4f, -11.5f), (30f, -14.5f), (58f, -12f) })
                Quad("Parche", patch, p.Item1, p.Item2, R(2.5f, 4.2f), R(2f, 3.4f), R(-6f, 6f) + (rng.Next(2) == 0 ? 0f : 90f), false);
            for (int i = 0; i < 9; i++) Quad("Aceite", oil, R(-80f, 80f), R(-17.4f, -10.6f), R(1.2f, 2.6f), R(1.2f, 2.6f), R(0f, 360f), false);
            for (int i = 0; i < 9; i++) Quad("Charco", puddle, R(-80f, 80f), R(-17.6f, -10.4f), R(2.2f, 4.6f), R(2.2f, 4.6f), R(0f, 360f), false);
            foreach (var p in new[] { (-66f, -13.2f), (-30f, -15.5f), (-8f, -12.4f), (12f, -14.8f), (34f, -11.8f), (64f, -16.5f) }) Quad("Alcantarilla", manhole, p.Item1, p.Item2, 0.95f, 0.95f, R(0f, 360f), false);
            for (float x = -72f; x <= 72f; x += 24f) Quad("Desague", drain, x + R(-2f, 2f), -10.3f, 0.9f, 0.45f, 0f, false);
            for (float x = -54f; x <= 66f; x += 30f) Quad("Desague", drain, x + R(-2f, 2f), -17.7f, 0.9f, 0.45f, 180f, false);
            foreach (float x in new[] { -6f, 38f }) Quad("PasoCebra", zebra, x, -14f, 3.6f, 8f, 0f, false);
            for (int i = 0; i < 14; i++) Quad("Papeles", litter, R(-75f, 75f), i % 3 == 0 ? R(-9.8f, -8.2f) : i % 3 == 1 ? R(-19.8f, -18.2f) : R(-17.5f, -10.5f), 3f, 3f, R(0f, 360f), i % 3 != 2);
            foreach (var p in new[] { (-26f, -13f, 4f), (18f, -16f, -8f), (50f, -14.5f, 175f), (-58f, -11.5f, 6f) }) Quad("Frenada", skid, p.Item1, p.Item2, 7f, 1.75f, p.Item3, false);
        }
    }
}
#endif
