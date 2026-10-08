#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Zombi "Oficial": policia no muerto generado con Meshy (50.000 caras, ver art-pipeline.md), riggeado en Mixamo y
    /// montado con las mismas animaciones de Mixamo que el resto de zombis. Crea:
    /// - Zombie_Oficial: zombi de pie, resistente y de golpe fuerte (mismas animaciones que el Cop).
    /// - Zombie_OficialReptante: el mismo modelo arrastrandose (clips Crawl / Running Crawl): bajo, rapido y fragil.
    /// El material usa la textura de color y el mapa de normales de Meshy (el normal devuelve el detalle perdido al decimar).
    /// Menu: Horror/Construir zombi Oficial. Repetible.
    /// </summary>
    public static class OficialZombieKit
    {
        const string M = "Assets/_Project/Art/Mixamo/";
        const string FbxPath = M + "Characters/Zombie_Oficial.fbx";
        const string MatPath = M + "Materials/Oficial_body.mat";
        const string AnimDir = "Assets/_Project/Animation/";
        const float TargetHeight = 1.85f;

        static void TextureSettings()
        {
            foreach (var (path, normal) in new[] { (M + "Textures/Oficial_basecolor.png", false), (M + "Textures/Oficial_normal.png", true) })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                if (ti == null) continue;
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal;
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.SaveAndReimport();
            }
        }

        static Material BuildMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, MatPath); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(M + "Textures/Oficial_basecolor.png"));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(M + "Textures/Oficial_normal.png"));
            mat.EnableKeyword("_NORMALMAP");
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Altura (m) del modelo a escala 1, medida con los renderers de una instancia.</summary>
        static float ModelHeight(GameObject model)
        {
            var inst = Object.Instantiate(model);
            try
            {
                var rs = inst.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) return 1.8f;
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                return b.size.y;
            }
            finally { Object.DestroyImmediate(inst); }
        }

        [MenuItem("Horror/Construir zombi Oficial")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "ZombieHumanoid.controller");
            if (baseCtrl == null) return "falta ZombieHumanoid.controller (ejecuta Horror/Construir zombis y jefe)";
            TextureSettings();
            var mat = BuildMaterial();
            AssetDatabase.SaveAssets();
            var log = new List<string> { MixamoImport.ConfigureCharacter(FbxPath, MatPath) };
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (model == null) return "falta " + FbxPath;
            float h = ModelHeight(model);
            float scale = h > 0.1f ? TargetHeight / h : 1f;
            log.Add("altura nativa " + h.ToString("F2") + " m -> escala " + scale.ToString("F3"));

            // ---- de pie: mismas animaciones que el Cop
            var attacks = new[] { "Z_ZombieNeckBite", "Z_ZombieBiting", "G_ZombiePunching2" };
            var oc = ZombieKit.Override("Zombie_Oficial", baseCtrl, new Dictionary<string, string>
            {
                ["Z_ZombieIdle"] = "G_ZombieScratchIdle", ["Z_ZombieWalk"] = "Z_ZombieWalk", ["Z_ZombieRun"] = "Z_ZombieRun",
                ["Z_ZombieAttack"] = attacks[0], ["Z_ZombieBiting"] = attacks[1], ["Z_ZombieNeckBite"] = attacks[2],
                ["G_ZombieReactionHit"] = "G_ZombieReactionHit", ["Z_ZombieDying"] = "Z_ZombieDying",
            });
            var variants = Variants(model, attacks, 1.4f);
            var prefab = ZombieKit.BuildPrefab(new ZombieKit.Spec
            {
                name = "Zombie_Oficial", fbxPath = FbxPath, scale = scale, controller = oc, height = 2f, radius = 0.4f,
                hp = ZombieKit.GenericHp, torsoMult = ZombieKit.PoliceTorsoMult, chase = ZombieKit.BaseChase, damage = 22f, attackRange = 1.6f, cooldown = 1.4f, stagger = 0.5f, alertTime = 1.6f,
                walkClip = 0.33f, runClip = 2.84f, runAbove = 1.6f, variants = variants,
            });
            Recolor(prefab, mat);
            log.Add("Zombie_Oficial listo");

            // ---- reptante: bajo y rapido (clips de arrastrarse; los ataques son mordiscos)
            var crawlAttacks = new[] { "Z_ZombieBiting", "Z_ZombieBiting2", "Z_ZombieNeckBite" };
            var oc2 = ZombieKit.Override("Zombie_OficialReptante", baseCtrl, new Dictionary<string, string>
            {
                ["Z_ZombieIdle"] = "Z_ZombieCrawl", ["Z_ZombieWalk"] = "Z_Crawling", ["Z_ZombieRun"] = "Z_RunningCrawl",
                ["Z_ZombieAttack"] = crawlAttacks[0], ["Z_ZombieBiting"] = crawlAttacks[1], ["Z_ZombieNeckBite"] = crawlAttacks[2],
                ["G_ZombieReactionHit"] = "Z_ZombieCrawl", ["Z_ZombieScream"] = "Z_ZombieCrawl", ["Z_ZombieDying"] = "Z_ZombieDying",
            });
            var variants2 = Variants(model, crawlAttacks, 1.2f);
            // los clips de arrastrarse de Mixamo son 'en el sitio' (averageSpeed = 0): velocidad de zancada estimada a mano
            float cw = 0.5f, cr = 1.6f;
            var prefab2 = ZombieKit.BuildPrefab(new ZombieKit.Spec
            {
                name = "Zombie_OficialReptante", fbxPath = FbxPath, scale = scale, controller = oc2, height = 0.9f, radius = 0.45f,
                hp = ZombieKit.GenericHp, torsoMult = ZombieKit.PoliceTorsoMult, chase = ZombieKit.BaseChase, damage = 14f, attackRange = 1.4f, cooldown = 1.2f, stagger = 0.4f, alertTime = 0.8f,
                walkClip = cw, runClip = cr, runAbove = 0.9f, variants = variants2,
                headRadius = 0.17f, torsoRadius = 0.25f, limbRadiusScale = 1.2f,
            });
            Recolor(prefab2, mat);
            log.Add("Zombie_OficialReptante listo [crawl " + cw.ToString("F2") + " m/s, running crawl " + cr.ToString("F2") + " m/s]");
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }

        static ZombieAI.AttackVariant[] Variants(GameObject model, string[] attacks, float cooldownMin)
        {
            var list = new List<ZombieAI.AttackVariant>();
            foreach (var a in attacks)
            {
                var clip = ZombieKit.Clip(a);
                float eff = clip.length * 0.62f / 1.5f;                // el estado reproduce a 1.5x y sale al 62 %
                float impact = ZombieKit.ImpactTime(model, clip) / 1.5f;
                list.Add(new ZombieAI.AttackVariant { hitDelay = Mathf.Clamp(impact, 0.2f, eff), damageMultiplier = 1f, cooldown = Mathf.Max(cooldownMin, eff + 0.35f) });
            }
            return list.ToArray();
        }

        // El FBX de Mixamo trae su propio material: el prefab usa el del proyecto (textura de color + normales)
        static void Recolor(GameObject prefabAsset, Material mat)
        {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) ms[i] = mat;
                r.sharedMaterials = ms;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
