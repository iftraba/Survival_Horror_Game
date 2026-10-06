#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Monta el protagonista (Soldier de Mixamo, Humanoid) con las animaciones de Mixamo que haya en PlayerAnims.
    /// Capa base (cuerpo entero): libre (reposo/andar/correr) y apuntando (movimiento en 8 direcciones).
    /// Capa de torso (mascara de brazos/columna/cabeza): pose de apuntar y disparo, encima de la base.
    /// Cada ranura prueba varios nombres de clip y, si falta alguno, usa un sustituto provisional: al descargar la
    /// animacion que falta basta repetir "Horror/Construir protagonista" y la sustituye sola.
    /// </summary>
    public static class PlayerKit
    {
        const string M = "Assets/_Project/Art/Mixamo/";
        const string AnimDir = "Assets/_Project/Animation/";

        static AnimationClip First(params string[] names)
        {
            foreach (var n in names)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(M + "PlayerAnims/" + n + ".fbx"))
                    if (o is AnimationClip c && !c.name.StartsWith("__")) return c;
            }
            return null;
        }

        /// <summary>Clip de una sola postura (fotograma 0 de otro) para quedarse quieto mientras no hay animacion de reposo.</summary>
        static AnimationClip PoseClip(string name, AnimationClip src, float time)
        {
            string path = AnimDir + name + ".anim";
            var dst = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (dst == null) { dst = new AnimationClip { name = name }; AssetDatabase.CreateAsset(dst, path); }
            dst.ClearCurves();
            foreach (var b in AnimationUtility.GetCurveBindings(src))
            {
                float v = AnimationUtility.GetEditorCurve(src, b).Evaluate(time);
                AnimationUtility.SetEditorCurve(dst, b, AnimationCurve.Constant(0f, 1f, v));
            }
            var st = AnimationUtility.GetAnimationClipSettings(dst);
            st.loopTime = true; st.stopTime = 1f; st.startTime = 0f;
            AnimationUtility.SetAnimationClipSettings(dst, st);
            EditorUtility.SetDirty(dst);
            return dst;
        }

        static BlendTree NewTree(AnimatorController c, AnimatorState s, string name)
        {
            var t = new BlendTree { name = name };
            AssetDatabase.AddObjectToAsset(t, c);
            s.motion = t;
            return t;
        }

        static AvatarMask UpperMask()
        {
            string path = AnimDir + "UpperBodyHumanoid.mask";
            var m = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (m == null) { m = new AvatarMask(); AssetDatabase.CreateAsset(m, path); }
            foreach (AvatarMaskBodyPart part in System.Enum.GetValues(typeof(AvatarMaskBodyPart)))
                if (part != AvatarMaskBodyPart.LastBodyPart) m.SetHumanoidBodyPartActive(part, false);
            foreach (var part in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers })
                m.SetHumanoidBodyPartActive(part, true);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Trans(AnimatorState from, AnimatorState to, float dur, params (string p, AnimatorConditionMode mode, float v)[] conds)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false; t.duration = dur;
            foreach (var c in conds) t.AddCondition(c.mode, c.v, c.p);
        }

        public static AnimatorController BuildController(List<string> log)
        {
            string path = AnimDir + "PlayerHumanoid.controller";
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(path);
            // vaciar y reconstruir (se conserva el archivo: los prefabs siguen apuntando a el)
            foreach (var l in c.layers.Skip(1).ToArray()) c.RemoveLayer(c.layers.ToList().IndexOf(l));
            var sm0 = c.layers[0].stateMachine;
            foreach (var st in sm0.states.ToArray()) sm0.RemoveState(st.state);
            foreach (var t in sm0.anyStateTransitions.ToArray()) sm0.RemoveAnyStateTransition(t);
            for (int i = c.parameters.Length - 1; i >= 0; i--) c.RemoveParameter(i);

            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            c.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            c.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
            c.AddParameter("Armed", AnimatorControllerParameterType.Bool);
            c.AddParameter("LongGun", AnimatorControllerParameterType.Bool);
            c.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            // ---- clips por ranura (con sustituto provisional)
            var runF = First("P_RunForward", "P_Running", "P_Run");
            var idle = First("P_BreathingIdle", "P_Idle", "P_BreathingIdle2");
            string idleNote = "reposo: clip real";
            if (idle == null) { idle = PoseClip("P_IdlePose", First("P_TurnLeft45Degrees", "P_RifleTurn"), 0f); idleNote = "reposo: POSTURA FIJA provisional (falta Idle)"; }
            var walk = First("P_Walking", "P_WalkForward", "P_Walk");
            string walkNote = "andar: clip real";
            float walkScale = 1f;
            if (walk == null) { walk = runF; walkScale = 0.6f; walkNote = "andar: correr a ritmo lento provisional (falta Walking)"; }
            var back = First("P_WalkingBackwards", "P_WalkBackward", "P_PistolWalkBackward");
            var wl = First("P_WalkLeft", "P_LeftStrafeWalking"); var wr = First("P_WalkRight", "P_RightStrafeWalking");
            var wfl = First("P_WalkForwardLeft");
            var aimRifle = First("P_RifleAimingIdle", "P_RifleAimIdle");
            var aimPistol = First("P_PistolIdle", "P_PistolAimIdle", "P_PistolAim") ?? aimRifle;
            var fireRifle = First("P_FiringRifle", "P_RifleFire");
            var firePistol = First("P_PistolFire", "P_Shooting", "P_PistolShoot") ?? fireRifle;
            var dying = First("P_Dying");
            log.Add(idleNote); log.Add(walkNote);
            log.Add(aimPistol == aimRifle ? "apuntar con pistola: usa la pose de rifle (falta Pistol Idle)" : "apuntar con pistola: clip real");
            log.Add(First("P_Reloading", "P_PistolReload", "P_RifleReload") == null ? "recarga: SIN animacion (falta Reloading)" : "recarga: clip real");
            log.Add(First("P_HitReaction", "P_HitReactionStanding") == null ? "recibir golpe: SIN animacion (falta Hit Reaction)" : "recibir golpe: clip real");

            // ---- capa base
            var sm = c.layers[0].stateMachine;
            var free = sm.AddState("Free");
            var tree = NewTree(c, free, "Free");
            tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 2.4f);
            tree.AddChild(runF, 4.4f);
            var ch = tree.children; ch[1].timeScale = walkScale; ch[1].cycleOffset = 0f; tree.children = ch;
            sm.defaultState = free;

            var aim = sm.AddState("Aim");
            var t2 = NewTree(c, aim, "Aim");
            t2.blendType = BlendTreeType.FreeformDirectional2D;
            t2.blendParameter = "MoveX"; t2.blendParameterY = "MoveY";
            t2.AddChild(idle, new Vector2(0f, 0f));
            if (wl != null) t2.AddChild(wl, new Vector2(-1f, 0f));
            if (wr != null) t2.AddChild(wr, new Vector2(1f, 0f));
            if (back != null) t2.AddChild(back, new Vector2(0f, -1f));
            if (wfl != null) t2.AddChild(wfl, new Vector2(-0.7071f, 0.7071f));
            if (wfl != null) t2.AddChild(wfl, new Vector2(0.7071f, 0.7071f));
            t2.AddChild(walk == runF ? runF : walk, new Vector2(0f, 1f));
            var ch2 = t2.children;
            if (wfl != null) { int k = ch2.ToList().FindLastIndex(x => x.motion == wfl); ch2[k].mirror = true; }     // diagonal derecha = espejo de la izquierda
            { int k = ch2.Length - 1; ch2[k].timeScale = walk == runF ? 0.43f : 1f; }                                 // adelante a ~1.7 m/s
            t2.children = ch2;

            Trans(free, aim, 0.18f, ("Aiming", AnimatorConditionMode.If, 0));
            Trans(aim, free, 0.22f, ("Aiming", AnimatorConditionMode.IfNot, 0));
            var death = sm.AddState("Death"); death.motion = dying;
            var td = sm.AddAnyStateTransition(death); td.hasExitTime = false; td.duration = 0.15f; td.canTransitionToSelf = false;
            td.AddCondition(AnimatorConditionMode.If, 0, "Dead");

            // ---- capa de torso
            c.AddLayer("UpperBody");
            var layers = c.layers;
            layers[1].avatarMask = UpperMask();
            layers[1].defaultWeight = 1f;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            c.layers = layers;
            var um = c.layers[1].stateMachine;
            var empty = um.AddState("Empty"); um.defaultState = empty;
            var aimR = um.AddState("AimRifle"); aimR.motion = aimRifle;
            var aimP = um.AddState("AimPistol"); aimP.motion = aimPistol;
            var shootR = um.AddState("ShootRifle"); shootR.motion = fireRifle; shootR.speed = 2.6f;
            var shootP = um.AddState("ShootPistol"); shootP.motion = firePistol; shootP.speed = 2.6f;
            Trans(empty, aimR, 0.15f, ("Aiming", AnimatorConditionMode.If, 0), ("Armed", AnimatorConditionMode.If, 0), ("LongGun", AnimatorConditionMode.If, 0));
            Trans(empty, aimP, 0.15f, ("Aiming", AnimatorConditionMode.If, 0), ("Armed", AnimatorConditionMode.If, 0), ("LongGun", AnimatorConditionMode.IfNot, 0));
            foreach (var a in new[] { aimR, aimP })
            {
                Trans(a, empty, 0.2f, ("Aiming", AnimatorConditionMode.IfNot, 0));
                Trans(a, empty, 0.2f, ("Armed", AnimatorConditionMode.IfNot, 0));
            }
            Trans(aimR, aimP, 0.15f, ("LongGun", AnimatorConditionMode.IfNot, 0));
            Trans(aimP, aimR, 0.15f, ("LongGun", AnimatorConditionMode.If, 0));
            foreach (var (a, s) in new[] { (aimR, shootR), (aimP, shootP) })
            {
                Trans(a, s, 0.04f, ("Shoot", AnimatorConditionMode.If, 0));
                var back2 = s.AddTransition(a); back2.hasExitTime = true; back2.exitTime = 0.45f; back2.duration = 0.1f;
            }
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        // ------------------------------------------------------------------ prefab
        static Transform Find(Transform root, string n)
        {
            if (root.name == n) return root;
            foreach (Transform ch in root) { var r = Find(ch, n); if (r != null) return r; }
            return null;
        }

        [MenuItem("Horror/Construir protagonista")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var log = new List<string>();
            var ctrl = BuildController(log);
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Characters/Player_Soldier.fbx");

            // agarre de las armas: en la pose de apuntar, el cañon va de la mano derecha a la izquierda
            var inst = Object.Instantiate(fbx);
            Vector3 holderLocalPos; Quaternion holderLocalRot;
            {
                var an = inst.GetComponent<Animator>();
                var aimClip = First("P_RifleAimingIdle");
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(inst, aimClip, 0.5f);
                var rh = an.GetBoneTransform(HumanBodyBones.RightHand); var lh = an.GetBoneTransform(HumanBodyBones.LeftHand); var rl = an.GetBoneTransform(HumanBodyBones.RightLowerArm);
                Vector3 dir = (lh.position - rh.position).normalized;
                Vector3 handDir = (rh.position - rl.position).normalized;
                Vector3 worldPos = rh.position + handDir * 0.06f;
                Quaternion worldRot = Quaternion.LookRotation(dir, inst.transform.up);
                holderLocalPos = Quaternion.Inverse(rh.rotation) * (worldPos - rh.position);
                holderLocalRot = Quaternion.Inverse(rh.rotation) * worldRot;
                AnimationMode.StopAnimationMode();
            }
            Object.DestroyImmediate(inst);

            string pp = "Assets/_Project/Prefabs/Characters/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(pp);
            var old = root.transform.Find("Model");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            model.name = "Model";
            model.transform.localPosition = new Vector3(0f, -1f, 0f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var an2 = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            an2.runtimeAnimatorController = ctrl; an2.applyRootMotion = false; an2.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var rightHand = an2.GetBoneTransform(HumanBodyBones.RightHand);
            Transform Holder(string n)
            {
                var h = new GameObject(n).transform;
                h.SetParent(rightHand, false);
                h.localPosition = holderLocalPos; h.localRotation = holderLocalRot; h.localScale = Vector3.one;
                return h;
            }
            var holder = Holder("WeaponHolder");
            var wc = root.GetComponent<WeaponController>();
            wc.handSocket = holder; wc.longSocket = Holder("LongWeaponHolder");
            var pa = root.GetComponent<PlayerAnimation>();
            pa.animator = an2; pa.handgunController = ctrl; pa.longGunController = ctrl;
            var pc = root.GetComponent<PlayerController>();
            pc.aimSpeed = 1.7f;                       // la velocidad de los pasos laterales de Mixamo (1.73 m/s)
            PrefabUtility.SaveAsPrefabAsset(root, pp);
            PrefabUtility.UnloadPrefabContents(root);
            log.Add("prefab del jugador con el Soldier");
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }
    }
}
#endif
