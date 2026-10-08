#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Animaciones de Mixamo del 2026-10-08 (menu Horror/Animaciones nuevas: puertas, derribos, muertes). Repetible:
    /// - Importa los 14 clips (humanoides; los que giran o avanzan dejan la raiz fuera para que la mueva el codigo).
    /// - Jugador: abrir puertas, entrar en la sala del jefe, giro corriendo, voltereta y subir escaleras corriendo (PlayerKit).
    /// - Zombis (controlador base): golpe en la cabeza, aturdido, derribo y levantarse, agarre con mordisco y 3 muertes nuevas.
    /// - Pxltiger: segundo ataque (puñetazo de Mixamo). Reptantes: nueva animacion de arrastrarse.
    /// </summary>
    public static class AnimPackKit
    {
        const string M = "Assets/_Project/Art/Mixamo/";
        const string AnimDir = "Assets/_Project/Animation/";

        // (ruta, ciclico, giro de la raiz en la pose)
        static readonly (string path, bool loop, bool bakeRotation)[] Clips =
        {
            ("PlayerAnims/P_OpeningDoorInwards", false, true), ("PlayerAnims/P_OpeningDoor", false, true),
            ("PlayerAnims/P_RunningToTurn", false, false),       // el giro de 180 lo aplica el codigo
            ("PlayerAnims/P_FallingToRoll", false, true), ("PlayerAnims/P_RunningUpStairs", true, true),
            ("ZombieAnims/Z_SitupToIdle", false, true), ("ZombieAnims/Z_Crawling", true, true), ("ZombieAnims/Z_NeckBiteGrab", true, true),
            ("ZombieAnims/Z_GroinA", false, true), ("ZombieAnims/Z_GroinB", false, true), ("ZombieAnims/Z_HeadHit", false, true),
            ("ZombieAnims/Z_Dying2", false, true), ("ZombieAnims/Z_StumbleDeath", false, true), ("ZombieAnims/Z_ZombiePunching", false, true),
            // segunda tanda: patada y ataque de zombi; Creature Pack para el primer jefe
            ("ZombieAnims/Z_ZombieKick", false, true), ("ZombieAnims/Z_ZombieAttack2", false, true),
            ("BossAnims/B_MutantBreathingIdle", true, true), ("BossAnims/B_MutantIdle", true, true), ("BossAnims/B_MutantFlexing", true, true),
            ("BossAnims/B_MutantRoaring", false, true), ("BossAnims/B_MutantPunch", false, true), ("BossAnims/B_MutantSwiping", false, true),
            ("BossAnims/B_MutantJumpAttack", false, true), ("BossAnims/B_JumpAttack", false, true), ("BossAnims/B_MutantDying", false, true),
            ("BossAnims/B_MutantJumping", false, true),
        };

        const string PrefabDir = "Assets/_Project/Prefabs/Characters/";

        static GameObject ModelOf(GameObject prefabRoot)
        {
            var m = prefabRoot.transform.Find("Model");
            return m != null ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(m.gameObject) : null;
        }

        /// <summary>Patada (variante 3) y ataque nuevo (4) para los zombis de pie con los clips de Mixamo (los que tienen 3 ataques).</summary>
        static string ZombieKicks()
        {
            var names = new List<string>();
            foreach (var n in new[] { "Zombie_Civil", "Zombie_Girl", "Zombie_Cop", "Zombie_Oficial", "Zombie_Carronero", "Zombie_Yaku" })
            {
                string path = PrefabDir + n + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                var ai = root.GetComponent<ZombieAI>();
                var model = ModelOf(root);
                if (ai != null && model != null && ai.attackVariants != null && ai.attackVariants.Length >= 3)
                {
                    var list = new List<ZombieAI.AttackVariant>(ai.attackVariants);
                    if (list.Count > 3) list.RemoveRange(3, list.Count - 3);
                    foreach (var cn in new[] { "Z_ZombieKick", "Z_ZombieAttack2" })
                    {
                        var clip = ZombieKit.Clip(cn);
                        float eff = clip.length * 0.62f / 1.5f;               // el estado reproduce a 1.5x y sale al 62 %
                        float impact = ZombieKit.ImpactTime(model, clip) / 1.5f;
                        list.Add(new ZombieAI.AttackVariant { hitDelay = Mathf.Clamp(impact, 0.2f, eff), damageMultiplier = 1f, cooldown = Mathf.Max(1.4f, eff + 0.35f) });
                    }
                    ai.attackVariants = list.ToArray();
                    names.Add(n + " (patada " + list[3].hitDelay.ToString("F2") + "s, ataque " + list[4].hitDelay.ToString("F2") + "s)");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            return "patadas: " + string.Join(", ", names);
        }

        /// <summary>Primer jefe con el Creature Pack: override propio, ataques recalculados, rugido largo y ataque en salto (BossLeap).</summary>
        static string Colossus()
        {
            var bossCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "Boss.controller");
            if (bossCtrl == null) return "falta Boss.controller";
            ZombieKit.AddBossCreature(bossCtrl);
            var oc = ZombieKit.Override("Boss_Colossus", bossCtrl, new Dictionary<string, string>
            {
                ["B_GangnamStyle"] = "B_MutantFlexing",          // en letargo saca musculo en vez de bailar
                ["B_Idle"] = "B_MutantBreathingIdle", ["Z_ZombieScream"] = "B_MutantRoaring",
                ["B_Punching"] = "B_MutantPunch", ["B_PunchToElbowCombo"] = "B_MutantSwiping", ["Z_ZombieDying"] = "B_MutantDying",
            });
            string path = PrefabDir + "Boss.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var an = root.GetComponentInChildren<Animator>();
            var ai = root.GetComponent<ZombieAI>();
            var model = ModelOf(root);
            var info = new List<string>();
            if (an != null) an.runtimeAnimatorController = oc;
            if (ai != null && model != null)
            {
                string[] atk = { "B_MutantPunch", "B_MutantSwiping", "B_SurpriseUppercut" };
                float[] exit = { 0.9f, 0.8f, 0.5f };
                var v = new List<ZombieAI.AttackVariant>();
                for (int i = 0; i < atk.Length; i++)
                {
                    var clip = ZombieKit.Clip(atk[i]);
                    float eff = clip.length * exit[i];
                    float impact = Mathf.Clamp(ZombieKit.ImpactTime(model, clip), 0.2f, eff);
                    v.Add(new ZombieAI.AttackVariant { hitDelay = impact, damageMultiplier = 1f, cooldown = Mathf.Max(1.6f, eff + 0.4f) });
                    info.Add(atk[i] + " golpe " + impact.ToString("F2") + "s");
                }
                ai.attackVariants = v.ToArray();
                ai.alertTime = 4.6f;                                  // el rugido del mutante dura 5,4 s (sale al 90 %)
            }
            var leap = root.GetComponent<BossLeap>() ?? root.AddComponent<BossLeap>();
            leap.warningMaterial = AssetDatabase.LoadAssetAtPath<Material>(ToxicTextures.RingMatPath);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return "jefe 1: " + string.Join(", ", info);
        }

        /// <summary>Cambia un clip de un override sin tocar el resto.</summary>
        static void SetOverride(string overrideName, string baseClip, string newClip)
        {
            var oc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(AnimDir + overrideName + ".overrideController");
            if (oc == null) return;
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            oc.GetOverrides(pairs);
            var clip = ZombieKit.Clip(newClip);
            for (int i = 0; i < pairs.Count; i++)
                if (pairs[i].Key.name == baseClip) pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, clip);
            oc.ApplyOverrides(pairs);
            EditorUtility.SetDirty(oc);
        }

        [MenuItem("Horror/Animaciones nuevas (puertas, derribos, muertes)")]
        public static void Menu() { Debug.Log("[Horror] " + Apply()); }

        public static string Apply()
        {
            var log = new List<string>();
            foreach (var (path, loop, bake) in Clips) MixamoImport.ConfigureAnimation(M + path + ".fbx", loop, bake);
            log.Add(Clips.Length + " clips importados");

            var zc = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "ZombieHumanoid.controller");
            if (zc != null) { ZombieKit.AddReactions(zc); log.Add("zombis: reacciones y muertes nuevas"); }
            PlayerKit.BuildController(log);
            log.Add(PxlZombieKit.UpdateAnimations());
            // reptantes: la animacion nueva de arrastrarse para andar (la carrera sigue siendo Running Crawl)
            SetOverride("Zombie_OficialReptante", "Z_ZombieWalk", "Z_Crawling");
            SetOverride("Zombie_Carronero_Reptante", "Z_ZombieWalk", "Z_Crawling");
            log.Add("reptantes: Z_Crawling");
            log.Add(ZombieKicks());
            log.Add(Colossus());
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }
    }
}
#endif
