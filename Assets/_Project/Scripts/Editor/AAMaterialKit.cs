#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Calidad AA, fase C (menu Horror/Calidad AA/Muebles AA): modelos hechos con Tools/blender/aa_lib.py. Cada FBX trae una submalla por material AA_*
    /// con UV en metros, y aqui se les ponen materiales reales con texturas escaneadas de Poly Haven (Art/Textures/PBR) en vez de hornear una textura por
    /// modelo: chapa pintada (verde o gris neutro tenido), tapiceria, madera, metales y plasticos. Copia los FBX de Tools/raw_generated/furniture_aa sobre
    /// los anteriores (mismo .meta, asi la escena no pierde las referencias) y reasigna los materiales en el importador. Repetible.
    /// </summary>
    public static class AAMaterialKit
    {
        const string PBR = "Assets/_Project/Art/Textures/PBR/";
        const string MatDir = "Assets/_Project/Materials/AA/";
        const string Raw = "Tools/raw_generated/furniture_aa/";

        /// <summary>Modelos AA: nombre del FBX generado y ruta en el proyecto.</summary>
        public static readonly (string name, string path)[] Models =
        {
            ("Locker", "Assets/_Project/Art/Props/Locker.fbx"),
            ("FilingCabinet", "Assets/_Project/Art/Props/FilingCabinet.fbx"),
            ("Desk", "Assets/_Project/Art/Props/Desk.fbx"),
            ("SwivelChair", "Assets/_Project/Art/Props/Office/SwivelChair.fbx"),
            ("ReceptionDesk", "Assets/_Project/Art/Props/Exterior/ReceptionDesk.fbx"),
            ("SpiralStairAA", SpiralPath),
            ("CeilingLampAA", LampPath),
            ("DeskComputer", "Assets/_Project/Art/Props/Office/DeskComputer.fbx"),
            ("DeskLamp", "Assets/_Project/Art/Props/Office/DeskLamp.fbx"),
            ("TrashBin", "Assets/_Project/Art/Props/Office/TrashBin.fbx"),
            ("Chair", "Assets/_Project/Art/Props/Chair.fbx"),
            ("Bookcase", "Assets/_Project/Art/Props/Office/Bookcase.fbx"),
            ("AuditoriumChair", "Assets/_Project/Art/Props/Office/AuditoriumChair.fbx"),
            ("WaitingBench", "Assets/_Project/Art/Props/Exterior/WaitingBench.fbx"),
        };

        public const string LampPath = "Assets/_Project/Art/Props/Lamp/CeilingLampAA.fbx";
        public const string SpiralPath = "Assets/_Project/Art/Props/Station/SpiralStairAA.fbx";

        public static bool IsAA(string fbxPath) => Models.Any(m => m.path == fbxPath);

        // material: textura PBR (null = liso), color de la textura (_diff o _neutral_diff), tinte, escala de UV, intensidad de normal, metal, suavidad
        static readonly Dictionary<string, (string tex, string diff, Color tint, float scale, float bump, float metal, float smooth)> Defs =
            new Dictionary<string, (string, string, Color, float, float, float, float)>
        {
            { "AA_PinturaVerde", ("green_metal_rust", "_diff", new Color(0.92f, 0.95f, 0.92f), 1.0f, 1.0f, 0f, 1f) },
            { "AA_PinturaGris", ("green_metal_rust", "_neutral_diff", new Color(0.72f, 0.7f, 0.64f), 1.0f, 1.0f, 0f, 1f) },   // gris calido (la neutra tira a azul)
            { "AA_PinturaCrema", ("green_metal_rust", "_neutral_diff", new Color(0.9f, 0.85f, 0.7f), 1.0f, 1.0f, 0f, 1f) },
            { "AA_Tela", ("poly_wool_herringbone", "_diff", new Color(0.4f, 0.41f, 0.45f), 2.0f, 1.0f, 0f, 1f) },
            { "AA_Madera", ("dark_wood", "_diff", new Color(0.62f, 0.5f, 0.45f), 1.2f, 0.6f, 0f, 0.6f) },
            { "AA_Laminado", ("dark_wood", "_diff", new Color(0.5f, 0.5f, 0.47f), 0.8f, 0.25f, 0f, 0.45f) },   // laminado imitacion nogal (sin el rojo de la madera)
            { "AA_Laton", (null, null, new Color(0.62f, 0.47f, 0.22f), 1f, 0f, 1f, 0.62f) },
            { "AA_Acero", (null, null, new Color(0.62f, 0.63f, 0.65f), 1f, 0f, 1f, 0.5f) },
            { "AA_Cromo", (null, null, new Color(0.78f, 0.78f, 0.8f), 1f, 0f, 1f, 0.82f) },
            { "AA_Negro", (null, null, new Color(0.04f, 0.04f, 0.045f), 1f, 0f, 0.2f, 0.35f) },
            { "AA_Plastico", (null, null, new Color(0.05f, 0.05f, 0.055f), 1f, 0f, 0f, 0.42f) },
            { "AA_Goma", (null, null, new Color(0.025f, 0.025f, 0.028f), 1f, 0f, 0f, 0.15f) },
            { "AA_Papel", (null, null, new Color(0.78f, 0.75f, 0.66f), 1f, 0f, 0f, 0.08f) },
            { "AA_Hierro", ("green_metal_rust", "_neutral_diff", new Color(0.17f, 0.18f, 0.17f), 1.0f, 1.0f, 0f, 1f) },   // fundicion pintada de negro
            { "AA_TelaAzul", ("poly_wool_herringbone", "_diff", new Color(0.22f, 0.28f, 0.55f), 2.0f, 1.0f, 0f, 1f) },     // butacas del auditorio
            { "AA_LibroRojo", ("poly_wool_herringbone", "_diff", new Color(0.72f, 0.22f, 0.17f), 6.0f, 0.5f, 0f, 1f) },    // tela de encuadernar (la textura de lana es oscura: tintes altos)
            { "AA_LibroAzul", ("poly_wool_herringbone", "_diff", new Color(0.27f, 0.35f, 0.58f), 6.0f, 0.5f, 0f, 1f) },
            { "AA_LibroVerde", ("poly_wool_herringbone", "_diff", new Color(0.27f, 0.46f, 0.3f), 6.0f, 0.5f, 0f, 1f) },
            { "AA_LibroMarron", ("poly_wool_herringbone", "_diff", new Color(0.6f, 0.42f, 0.28f), 6.0f, 0.5f, 0f, 1f) },
            { "AA_LibroCrema", ("poly_wool_herringbone", "_diff", new Color(0.85f, 0.8f, 0.68f), 6.0f, 0.5f, 0f, 1f) },
            { "AA_Carton", (null, null, new Color(0.45f, 0.34f, 0.21f), 1f, 0f, 0f, 0.12f) },
            { "AA_Pantalla", (null, null, new Color(0.015f, 0.018f, 0.022f), 1f, 0f, 0f, 0.88f) },     // pantalla apagada, cristal brillante
        };

        static Texture2D Tex(string file, bool normal, bool srgb)
        {
            string p = PBR + file;
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) return null;
            bool changed = false;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ti.textureType != type) { ti.textureType = type; changed = true; }
            if (ti.sRGBTexture != srgb) { ti.sRGBTexture = srgb; changed = true; }
            if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.CompressedHQ) { ti.textureCompression = TextureImporterCompression.CompressedHQ; changed = true; }
            if (ti.anisoLevel != 8) { ti.anisoLevel = 8; changed = true; }
            if (file.EndsWith("_ms.png") && ti.alphaSource != TextureImporterAlphaSource.FromInput) { ti.alphaSource = TextureImporterAlphaSource.FromInput; changed = true; }
            if (changed) ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        public static Material Get(string name)
        {
            if (!Directory.Exists(MatDir)) Directory.CreateDirectory(MatDir);
            string path = MatDir + name + ".mat";
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            m.shader = lit;
            if (!Defs.TryGetValue(name, out var d)) d = (null, null, new Color(0.5f, 0.5f, 0.5f), 1f, 0f, 0f, 0.4f);
            m.SetColor("_BaseColor", d.tint);
            Texture2D col = null, nor = null, ms = null, ao = null;
            if (d.tex != null)
            {
                col = Tex(d.tex + d.diff + ".jpg", false, true);
                nor = Tex(d.tex + "_nor.jpg", true, false);
                ms = Tex(d.tex + "_ms.png", false, false);
                ao = Tex(d.tex + "_ao.jpg", false, false);
            }
            m.SetTexture("_BaseMap", col);
            m.SetTextureScale("_BaseMap", Vector2.one * d.scale);
            m.SetTexture("_BumpMap", nor);
            if (nor != null) { m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", d.bump); } else m.DisableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", ms);
            if (ms != null) { m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_SmoothnessTextureChannel", 0f); m.SetFloat("_Metallic", 1f); m.SetFloat("_Smoothness", d.smooth); }
            else { m.DisableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Metallic", d.metal); m.SetFloat("_Smoothness", d.smooth); }
            m.SetTexture("_OcclusionMap", ao);
            if (ao != null) { m.EnableKeyword("_OCCLUSIONMAP"); m.SetFloat("_OcclusionStrength", 0.8f); } else m.DisableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("Horror/Calidad AA/Muebles AA (taquilla, archivador, escritorio, silla)")]
        public static void Menu() { Debug.Log("[Horror] " + Apply()); }

        public static string Apply()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var log = new List<string>();
            string root = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/') + "/";
            foreach (var (name, path) in Models)
            {
                string src = root + Raw + name + ".fbx";
                if (File.Exists(src)) { File.Copy(src, root + path, true); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); }
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) { log.Add("falta " + path); continue; }
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                if (name == "SpiralStairAA" || name == "CeilingLampAA") imp.generateSecondaryUV = true;                // estatica: UV2 para la luz horneada
                foreach (var old in imp.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material) && !k.name.StartsWith("AA_")).ToList()) imp.RemoveRemap(old);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);      // los materiales del modelo (los suyos o ya reasignados: se llaman igual)
                var names = go == null ? new List<string>() : go.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(x => x != null)
                    .Select(x => x.name).Where(n => n.StartsWith("AA_")).Distinct().ToList();
                foreach (var n in names) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), Get(n));
                imp.SaveAndReimport();
                log.Add(name + " (" + string.Join(", ", names) + ")");
            }
            AssetDatabase.SaveAssets();
            var esc = GameObject.Find("EscaleraCaracol");
            if (esc != null) log.Add(SpiralSwap(esc.transform));
            if (GameObject.Find("--- COMISARIA V2 ---") != null) log.Add(ComisariaGrandeLampDetail.Build());
            return "muebles AA: " + string.Join(" | ", log);
        }

        /// <summary>
        /// Escalera de caracol del vestibulo: quita lo que se ve de la de primitivas (peldanos, pies derechos, barrotes, pasamanos y la malla de la
        /// columna, que conserva su colision) y pone el modelo AA en el centro. La rampa de colision, el rellano y las barandillas del hueco se quedan.
        /// La llama tambien ComisariaGrande.SpiralStair (menu 1). Repetible.
        /// </summary>
        public static string SpiralSwap(Transform root)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpiralPath);
            if (prefab == null) return "caracol AA: falta el modelo";
            int removed = 0;
            foreach (Transform t in root.Cast<Transform>().ToList())
            {
                if (t.name == "Peldano" || t.name == "PieDerecho" || t.name == "Barrote" || t.name == "Pasamanos" || t.name == "CaracolAA") { Object.DestroyImmediate(t.gameObject); removed++; }
                else if (t.name == "Columna") { var r = t.GetComponent<MeshRenderer>(); if (r != null) Object.DestroyImmediate(r); }
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            go.name = "CaracolAA";
            go.transform.position = ComisariaGrande.SpiralC;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)~0);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            return "caracol AA puesto (" + removed + " piezas de primitivas quitadas)";
        }
    }
}
#endif
