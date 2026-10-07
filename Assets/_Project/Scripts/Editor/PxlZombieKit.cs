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
    /// animaciones. Construye tres AnimatorOverrideController sobre el controlador base de los zombis (ZombieHumanoid) y
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


        // ------------------------------------------------------------------ segundo jefe
        /// <summary>
        /// Segundo jefe: el modelo mas corpulento del pack (Zombie3) a escala 1.65 con piel verde toxica, sobre el controlador del
        /// primer jefe (Boss.controller) con las animaciones del pack (idle, andar, correr, un ataque, caida). Lento, muy
        /// resistente y de golpe fuerte; suelta la llave maestra. Prefab: Zombie_BossPxl.
        /// </summary>
        public static GameObject BuildBoss(List<string> log)
        {
            var bossCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "Boss.controller");
            if (bossCtrl == null) { log.Add("falta Boss.controller"); return null; }
            // controlador: el del jefe 1 con otros clips (el estado de baile pasa a ser un idle: este jefe espera quieto)
            var map = new Dictionary<string, string>
            {
                ["B_Idle"] = "Z_Idle", ["B_MutantWalking"] = "Z_Walk_InPlace", ["B_MutantRun"] = "Z_Run_InPlace", ["B_GangnamStyle"] = "Z_Idle",
                ["Z_ZombieScream"] = "Z_Idle", ["B_Punching"] = "Z_Attack", ["B_PunchToElbowCombo"] = "Z_Attack", ["B_SurpriseUppercut"] = "Z_Attack",
                ["Z_ZombieDying"] = "Z_FallingBack",
            };
            string path = AnimDir + "Boss_Pxl.overrideController";
            var oc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            bool isNew = oc == null;
            if (isNew) oc = new AnimatorOverrideController(bossCtrl); else oc.runtimeAnimatorController = bossCtrl;
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var orig in bossCtrl.animationClips)
                pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(orig, map.TryGetValue(orig.name, out var rep) ? Clip(rep) : null));
            oc.ApplyOverrides(pairs);
            if (isNew) AssetDatabase.CreateAsset(oc, path); else EditorUtility.SetDirty(oc);

            const float scale = 1.65f;
            string fbxPath = "Assets/Zombie/FBXs/Zombie3.FBX";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var attack = Clip("Z_Attack");
            float eff = attack.length * 0.85f;                                   // el estado de ataque del jefe sale al 85 %
            float impact = Mathf.Clamp(ZombieKit.ImpactTime(model, attack), 0.2f, eff);
            var variant = new ZombieAI.AttackVariant { hitDelay = impact, damageMultiplier = 1f, cooldown = Mathf.Max(2.0f, eff + 0.6f) };
            var spec = new ZombieKit.Spec
            {
                name = "Zombie_BossPxl", fbxPath = fbxPath, scale = scale, controller = oc, height = 3.0f, radius = 0.85f,
                hp = 1800f, chase = 1.17f, damage = 60f, attackRange = 2.6f, cooldown = 2.0f, stagger = 0f, alertTime = 0.6f,
                walkClip = Mathf.Max(0.3f, Clip("Z_Walk_InPlace").averageSpeed.magnitude, Clip("Z_Walk").averageSpeed.magnitude) * scale,
                runClip = Clip("Z_Run").averageSpeed.magnitude * scale, runAbove = 1.8f, variants = new[] { variant }, dormant = true,
                drop = "I_KeyFinal", bossName = "ABOMINACION",
                headRadius = 0.22f, headLift = 0.1f, torsoRadius = 0.48f, limbRadiusScale = 2.0f,
                headMult = 3f, torsoMult = 0.65f, limbMult = 0.3f,                // cuerpo grueso: la cabeza es el punto debil
            };
            var prefab = ZombieKit.BuildPrefab(spec);

            // piel verde toxica: material propio (el del pack teñido y con brillo)
            string matPath = "Assets/Zombie/Materials/Zombie_URP_Boss.mat";
            var baseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Zombie/Materials/Zombie_URP.mat");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(baseMat); AssetDatabase.CreateAsset(mat, matPath); }
            mat.CopyPropertiesFromMaterial(baseMat);
            mat.SetColor("_BaseColor", new Color(0.55f, 1f, 0.5f));
            mat.SetTexture("_EmissionMap", null);                       // el mapa del pack solo ilumina los ojos: aqui brilla todo el cuerpo, poco
            mat.SetColor("_EmissionColor", new Color(0.001f, 0.012f, 0.003f));
            mat.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            var root = PrefabUtility.LoadPrefabContents(PrefabDir + "Zombie_BossPxl.prefab");
            foreach (var r in root.GetComponentsInChildren<Renderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; }
            // rastro y escupitajo de acido: materiales con textura (ToxicTextures)
            ToxicTextures.EnsureMaterials();
            var trail = root.GetComponent<ToxicTrail>() ?? root.AddComponent<ToxicTrail>();
            trail.puddleMaterial = AssetDatabase.LoadAssetAtPath<Material>(ToxicTextures.PuddleMatPath);
            trail.spitMaterial = AssetDatabase.LoadAssetAtPath<Material>(ToxicTextures.GlobMatPath);
            var atk = root.GetComponent<BossAttacks>() ?? root.AddComponent<BossAttacks>();
            const string snd = "Assets/_Project/Audio/Generated/";
            atk.chargeSound = AssetDatabase.LoadAssetAtPath<AudioClip>(snd + "boss_charge_fal.mp3");
            atk.warningMaterial = AssetDatabase.LoadAssetAtPath<Material>(ToxicTextures.RingMatPath);
            atk.crashSound = AssetDatabase.LoadAssetAtPath<AudioClip>(snd + "boss_crash_fal.mp3");
            atk.spitSound = AssetDatabase.LoadAssetAtPath<AudioClip>(snd + "acid_spit_fal.mp3");
            trail.sizzleSound = AssetDatabase.LoadAssetAtPath<AudioClip>(snd + "acid_sizzle_fal.mp3");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "Zombie_BossPxl.prefab");
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            log.Add("Boss_Pxl [golpe " + impact.ToString("F2") + "s, ciclo " + eff.ToString("F2") + "s, 2600 de vida, 60 de daño, persigue a 0,95 m/s]");
            return prefab;
        }

        [MenuItem("Horror/Construir zombis Pxltiger")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "ZombieHumanoid.controller");
            if (baseCtrl == null) return "falta ZombieHumanoid.controller (ejecuta Horror/Construir zombis y jefe)";
            var kinds = new[]
            {
                new Kind { name = "Pxl1", fbx = "Zombie1", walkInPlace = "Z_Walk_InPlace",  walkMoving = "Z_Walk",  runInPlace = "Z_Run_InPlace", runMoving = "Z_Run", hp = ZombieKit.GenericHp, chase = ZombieKit.BaseChase, damage = 15f },  // equilibrado (el clip anda a 0.27 m/s: a 0.42 se reproduce a ~1.5x; a 0.6 iba a 2x y se veia nervioso)
                new Kind { name = "Pxl2", fbx = "Zombie2", walkInPlace = "Z_Walk1_InPlace", walkMoving = "Z_Walk1", runInPlace = "Z_Run_InPlace", runMoving = "Z_Run", hp = ZombieKit.GenericHp, chase = ZombieKit.BaseChase, damage = 12f },  // rapido y fragil (corre: la carrera del clip va a 3.7 m/s; a 2.2 iba a camara lenta, 0.6x)
                new Kind { name = "Pxl3", fbx = "Zombie3", walkInPlace = "Z_Walk1_InPlace", walkMoving = "Z_Walk1", runInPlace = "Z_Run_InPlace", runMoving = "Z_Run", hp = ZombieKit.GenericHp, chase = ZombieKit.BaseChase, damage = 22f },  // lento y resistente (~1.3x)
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
                    hp = k.hp, chase = k.chase, damage = k.damage, attackRange = 1.6f, cooldown = 1.4f, stagger = 0.5f, alertTime = 0.35f,   // sin clip de grito: una parada larga solo se ve como zombi congelado
                    walkClip = Mathf.Max(0.3f, walkSpeed), runClip = Mathf.Max(1f, runSpeed), runAbove = 1.3f, variants = new[] { variant },
                };
                ZombieKit.BuildPrefab(spec);
                log.Add(k.name + " [paso " + walkSpeed.ToString("F2") + " m/s, carrera " + runSpeed.ToString("F2") + " m/s, golpe " + variant.hitDelay.ToString("F2") + "s]");
            }
            BuildBoss(log);
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }
    }
}
#endif
