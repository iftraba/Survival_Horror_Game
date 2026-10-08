#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Aplica las texturas horneadas en Blender (Tools/blender/texture_items.py) a los objetos recogibles. Menu: Horror/Aplicar
    /// texturas de objetos. Repetible.
    /// - Objetos hechos en Blender (riñonera, tarjeta, llaves): su FBX reexportado trae un unico material "Nombre_baked"; se crea
    ///   Art/Textures/Items/Nombre_baked.mat (color, metal/suavidad, normales) y se reasigna en el importador del FBX.
    /// - Modelos de Meshy (pistola, escopeta, cajas de municion, spray): se anaden normales y metal/suavidad a su material; la
    ///   escopeta usa ademas el color con la madera recoloreada.
    /// </summary>
    public static class ItemTextureKit
    {
        const string T = "Assets/_Project/Art/Textures/Items/";
        const string TP = "Assets/_Project/Art/Textures/Props/";   // muebles

        static readonly (string fbx, string name)[] Baked =
        {
            ("Assets/_Project/Art/Props/Rinonera.fbx", "Rinonera"),
            ("Assets/_Project/Art/Weapons/KeyCard.fbx", "KeyCard"),
            ("Assets/_Project/Art/Weapons/Key.fbx", "Key"),
            ("Assets/_Project/Art/Weapons/KeyMaster.fbx", "KeyMaster"),
            ("Assets/_Project/Art/Weapons/KeyGarage.fbx", "KeyGarage"),
        };

        /// <summary>Muebles (mismo metodo: madera con veta, chapa pintada desconchada, oxido, carton, tela).</summary>
        static readonly string[] Props = { "Desk", "Chair", "Locker", "Shelf", "FilingCabinet", "Cot", "Crate", "Barrel" };

        /// <summary>Atrezzo de pared (Tools/blender/build_wall_props.py), en Art/Props/Wall.</summary>
        /// <summary>Piezas de puerta (Tools/blender/build_doors.py), en Art/Props/Door: las monta DoorModelKit.</summary>
        public static readonly string[] DoorParts = { "DoorLeaf", "DoorJamb", "DoorHeader" };

        public static readonly string[] WallProps = { "WallRadiator", "WallExtinguisher", "WallElectricPanel", "WallNoticeBoard", "WallVent", "WallClock", "WallPipeRun" };

        static readonly (string mat, string name, bool baseColor)[] Meshy =
        {
            ("Assets/_Project/Art/Generated/MeshyPistolLow_mat.mat", "Pistol", false),
            ("Assets/_Project/Art/Generated/MeshyShotgunLow_mat.mat", "Shotgun", true),
            ("Assets/_Project/Art/Generated/MeshyAmmoBoxLow_mat.mat", "AmmoBox", false),
            ("Assets/_Project/Art/Generated/MeshyShellBoxLow_mat.mat", "ShellBox", false),
            ("Assets/_Project/Art/Generated/MeshySprayLow_mat.mat", "Spray", false),
        };

        static string dir = T;
        static Texture2D Tex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(dir + file);

        static void Importers()
        {
            foreach (var path in Directory.GetFiles(T, "*.png").Concat(Directory.Exists(TP) ? Directory.GetFiles(TP, "*.png") : new string[0]))
            {
                var p = path.Replace('\\', '/');
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                if (ti == null) continue;
                bool normal = p.EndsWith("_Normal.png"), ms = p.EndsWith("_MetallicSmoothness.png");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !(normal || ms);
                ti.alphaSource = ms ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                ti.alphaIsTransparency = false;
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
            }
        }

        static void SetMaps(Material m, string name, bool baseColor)
        {
            if (baseColor) { var bc = Tex(name + "_BaseColor.png"); if (bc != null) m.SetTexture("_BaseMap", bc); }
            var ms = Tex(name + "_MetallicSmoothness.png");
            if (ms != null)
            {
                m.SetTexture("_MetallicGlossMap", ms);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_SmoothnessTextureChannel", 0f);     // suavidad en el alfa del mapa de metal
                m.SetFloat("_Smoothness", 1f);                   // con mapa, multiplica el alfa
                m.SetFloat("_Metallic", 1f);
            }
            var n = Tex(name + "_Normal.png");
            if (n != null) { m.SetTexture("_BumpMap", n); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            EditorUtility.SetDirty(m);
        }

        [MenuItem("Horror/Aplicar texturas de objetos")]
        public static void Menu() { Debug.Log("[Horror] " + Apply()); }

        public static string Apply()
        {
            Importers();
            var log = new List<string>();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var all = Baked.Select(b => (b.fbx, b.name, T)).Concat(Props.Select(n => ("Assets/_Project/Art/Props/" + n + ".fbx", n, TP)))
                .Concat(WallProps.Select(n => ("Assets/_Project/Art/Props/Wall/" + n + ".fbx", n, TP)))
                .Concat(DoorParts.Select(n => ("Assets/_Project/Art/Props/Door/" + n + ".fbx", n, TP)));
            foreach (var (fbx, name, folder) in all)
            {
                dir = folder;
                string mp = folder + name + "_baked.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, mp); }
                m.shader = lit;
                m.SetColor("_BaseColor", Color.white);
                SetMaps(m, name, true);
                var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
                if (imp == null) { log.Add("falta " + fbx); continue; }
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name + "_baked"), m);
                imp.SaveAndReimport();
                log.Add(name);
            }
            dir = T;
            foreach (var (mat, name, bc) in Meshy)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(mat);
                if (m == null) { log.Add("falta " + mat); continue; }
                SetMaps(m, name, bc);
                log.Add(name);
            }
            AssetDatabase.SaveAssets();
            return "texturas de objetos aplicadas: " + string.Join(", ", log);
        }
    }
}
#endif
