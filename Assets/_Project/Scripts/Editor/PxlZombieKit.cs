#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Zombis del pack de la Asset Store "Zombie" (Pxltiger, importado en Assets/Zombie): tres modelos humanoides y 10
    /// animaciones. Construye dos AnimatorOverrideController sobre el controlador base de los zombis (ZombieHumanoid) y
    /// los prefabs Zombie_Pxl1..3 con el mismo montaje que ZombieKit. El pack solo trae un ataque y ninguna reaccion a
    /// golpes ni grito de alerta: se reutilizan idle y la caida para la muerte. Se puede repetir.
    /// </summary>
    public static class PxlZombieKit
    {
        const string Anims = "Assets/Zombie/Animations/";
        const string AnimDir = "Assets/_Project/Animation/";
        const string PrefabDir = "Assets/_Project/Prefabs/Characters/";

        static AnimationClip Clip(string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Anims + "Zombie@" + name + ".FBX"))
                if (o is AnimationClip c && !c.name.StartsWith("__")) return c;
            Debug.LogWarning("[Horror] No se encontro el clip " + name);
            return null;
        }

        static AnimatorOverrideController Override(string name, AnimatorController baseCtrl, string walk, string run)
        {
            var map = new Dictionary<string, string>
            {
                ["Z_ZombieIdle"] = "Z_Idle", ["Z_ZombieWalk"] = walk, ["Z_ZombieRun"] = run,
                ["Z_ZombieAttack"] = "Z_Attack", ["Z_ZombieBiting"] = "Z_Attack", ["Z_ZombieNeckBite"] = "Z_Attack",
                ["Z_ZombieScream"] = "Z_Idle", ["G_ZombieReactionHit"] = "Z_Idle", ["Z_ZombieDying"] = "Z_FallingBack",
            };
            string path = AnimDir + name + ".overrideController";
            var oc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            bool isNew = oc == null;
            if (isNew) oc = new AnimatorOverrideController(baseCtrl); else oc.runtimeAnimatorController = baseCtrl;
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var orig in baseCtrl.animationClips)
                pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(orig, map.TryGetValue(orig.name, out var rep) ? Clip(rep) : null));
            oc.ApplyOverrides(pairs);
            if (isNew) AssetDatabase.CreateAsset(oc, path); else EditorUtility.SetDirty(oc);
            return oc;
        }

        class Kind { public string name, fbx, walkInPlace, walkMoving, runInPlace, runMoving; public float hp, chase, damage; }

        [MenuItem("Horror/Construir zombis Pxltiger")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "ZombieHumanoid.controller");
            if (baseCtrl == null) return "falta ZombieHumanoid.controller (ejecuta Horror/Construir zombis y jefe)";
            var kinds = new[]
            {
                new Kind { name = "Pxl1", fbx = "Zombie1", walkInPlace = "Z_Walk_InPlace",  walkMoving = "Z_Walk",  runInPlace = "Z_Run_InPlace", runMoving = "Z_Run", hp = 110f, chase = 0.6f, damage = 15f },  // equilibrado (andar de 0.27 m/s en el clip: a 0.6 va a 2.2x, el tope sin patinar)
                new Kind { name = "Pxl2", fbx = "Zombie2", walkInPlace = "Z_Walk1_InPlace", walkMoving = "Z_Walk1", runInPlace = "Z_Run_InPlace", runMoving = "Z_Run", hp = 90f,  chase = 2.2f, damage = 12f },  // rapido y fragil (corre: la carrera del clip va a 3.7 m/s)
                new Kind { name = "Pxl3", fbx = "Zombie3", walkInPlace = "Z_Walk1_InPlace", walkMoving = "Z_Walk1", runInPlace = "Z_Run_InPlace", runMoving = "Z_Run", hp = 170f, chase = 0.4f, damage = 22f },  // lento y resistente
            };
            var log = new List<string>();
            foreach (var k in kinds)
            {
                var oc = Override("Zombie_" + k.name, baseCtrl, k.walkInPlace, k.runInPlace);
                string fbxPath = "Assets/Zombie/FBXs/" + k.fbx + ".FBX";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                var attack = Clip("Z_Attack");
                float eff = attack.length * 0.62f / 1.5f;                       // el estado de ataque reproduce a 1.5x y sale al 62 %
                float impact = ZombieKit.ImpactTime(model, attack) / 1.5f;
                var variant = new ZombieAI.AttackVariant { hitDelay = Mathf.Clamp(impact, 0.2f, eff), damageMultiplier = 1f, cooldown = Mathf.Max(1.4f, eff + 0.35f) };
                float walkSpeed = Clip(k.walkMoving).averageSpeed.magnitude, runSpeed = Clip(k.runMoving).averageSpeed.magnitude;
                var spec = new ZombieKit.Spec
                {
                    name = "Zombie_" + k.name, fbxPath = fbxPath, scale = 1.0f, controller = oc, height = 2f, radius = 0.4f,
                    hp = k.hp, chase = k.chase, damage = k.damage, attackRange = 1.6f, cooldown = 1.4f, stagger = 0.5f, alertTime = 1.6f,
                    walkClip = Mathf.Max(0.3f, walkSpeed), runClip = Mathf.Max(1f, runSpeed), runAbove = 1.3f, variants = new[] { variant },
                };
                ZombieKit.BuildPrefab(spec);
                log.Add(k.name + " [paso " + walkSpeed.ToString("F2") + " m/s, carrera " + runSpeed.ToString("F2") + " m/s, golpe " + variant.hitDelay.ToString("F2") + "s]");
            }
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }
    }
}
#endif
