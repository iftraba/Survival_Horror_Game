#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Mismas animaciones para todos los zombis de pie (2026-10-08, menu Horror/Unificar zombis). Antes cada tipo tenia su reposo,
    /// su andar y sus ataques (la chica andaba con un paseo normal acelerado a casi 2,5x y se veia raro). Ahora todos usan el mismo
    /// juego y la misma velocidad de clip; las unicas diferencias son el chaleco de los policias (torso) y las reacciones que
    /// salen por probabilidad (derribo, aturdimiento, agarre, muertes). Tambien quita de la escena los zombis con la piel del
    /// segundo jefe (Pxl3) y pone otros modelos en su sitio. Repetible.
    /// </summary>
    public static class ZombieUnifyKit
    {
        const string AnimDir = "Assets/_Project/Animation/";
        const string PrefabDir = "Assets/_Project/Prefabs/Characters/";

        /// <summary>Juego comun: clip base del controlador -> clip de Mixamo.</summary>
        public static readonly Dictionary<string, string> Common = new Dictionary<string, string>
        {
            // el juego de los policias (el que funcionaba bien): el del civil (andar tambaleante) se descarto
            ["Z_ZombieIdle"] = "G_ZombieScratchIdle", ["Z_ZombieWalk"] = "Z_ZombieWalk", ["Z_ZombieRun"] = "Z_ZombieRun",
            ["Z_ZombieAttack"] = "Z_ZombieNeckBite", ["Z_ZombieBiting"] = "Z_ZombieBiting", ["Z_ZombieNeckBite"] = "G_ZombiePunching2",
            ["G_ZombieReactionHit"] = "G_ZombieReactionHit", ["Z_ZombieScream"] = "Z_ZombieScream", ["Z_ZombieDying"] = "Z_ZombieDying",
        };
        /// <summary>Ataques comunes (variantes 0-4: estados Attack0-4 del controlador base).</summary>
        static readonly string[] Attacks = { "Z_ZombieNeckBite", "Z_ZombieBiting", "G_ZombiePunching2", "Z_ZombieKick", "Z_ZombieAttack2" };
        const float WalkClip = 0.33f, RunClip = 2.84f, Damage = 15f;

        /// <summary>Juego de reptante comun (cualquier modelo): arrastrarse, mordiscos desde el suelo.</summary>
        static readonly Dictionary<string, string> Crawl = new Dictionary<string, string>
        {
            ["Z_ZombieIdle"] = "Z_ZombieCrawl", ["Z_ZombieWalk"] = "Z_Crawling", ["Z_ZombieRun"] = "Z_RunningCrawl",
            ["Z_ZombieAttack"] = "Z_ZombieBiting", ["Z_ZombieBiting"] = "Z_ZombieBiting2", ["Z_ZombieNeckBite"] = "Z_ZombieNeckBite",
            ["G_ZombieReactionHit"] = "Z_ZombieCrawl", ["Z_ZombieScream"] = "Z_ZombieCrawl", ["Z_ZombieDying"] = "Z_ZombieDying",
        };
        static readonly string[] CrawlAttacks = { "Z_ZombieBiting", "Z_ZombieBiting2", "Z_ZombieNeckBite" };

        static ZombieAI.AttackVariant[] Variants(GameObject model, string[] clips, float minCooldown)
        {
            var v = new List<ZombieAI.AttackVariant>();
            foreach (var a in clips)
            {
                var clip = ZombieKit.Clip(a);
                float eff = clip.length * 0.62f / 1.5f;                      // los estados de ataque van a 1,5x y salen al 62 %
                float impact = ZombieKit.ImpactTime(model, clip) / 1.5f;
                v.Add(new ZombieAI.AttackVariant { hitDelay = Mathf.Clamp(impact, 0.2f, eff), damageMultiplier = 1f, cooldown = Mathf.Max(minCooldown, eff + 0.35f) });
            }
            return v.ToArray();
        }

        // prefab -> override (el carronero conserva su reposo y su golpe: el bucle de comerse el cadaver)
        static readonly (string prefab, string over, bool feeder)[] Types =
        {
            ("Zombie_Civil", "Zombie_Civil", false), ("Zombie_Girl", "Zombie_Girl", false), ("Zombie_Cop", "Zombie_Cop", false),
            ("Zombie_Yaku", "Zombie_Yaku", false), ("Zombie_Oficial", "Zombie_Oficial", false), ("Zombie_Carronero", "Zombie_Carronero", true),
            ("Zombie_Pxl1", "Zombie_Pxl1", false), ("Zombie_Pxl2", "Zombie_Pxl2", false), ("Zombie_Pxl3", "Zombie_Pxl3", false),
        };

        // zombis con la piel del segundo jefe en la escena -> otro modelo
        static readonly (string oldName, string prefab, string newName)[] Swaps =
        {
            ("Zombie_Pxl3_Barricada", "Zombie_Civil", "Zombie_Civil_Barricada"),
            ("Z2_Pxl3_Maquinas", "Zombie_Civil", "Z2_Civil_Maquinas"),
            ("Z2_Pxl3_Almacen", "Zombie_Girl", "Z2_Girl_Almacen"),
        };

        [MenuItem("Horror/Unificar zombis (mismas animaciones, sin los Pxl3)")]
        public static void Menu() { Debug.Log("[Horror] " + Apply()); }

        public static string Apply()
        {
            var log = new List<string>();
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "ZombieHumanoid.controller");
            if (baseCtrl == null) return "falta ZombieHumanoid.controller";
            var crawlOc = ZombieKit.Override("Zombie_CrawlCommon", baseCtrl, Crawl);
            var feedClip = ZombieKit.Clip("Z_ZombieBiting2");
            var corpse = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "Cadaver_Civil.prefab");
            var blood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/BloodPool.mat");
            foreach (var (prefab, over, feeder) in Types)
            {
                var map = new Dictionary<string, string>(Common);
                if (feeder) { map["Z_ZombieIdle"] = "Z_ZombieBiting2"; map["G_ZombieReactionHit"] = "Z_ZombieBiting2"; }
                ZombieKit.Override(over, baseCtrl, map);

                string path = PrefabDir + prefab + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                var ai = root.GetComponent<ZombieAI>();
                var za = root.GetComponent<ZombieAnimation>();
                var m = root.transform.Find("Model");
                var model = m != null ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(m.gameObject) : null;
                float scale = m != null ? m.localScale.y : 1f;
                if (ai != null && model != null)
                {
                    ai.attackVariants = Variants(model, Attacks, 1.4f);
                    // cualquier zombi puede arrastrarse (2 tiros en las piernas, o 'startCrawling' en la escena) y empezar comiendo
                    var cr = root.GetComponent<ZombieCripple>() ?? root.AddComponent<ZombieCripple>();
                    cr.crawlController = crawlOc; cr.crawlVariants = Variants(model, CrawlAttacks, 1.2f);
                    cr.legHitsToCripple = 2; cr.fallTime = 1.0f; cr.crawlChase = ZombieKit.BaseChase;
                    var fd = root.GetComponent<ZombieFeeding>() ?? root.AddComponent<ZombieFeeding>();
                    fd.feedClip = feedClip; fd.corpsePrefab = corpse; fd.bloodMaterial = blood;
                    ai.attackDamage = Damage;
                    ai.alertTime = 1.6f;
                    ai.staggerTime = 0.5f;
                    ai.chaseSpeed = ZombieKit.BaseChase;
                }
                if (za != null)
                {
                    za.walkClipSpeed = WalkClip * scale; za.runClipSpeed = RunClip * scale; za.runAbove = 1.6f; za.maxWalkPlayback = 2.2f;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                log.Add(prefab);
            }
            AssetDatabase.SaveAssets();

            // escena: fuera los Pxl3 (misma piel que el segundo jefe)
            var zr = GameObject.Find("Zombies");
            int swapped = 0;
            foreach (var (oldName, prefab, newName) in Swaps)
            {
                var old = GameObject.Find(oldName);
                if (old == null) continue;
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + prefab + ".prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, zr != null ? zr.transform : old.transform.parent);
                go.name = newName;
                go.transform.SetPositionAndRotation(old.transform.position, old.transform.rotation);
                Object.DestroyImmediate(old);
                swapped++;
            }
            // los dos de ambiente pasan a ser zombis normales con la opcion marcada (comiendo / arrastrandose)
            string pose = "";
            var feederOld = GameObject.Find("X_Carronero");
            if (feederOld != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "Zombie_Girl.prefab"), feederOld.transform.parent);
                go.name = "X_Comiendo";
                go.transform.SetPositionAndRotation(feederOld.transform.position, feederOld.transform.rotation);
                var fd = go.GetComponent<ZombieFeeding>(); fd.feeding = true; fd.spawnCorpse = GameObject.Find("X_Cadaver") == null;
                PrefabUtility.RecordPrefabInstancePropertyModifications(fd);
                Object.DestroyImmediate(feederOld);
                pose += "X_Comiendo (chica) ";
            }
            var crawlerOld = GameObject.Find("X_Reptante");
            if (crawlerOld != null && crawlerOld.GetComponent<ZombieCripple>() == null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "Zombie_Oficial.prefab"), crawlerOld.transform.parent);
                go.name = "X_Reptante";
                var p = crawlerOld.transform.position; p.y = Mathf.Max(p.y, 1f);
                go.transform.SetPositionAndRotation(p, crawlerOld.transform.rotation);
                var cr = go.GetComponent<ZombieCripple>(); cr.startCrawling = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(cr);
                Object.DestroyImmediate(crawlerOld);
                pose += "X_Reptante (Oficial) ";
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            return "animaciones comunes: " + string.Join(", ", log) + " | Pxl3 sustituidos: " + swapped + " | " + pose;
        }
    }
}
#endif
