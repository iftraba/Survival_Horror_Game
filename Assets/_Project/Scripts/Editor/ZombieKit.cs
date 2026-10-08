#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace Horror.EditorTools
{
    /// <summary>
    /// Construye todos los enemigos a partir de los personajes y animaciones de Mixamo: controladores de animacion
    /// (uno base + un override por tipo de zombi + el del jefe) y los prefabs. Se puede repetir cuando cambien las animaciones.
    /// </summary>
    public static class ZombieKit
    {
        const string M = "Assets/_Project/Art/Mixamo/";
        const string AnimDir = "Assets/_Project/Animation/";
        const string PrefabDir = "Assets/_Project/Prefabs/Characters/";

        public static AnimationClip Clip(string name)
        {
            string folder = name.StartsWith("Z_") ? "ZombieAnims/" : name.StartsWith("G_") ? "GenericAnims/" : name.StartsWith("B_") ? "BossAnims/" : "PlayerAnims/";
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(M + folder + name + ".fbx"))
                if (o is AnimationClip c && !c.name.StartsWith("__")) return c;
            Debug.LogWarning("[Horror] No se encontro el clip " + name);
            return null;
        }

        // ------------------------------------------------------------------ momento del golpe
        /// <summary>Instante (s) de maxima velocidad de manos y pies respecto al cuerpo: es cuando conecta el golpe.</summary>
        public static float ImpactTime(GameObject model, AnimationClip clip)
        {
            var inst = Object.Instantiate(model);
            try
            {
                var an = inst.GetComponent<Animator>();
                var bones = new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot }
                    .Select(b => an.GetBoneTransform(b)).Where(t => t != null).ToArray();
                if (bones.Length == 0) return clip.length * 0.4f;
                int n = Mathf.Max(8, Mathf.CeilToInt(clip.length * 30f));
                var prev = new Vector3[bones.Length];
                var speed = new float[n + 1];
                AnimationMode.StartAnimationMode();
                for (int i = 0; i <= n; i++)
                {
                    float t = clip.length * i / n;
                    AnimationMode.SampleAnimationClip(inst, clip, t);
                    float sp = 0f;
                    for (int b = 0; b < bones.Length; b++)
                    {
                        Vector3 p = inst.transform.InverseTransformPoint(bones[b].position);
                        if (i > 0) sp = Mathf.Max(sp, (p - prev[b]).magnitude / (clip.length / n));
                        prev[b] = p;
                    }
                    speed[i] = sp;
                }
                AnimationMode.StopAnimationMode();
                // suavizado corto y primer pico fuerte (>= 60 % del maximo): el combo tiene varios golpes, vale el primero
                var sm = new float[n + 1];
                for (int i = 0; i <= n; i++) { float a = 0; int c = 0; for (int k = -1; k <= 1; k++) { int q = i + k; if (q >= 0 && q <= n) { a += speed[q]; c++; } } sm[i] = a / c; }
                int lo = Mathf.Max(1, Mathf.RoundToInt(n * 0.08f)), hi = Mathf.RoundToInt(n * 0.9f);
                float max = 0f;
                for (int i = lo; i < hi; i++) max = Mathf.Max(max, sm[i]);
                for (int i = lo + 1; i < hi - 1; i++)
                    if (sm[i] >= 0.6f * max && sm[i] >= sm[i - 1] && sm[i] >= sm[i + 1]) return clip.length * i / n;
                return clip.length * 0.4f;
            }
            finally { Object.DestroyImmediate(inst); }
        }

        // ------------------------------------------------------------------ controladores
        static AnimatorController FreshController(string path)
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (c == null) return AnimatorController.CreateAnimatorControllerAtPath(path);
            // se vacia por dentro y se conserva el archivo: los prefabs y los overrides siguen apuntando a el
            var sm = c.layers[0].stateMachine;
            foreach (var st in sm.states.ToArray()) sm.RemoveState(st.state);
            foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
            for (int i = c.parameters.Length - 1; i >= 0; i--) c.RemoveParameter(i);
            return c;
        }

        static void Params(AnimatorController c)
        {
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            c.AddParameter("AttackVariant", AnimatorControllerParameterType.Int);
            c.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            c.AddParameter("Alert", AnimatorControllerParameterType.Trigger);
        }

        static AnimatorState Locomotion(AnimatorController c, AnimatorStateMachine sm, AnimationClip idle, AnimationClip walk, AnimationClip run)
        {
            var loco = c.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f); tree.AddChild(walk, 1f); tree.AddChild(run, 2f);
            return loco;
        }

        static AnimatorState Oneshot(AnimatorStateMachine sm, AnimatorState loco, string name, AnimationClip clip, float speed, float exitTime)
        {
            var s = sm.AddState(name);
            s.motion = clip;
            s.speed = speed;
            var back = s.AddTransition(loco);
            back.hasExitTime = true; back.exitTime = exitTime; back.duration = 0.25f;
            return s;
        }

        static void Any(AnimatorStateMachine sm, AnimatorState to, string trigger, int variant = -1)
        {
            var t = sm.AddAnyStateTransition(to);
            t.duration = 0.15f; t.hasExitTime = false; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            if (variant >= 0) t.AddCondition(AnimatorConditionMode.Equals, variant, "AttackVariant");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
        }

        static void DeathState(AnimatorStateMachine sm, AnimationClip clip)
        {
            var d = sm.AddState("Death"); d.motion = clip;
            var t = sm.AddAnyStateTransition(d);
            t.duration = 0.15f; t.hasExitTime = false; t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        }

        /// <summary>Controlador base de los zombis (los clips son marcadores: cada tipo los sustituye en su override).</summary>
        public static AnimatorController BuildZombieBase()
        {
            var c = FreshController(AnimDir + "ZombieHumanoid.controller");
            Params(c);
            var sm = c.layers[0].stateMachine;
            var loco = Locomotion(c, sm, Clip("Z_ZombieIdle"), Clip("Z_ZombieWalk"), Clip("Z_ZombieRun"));
            sm.defaultState = loco;
            string[] atk = { "Z_ZombieAttack", "Z_ZombieBiting", "Z_ZombieNeckBite" };
            for (int i = 0; i < atk.Length; i++) Any(sm, Oneshot(sm, loco, "Attack" + i, Clip(atk[i]), 1.5f, 0.62f), "Attack", i);
            Any(sm, Oneshot(sm, loco, "Alert", Clip("Z_ZombieScream"), 1f, 0.85f), "Alert");
            Any(sm, Oneshot(sm, loco, "Hit", Clip("G_ZombieReactionHit"), 1.3f, 0.8f), "Hit");
            DeathState(sm, Clip("Z_ZombieDying"));
            AddReactions(c);
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        static readonly string[] ReactionStates = { "Attack3", "Attack4", "HeadHit", "Stun", "KnockFall", "Situp", "Downed", "Grab", "Death1", "Death2", "Death3" };
        static readonly string[] ReactionParams = { "HeadHit", "Stun", "Knockdown", "Grab", "DeathVariant" };

        /// <summary>
        /// Reacciones nuevas (2026-10-08) sobre el controlador base de los zombis, repetible: golpe en la cabeza, aturdido (escopetazo),
        /// derribo (cae de espaldas y se incorpora), tumbado (muere en el suelo), agarre con mordisco al cuello y tres muertes mas
        /// (DeathVariant 1 vientre/torso, 2 desplomarse, 3 tambalearse; 0 = la de cada tipo). Son clips humanoides de Mixamo (y la
        /// caida de espaldas del pack Pxltiger, que acaba boca arriba como empieza "Situp To Idle"): valen para todos los modelos.
        /// </summary>
        public static void AddReactions(AnimatorController c)
        {
            var sm = c.layers[0].stateMachine;
            foreach (var cs in sm.states.ToArray()) if (System.Array.IndexOf(ReactionStates, cs.state.name) >= 0) sm.RemoveState(cs.state);
            foreach (var t in sm.anyStateTransitions.ToArray()) if (t.destinationState == null) sm.RemoveAnyStateTransition(t);
            var ps = c.parameters;
            for (int i = ps.Length - 1; i >= 0; i--) if (System.Array.IndexOf(ReactionParams, ps[i].name) >= 0) c.RemoveParameter(i);
            c.AddParameter("HeadHit", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Stun", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Knockdown", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Grab", AnimatorControllerParameterType.Bool);
            c.AddParameter("DeathVariant", AnimatorControllerParameterType.Int);

            var loco = sm.states.First(s => s.state.name == "Locomotion").state;
            // ataques 3 y 4 (patada y ataque de Mixamo): solo los usan los tipos cuyo attackVariants tiene 5 entradas
            Any(sm, Oneshot(sm, loco, "Attack3", Clip("Z_ZombieKick"), 1.5f, 0.62f), "Attack", 3);
            Any(sm, Oneshot(sm, loco, "Attack4", Clip("Z_ZombieAttack2"), 1.5f, 0.62f), "Attack", 4);
            Any(sm, Oneshot(sm, loco, "HeadHit", Clip("Z_HeadHit"), 1.15f, 0.85f), "HeadHit");
            Any(sm, Oneshot(sm, loco, "Stun", Clip("Z_GroinB"), 1.1f, 0.92f), "Stun");
            AnimationClip fallBack = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Zombie/Animations/Zombie@Z_FallingBack.FBX"))
                if (o is AnimationClip fc && !fc.name.StartsWith("__")) fallBack = fc;
            var situp = Oneshot(sm, loco, "Situp", Clip("Z_SitupToIdle"), 1.3f, 0.95f);
            var fall = sm.AddState("KnockFall"); fall.motion = fallBack;
            var toSit = fall.AddTransition(situp); toSit.hasExitTime = true; toSit.exitTime = 1f; toSit.duration = 0.25f;
            Any(sm, fall, "Knockdown");
            var downed = sm.AddState("Downed"); downed.motion = fallBack;      // sin salidas: muerto en el suelo
            var grab = sm.AddState("Grab"); grab.motion = Clip("Z_NeckBiteGrab");
            var tg = sm.AddAnyStateTransition(grab); tg.duration = 0.2f; tg.hasExitTime = false; tg.canTransitionToSelf = false;
            tg.AddCondition(AnimatorConditionMode.If, 0, "Grab"); tg.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            var back = grab.AddTransition(loco); back.hasExitTime = false; back.duration = 0.25f; back.AddCondition(AnimatorConditionMode.IfNot, 0, "Grab");

            // muertes: la de siempre pasa a ser la variante 0
            var death0 = sm.states.First(s => s.state.name == "Death").state;
            foreach (var t in sm.anyStateTransitions)
                if (t.destinationState == death0 && !t.conditions.Any(k => k.parameter == "DeathVariant"))
                    t.AddCondition(AnimatorConditionMode.Equals, 0, "DeathVariant");
            string[] clips = { "Z_GroinA", "Z_Dying2", "Z_StumbleDeath" };
            for (int i = 0; i < clips.Length; i++)
            {
                var d = sm.AddState("Death" + (i + 1)); d.motion = Clip(clips[i]);
                var t = sm.AddAnyStateTransition(d);
                t.duration = 0.15f; t.hasExitTime = false; t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0, "Dead");
                t.AddCondition(AnimatorConditionMode.Equals, i + 1, "DeathVariant");
            }
            EditorUtility.SetDirty(c);
        }

        /// <summary>Controlador del jefe: baila (letargo) hasta que se le despierta; luego ruge, anda/corre y pega.</summary>
        public static AnimatorController BuildBossController()
        {
            var c = FreshController(AnimDir + "Boss.controller");
            Params(c);
            var sm = c.layers[0].stateMachine;
            var loco = Locomotion(c, sm, Clip("B_Idle"), Clip("B_MutantWalking"), Clip("B_MutantRun"));
            var dance = sm.AddState("Dance"); dance.motion = Clip("B_GangnamStyle");
            sm.defaultState = dance;
            var alert = Oneshot(sm, loco, "Alert", Clip("Z_ZombieScream"), 1f, 0.9f);
            var toAlert = dance.AddTransition(alert);
            toAlert.hasExitTime = false; toAlert.duration = 0.2f;
            toAlert.AddCondition(AnimatorConditionMode.If, 0, "Alert");
            string[] atk = { "B_Punching", "B_PunchToElbowCombo", "B_SurpriseUppercut" };
            float[] exit = { 0.85f, 0.45f, 0.5f };           // los combos largos traen cola de reposo: se cortan antes
            for (int i = 0; i < atk.Length; i++) Any(sm, Oneshot(sm, loco, "Attack" + i, Clip(atk[i]), 1f, exit[i]), "Attack", i);
            DeathState(sm, Clip("Z_ZombieDying"));
            AddBossCreature(c);
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        /// <summary>
        /// Creature Pack (2026-10-08) sobre el controlador de los jefes, repetible: estado JumpAttack (disparador "Leap", lo lanza
        /// BossLeap) y salidas de los ataques 0 y 1 ajustadas a los clips del mutante (punetazo y zarpazo). El jefe 2 no salta y solo
        /// usa el ataque 0 (su override pone el ataque del pack Pxltiger en todos).
        /// </summary>
        public static void AddBossCreature(AnimatorController c)
        {
            var sm = c.layers[0].stateMachine;
            foreach (var cs in sm.states.ToArray()) if (cs.state.name == "JumpAttack") sm.RemoveState(cs.state);
            foreach (var t in sm.anyStateTransitions.ToArray()) if (t.destinationState == null) sm.RemoveAnyStateTransition(t);
            var ps = c.parameters;
            for (int i = ps.Length - 1; i >= 0; i--) if (ps[i].name == "Leap") c.RemoveParameter(i);
            c.AddParameter("Leap", AnimatorControllerParameterType.Trigger);
            var loco = sm.states.First(s => s.state.name == "Locomotion").state;
            Any(sm, Oneshot(sm, loco, "JumpAttack", Clip("B_MutantJumpAttack"), 1.1f, 0.78f), "Leap");
            float[] exits = { 0.9f, 0.8f };
            for (int i = 0; i < exits.Length; i++)
            {
                var st = sm.states.FirstOrDefault(s => s.state.name == "Attack" + i).state;
                if (st == null) continue;
                foreach (var tr in st.transitions) if (tr.destinationState == loco) tr.exitTime = exits[i];
            }
            EditorUtility.SetDirty(c);
        }

        public static AnimatorOverrideController Override(string name, AnimatorController baseCtrl, Dictionary<string, string> map)
        {
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

        // ------------------------------------------------------------------ tipos
        /// <summary>Vida de todos los zombis normales (los jefes tienen la suya). Con ella mueren de 2 escopetazos a la cabeza y de 3 balas de pistola a la cabeza.</summary>
        public const float GenericHp = 220f;
        /// <summary>Velocidad de persecucion base de TODOS los zombis normales (x1,2 global = 1,0 m/s efectivos). Los jefes tienen la suya.</summary>
        public const float BaseChase = 0.84f;
        /// <summary>Multiplicador de dano al torso de los zombis policia (chaleco: aguantan mas los disparos al cuerpo; la cabeza no cambia).</summary>
        public const float PoliceTorsoMult = 0.5f;

        public class Kind
        {
            public string name, fbx;
            public float scale, hp, chase, damage, cooldownMin = 1.4f, torsoMult = 1f;
            public string[] idle, walk, run; // (uno solo; se toma el primero)
            public string[] attacks, hit;
            public string death = "Z_ZombieDying";
            public float walkClip, runClip = 2.84f, runAbove = 1.6f;
            public int hearing = 5;
        }

        public static readonly Kind[] Kinds =
        {
            new Kind { name = "Civil", fbx = "Zombie_Civil_Mixamo", scale = 1.0f, hp = GenericHp, chase = BaseChase, damage = 15f,
                       idle = new[] { "G_ZombieIdle" }, walk = new[] { "G_ZombieStumbling" }, run = new[] { "Z_ZombieRun" }, walkClip = 0.79f,
                       attacks = new[] { "Z_ZombieAttack", "G_ZombiePunching", "Z_ZombieBiting2" }, hit = new[] { "G_ZombieReactionHit" } },
            new Kind { name = "Girl", fbx = "Zombie_Girl", scale = 0.84f, hp = GenericHp, chase = BaseChase, damage = 12f,
                       idle = new[] { "G_ZombieIdle3" }, walk = new[] { "G_Walking" }, run = new[] { "G_ZombieRunning" }, walkClip = 0.50f,
                       attacks = new[] { "G_ZombieAttack", "G_ZombieHeadbutt", "G_ZombieKicking" }, hit = new[] { "G_ZombieReactionHit2" }, death = "Z_ZombieDeath" },
            new Kind { name = "Cop", fbx = "Zombie_Cop", scale = 0.9f, hp = GenericHp, torsoMult = PoliceTorsoMult, chase = BaseChase, damage = 18f,
                       idle = new[] { "G_ZombieScratchIdle" }, walk = new[] { "Z_ZombieWalk" }, run = new[] { "Z_ZombieRun" }, walkClip = 0.33f,
                       attacks = new[] { "Z_ZombieNeckBite", "Z_ZombieBiting", "G_ZombiePunching2" }, hit = new[] { "G_ZombieReactionHit" } },
            new Kind { name = "Yaku", fbx = "Zombie_Yaku", scale = 0.9f, hp = GenericHp, chase = BaseChase, damage = 20f,
                       idle = new[] { "G_ZombieIdle2" }, walk = new[] { "Z_ZombieWalk" }, run = new[] { "Z_ZombieRun" }, walkClip = 0.33f,
                       attacks = new[] { "Z_ZombieAttack", "G_ZombieKicking2", "G_ZombiePunching" }, hit = new[] { "G_ZombieReactionHit2" } },
        };

        /// <summary>Construye controladores y prefabs de todos los zombis y del jefe. Devuelve un resumen.</summary>
        [MenuItem("Horror/Construir zombis y jefe")]
        public static void BuildAllMenu() { Debug.Log("[Horror] " + BuildAll()); }

        public static string BuildAll()
        {
            var log = new List<string>();
            var baseCtrl = BuildZombieBase();
            foreach (var k in Kinds)
            {
                var map = new Dictionary<string, string>
                {
                    ["Z_ZombieIdle"] = k.idle[0], ["Z_ZombieWalk"] = k.walk[0], ["Z_ZombieRun"] = k.run[0],
                    ["Z_ZombieAttack"] = k.attacks[0], ["Z_ZombieBiting"] = k.attacks[1], ["Z_ZombieNeckBite"] = k.attacks[2],
                    ["G_ZombieReactionHit"] = k.hit[0], ["Z_ZombieDying"] = k.death,
                };
                var oc = Override("Zombie_" + k.name, baseCtrl, map);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Characters/" + k.fbx + ".fbx");
                var variants = new List<ZombieAI.AttackVariant>();
                var speedsInfo = new List<string>();
                foreach (var a in k.attacks)
                {
                    var clip = Clip(a);
                    float eff = clip.length * 0.62f / 1.5f;                // el estado reproduce a 1.5x y sale al 62 %
                    float impact = ImpactTime(model, clip) / 1.5f;
                    variants.Add(new ZombieAI.AttackVariant { hitDelay = Mathf.Clamp(impact, 0.2f, eff), damageMultiplier = 1f, cooldown = Mathf.Max(k.cooldownMin, eff + 0.35f) });
                    speedsInfo.Add(a + ":golpe " + variants.Last().hitDelay.ToString("F2") + "s/ciclo " + eff.ToString("F2") + "s");
                }
                var ctx = new Spec
                {
                    name = "Zombie_" + k.name, fbx = k.fbx, scale = k.scale, controller = oc, height = 2f, radius = 0.4f,
                    hp = k.hp, chase = k.chase, damage = k.damage, attackRange = 1.6f, cooldown = 1.4f, stagger = 0.5f, alertTime = 1.6f,
                    walkClip = k.walkClip, runClip = k.runClip, runAbove = k.runAbove, variants = variants.ToArray(), torsoMult = k.torsoMult,
                };
                BuildPrefab(ctx);
                log.Add(k.name + " [" + string.Join("; ", speedsInfo) + "]");
            }
            BuildBoss(log);
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }

        public class Spec
        {
            public string name, fbx, bossName;
            [Tooltip("Ruta completa del modelo (si no se indica, se busca 'fbx' en Art/Mixamo/Characters)")] public string fbxPath;
            public float scale, height, radius, hp, chase, damage, attackRange, cooldown, stagger, alertTime;
            public float walkClip, runClip, runAbove;
            public RuntimeAnimatorController controller;
            public ZombieAI.AttackVariant[] variants;
            public bool dormant;
            public string drop;
            public float headRadius = 0.15f, headLift = 0.13f, torsoRadius = 0.2f, limbRadiusScale = 1f;
            public float headMult = 3f, torsoMult = 1f, limbMult = 0.6f;
        }

        public static GameObject BuildPrefab(Spec s)
        {
            var root = new GameObject(s.name);
            var col = root.AddComponent<CapsuleCollider>();
            col.height = s.height; col.radius = s.radius; col.center = Vector3.zero;
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(string.IsNullOrEmpty(s.fbxPath) ? M + "Characters/" + s.fbx + ".fbx" : s.fbxPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            model.name = "Model";
            model.transform.localPosition = new Vector3(0f, -s.height * 0.5f, 0f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * s.scale;
            var an = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            an.runtimeAnimatorController = s.controller;
            an.applyRootMotion = false;
            an.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var zones = root.AddComponent<ZombieHitZones>();
            zones.headRadius = s.headRadius; zones.headLift = s.headLift; zones.torsoRadius = s.torsoRadius; zones.limbRadiusScale = s.limbRadiusScale;
            zones.headMultiplier = s.headMult; zones.torsoMultiplier = s.torsoMult; zones.limbMultiplier = s.limbMult;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = s.height; agent.radius = Mathf.Min(s.radius, 0.6f); agent.speed = s.chase; agent.angularSpeed = 200f;
            agent.acceleration = 6f; agent.baseOffset = s.height * 0.5f;
            var hp = root.AddComponent<Health>(); hp.maxHealth = s.hp;
            var ai = root.AddComponent<ZombieAI>();
            ai.chaseSpeed = s.chase; ai.attackDamage = s.damage; ai.attackRange = s.attackRange; ai.attackCooldown = s.cooldown;
            ai.staggerTime = s.stagger; ai.alertTime = s.alertTime; ai.attackVariants = s.variants; ai.dormant = s.dormant;
            ai.bossName = s.bossName ?? "";
            if (!string.IsNullOrEmpty(s.drop)) ai.dropOnDeath = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/" + s.drop + ".asset");
            root.AddComponent<ZombieAudio>();
            var za = root.AddComponent<ZombieAnimation>();
            za.animator = an; za.gaitMode = true; za.walkClipSpeed = s.walkClip * s.scale; za.runClipSpeed = s.runClip * s.scale; za.runAbove = s.runAbove;
            za.maxWalkPlayback = 2.2f;

            string path = PrefabDir + s.name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildBoss(List<string> log)
        {
            var ctrl = BuildBossController();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Characters/Boss.fbx");
            const float scale = 1.35f;
            string[] atk = { "B_Punching", "B_PunchToElbowCombo", "B_SurpriseUppercut" };
            float[] exit = { 0.85f, 0.45f, 0.5f };
            var variants = new List<ZombieAI.AttackVariant>();
            var info = new List<string>();
            for (int i = 0; i < atk.Length; i++)
            {
                var clip = Clip(atk[i]);
                float eff = clip.length * exit[i];
                float impact = ImpactTime(model, clip);
                if (atk[i] == "B_PunchToElbowCombo") impact = 1.06f;   // 1.o de sus dos golpes (el 2.o queda cortado: el estado sale al 45 %)
                variants.Add(new ZombieAI.AttackVariant { hitDelay = Mathf.Clamp(impact, 0.2f, eff), damageMultiplier = 1f, cooldown = Mathf.Max(1.6f, eff + 0.4f) });
                info.Add(atk[i] + ":golpe " + variants[i].hitDelay.ToString("F2") + "s/ciclo " + eff.ToString("F2") + "s");
            }
            var spec = new Spec
            {
                name = "Boss", fbx = "Boss", scale = scale, controller = ctrl, height = 2.8f, radius = 0.85f,
                hp = 1410f, chase = 1.9f, damage = 55f, attackRange = 2.4f, cooldown = 1.8f, stagger = 0f, alertTime = 2.6f,
                walkClip = 1.09f, runClip = 1.31f, runAbove = 1.2f, variants = variants.ToArray(), dormant = true,
                drop = "I_KeyExit", bossName = "COLOSSUS",
                headRadius = 0.19f, headLift = 0.1f, torsoRadius = 0.42f, limbRadiusScale = 2.0f,
                headMult = 3f, torsoMult = 0.5f, limbMult = 0.25f,   // cuerpo acorazado: la cabeza es el punto debil
            };
            BuildPrefab(spec);
            log.Add("Boss [" + string.Join("; ", info) + "]");
        }
    }
}
#endif
