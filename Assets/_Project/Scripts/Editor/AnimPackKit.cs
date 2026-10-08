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
        };

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
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }
    }
}
#endif
