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
            while (c.layers.Length > 1) c.RemoveLayer(c.layers.Length - 1);
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
            c.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            c.AddParameter("OpenDoor", AnimatorControllerParameterType.Trigger);
            c.AddParameter("EnterDoor", AnimatorControllerParameterType.Trigger);
            c.AddParameter("RunTurn", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Stairs", AnimatorControllerParameterType.Bool);
            c.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
            c.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            c.AddParameter("Falling", AnimatorControllerParameterType.Bool);
            c.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            c.AddParameter("HardLand", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Cover", AnimatorControllerParameterType.Int);
            c.AddParameter("CoverIn", AnimatorControllerParameterType.Trigger);
            c.AddParameter("CoverOut", AnimatorControllerParameterType.Trigger);
            c.AddParameter("CoverMove", AnimatorControllerParameterType.Float);
            c.AddParameter("Fidget", AnimatorControllerParameterType.Trigger);
            c.AddParameter("FidgetIndex", AnimatorControllerParameterType.Int);
            c.AddParameter("DeathVariant", AnimatorControllerParameterType.Int);
            c.AddParameter("Draw", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Holster", AnimatorControllerParameterType.Trigger);

            // ---- clips por ranura (con sustituto provisional)
            var runF = First("P_RunForward", "P_Running", "P_Run");
            var idle = First("P_BreathingIdle", "P_Idle", "P_BreathingIdle2");
            string idleNote = "reposo: clip real";
            if (idle == null) { idle = PoseClip("P_IdlePose", First("P_TurnLeft45Degrees", "P_RifleTurn"), 0f); idleNote = "reposo: POSTURA FIJA provisional (falta Idle)"; }
            var walk = First("P_Walking", "P_WalkForward", "P_Walk", "P_PistolWalk");
            string walkNote = "andar: clip real";
            float walkScale = 1f;
            float walkSpeedClip = walk != null && walk.name == "P_PistolWalk" ? 2.35f : 2.4f;
            if (walk == null) { walk = runF; walkScale = 0.6f; walkNote = "andar: correr a ritmo lento provisional (falta Walking)"; }
            var back = First("P_WalkingBackwards", "P_WalkBackward", "P_PistolWalkBackward");
            var wl = First("P_WalkLeft", "P_LeftStrafeWalking"); var wr = First("P_WalkRight", "P_RightStrafeWalking");
            var wfl = First("P_WalkForwardLeft");
            var aimRifle = First("P_RifleAimingIdle", "P_RifleAimIdle");
            var aimPistol = First("P_PistolIdle", "P_PistolAimIdle", "P_PistolAim") ?? aimRifle;
            var fireRifle = First("P_FiringRifle", "P_RifleFire");
            var firePistol = First("P_PistolFire", "P_Shooting", "P_PistolShoot") ?? aimPistol;   // sin clip de pistola: se mantiene la pose de apuntar (el retroceso es por codigo)
            fireRifle = aimRifle;   // el clip de rifle gira el torso unos 30 grados respecto a la pose de apuntar: tiron al disparar; retroceso por codigo
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
            { int k = ch2.Length - 1; ch2[k].timeScale = walk == runF ? 0.43f : Mathf.Min(1f, 1.7f / walkSpeedClip); }                                 // adelante a ~1.7 m/s
            t2.children = ch2;

            Trans(free, aim, 0.18f, ("Aiming", AnimatorConditionMode.If, 0));
            Trans(aim, free, 0.22f, ("Aiming", AnimatorConditionMode.IfNot, 0));
            var death = sm.AddState("Death"); death.motion = dying;
            var td = sm.AddAnyStateTransition(death); td.hasExitTime = false; td.duration = 0.15f; td.canTransitionToSelf = false;
            td.AddCondition(AnimatorConditionMode.If, 0, "Dead");

            // ---- acciones de cuerpo entero (PlayerActions). Las lanza el codigo; el avance y el giro tambien (raiz fuera del clip)
            void Action(string name, AnimationClip clip, float speed, float exitTime, float offset = 0f)
            {
                if (clip == null) { log.Add(name + ": SIN clip"); return; }
                var s = sm.AddState(name); s.motion = clip; s.speed = speed;
                var t = sm.AddAnyStateTransition(s); t.hasExitTime = false; t.duration = 0.2f; t.offset = offset; t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0, name); t.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
                var back = s.AddTransition(free); back.hasExitTime = true; back.exitTime = exitTime; back.duration = 0.3f;
                // abrir una puerta normal: en cuanto vuelve el control y echa a andar, se corta (si no, patinaria con los brazos en el pomo)
                if (name == "OpenDoor") { var mv = s.AddTransition(free); mv.hasExitTime = false; mv.duration = 0.25f; mv.AddCondition(AnimatorConditionMode.Greater, 0.4f, "Speed"); }
                log.Add(name + ": " + clip.name);
            }
            Action("OpenDoor", First("P_OpeningDoor"), 2.7f, 0.40f);                 // mas rapido a peticion del usuario (antes 1,8)                 // el clip trae dos aperturas: solo la primera (0-4,4 s)
            Action("EnterDoor", First("P_OpeningDoorInwards"), 1.25f, 0.97f);
            Action("RunTurn", First("P_RunningToTurn"), 1f, 0.72f);
            Action("Roll", First("P_FallingToRoll"), 1f, 0.85f, 0.23f);            // empieza al tocar el suelo (antes cae desde 2 m)
            var stairsClip = First("P_RunningUpStairs");
            if (stairsClip != null)
            {
                var st = sm.AddState("RunStairs"); st.motion = stairsClip;
                Trans(free, st, 0.2f, ("Stairs", AnimatorConditionMode.If, 0));
                Trans(st, free, 0.25f, ("Stairs", AnimatorConditionMode.IfNot, 0));
                Trans(st, aim, 0.18f, ("Aiming", AnimatorConditionMode.If, 0));
                log.Add("escaleras: " + stairsClip.name);
            }
            BuildMovementPack(c, sm, free, aim, death, log);

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
                Trans(a, s, 0.08f, ("Shoot", AnimatorConditionMode.If, 0));
                var back2 = s.AddTransition(a); back2.hasExitTime = true; back2.exitTime = 0.45f; back2.duration = 0.18f;
            }
            // recarga (pistola / arma larga) y golpe recibido: se pueden disparar desde cualquier estado del torso
            var rlP = First("P_Reload", "P_Reloading"); var rlR = First("P_Reloading", "P_Reload");
            float RT(string path, float fallback) { var w = AssetDatabase.LoadAssetAtPath<WeaponData>(path); return w != null ? w.reloadTime : fallback; }
            if (rlP != null)
            {
                var reloadP = um.AddState("ReloadPistol"); reloadP.motion = rlP; reloadP.speed = rlP.length / RT("Assets/_Project/Data/W_Pistol.asset", 1.6f);
                var reloadR = um.AddState("ReloadRifle"); reloadR.motion = rlR; reloadR.speed = rlR.length / RT("Assets/_Project/Data/W_Shotgun.asset", 2.5f);
                foreach (var (st, lg) in new[] { (reloadP, false), (reloadR, true) })
                {
                    var tr = um.AddAnyStateTransition(st); tr.hasExitTime = false; tr.duration = 0.15f; tr.canTransitionToSelf = false;
                    tr.AddCondition(AnimatorConditionMode.If, 0, "Reload");
                    tr.AddCondition(lg ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "LongGun");
                    var back3 = st.AddTransition(empty); back3.hasExitTime = true; back3.exitTime = 0.92f; back3.duration = 0.2f;
                }
                log.Add("recarga: clip real (" + rlP.name + " / " + rlR.name + ")");
            }
            var hitClip = First("P_HitReaction", "P_HitReactionStanding");
            if (hitClip != null)
            {
                var hit = um.AddState("Hit"); hit.motion = hitClip; hit.speed = 2.2f;
                var th = um.AddAnyStateTransition(hit); th.hasExitTime = false; th.duration = 0.08f; th.canTransitionToSelf = true;
                th.AddCondition(AnimatorConditionMode.If, 0, "Hit");
                var back4 = hit.AddTransition(empty); back4.hasExitTime = true; back4.exitTime = 0.55f; back4.duration = 0.2f;
            }
            // sacar / guardar la escopeta al cambiar de arma (solo brazos)
            foreach (var (st, clipName, trig, sp) in new[] { ("DrawLong", "L_RiflePullOut", "Draw", 1.6f), ("HolsterLong", "L_PutBackRifle", "Holster", 1.8f) })
            {
                var clip = First(clipName); if (clip == null) continue;
                var s = um.AddState(st); s.motion = clip; s.speed = sp;
                var tr = um.AddAnyStateTransition(s); tr.hasExitTime = false; tr.duration = 0.1f; tr.canTransitionToSelf = false;
                tr.AddCondition(AnimatorConditionMode.If, 0, trig);
                var b = s.AddTransition(empty); b.hasExitTime = true; b.exitTime = 0.9f; b.duration = 0.2f;
                var ba = s.AddTransition(empty); ba.hasExitTime = false; ba.duration = 0.15f; ba.AddCondition(AnimatorConditionMode.If, 0, "Aiming");
            }
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        /// <summary>
        /// Pro Rifle Pack y Action Adventure Pack (2026-10-08): agacharse (C), saltar (espacio) y caer, coberturas (F), gestos en
        /// reposo y muertes segun de donde venga el golpe. El override de arma larga (PlayerHumanoid_Long) cambia la locomocion por
        /// la del pack de rifle.
        /// </summary>
        static void BuildMovementPack(AnimatorController c, AnimatorStateMachine sm, AnimatorState free, AnimatorState aim, AnimatorState death, List<string> log)
        {
            // ---- agachado: mapa de 8 direcciones (MoveX/MoveY van normalizados a la velocidad de apuntar, 1,7 m/s; agachado va a 1,2)
            var crouchIdle = First("R_IdleCrouching");
            if (crouchIdle != null)
            {
                var cr = sm.AddState("Crouch");
                var ct = NewTree(c, cr, "Crouch");
                ct.blendType = BlendTreeType.FreeformDirectional2D; ct.blendParameter = "MoveX"; ct.blendParameterY = "MoveY";
                ct.AddChild(crouchIdle, Vector2.zero);
                const float k = 0.7f;
                var dirs = new (string clip, Vector2 at)[]
                {
                    ("R_WalkCrouchingForward", new Vector2(0, k)), ("R_WalkCrouchingBackward", new Vector2(0, -k)),
                    ("R_WalkCrouchingLeft", new Vector2(-k, 0)), ("R_WalkCrouchingRight", new Vector2(k, 0)),
                    ("R_WalkCrouchingForwardLeft", new Vector2(-k, k) * 0.7071f), ("R_WalkCrouchingForwardRight", new Vector2(k, k) * 0.7071f),
                    ("R_WalkCrouchingBackwardLeft", new Vector2(-k, -k) * 0.7071f), ("R_WalkCrouchingBackwardRight", new Vector2(k, -k) * 0.7071f),
                };
                foreach (var (clip, at) in dirs) { var m = First(clip); if (m != null) ct.AddChild(m, at); }
                var ch = ct.children; for (int i = 1; i < ch.Length; i++) ch[i].timeScale = 0.62f; ct.children = ch;   // clips a ~1,95 m/s; agachado va a 1,2
                Trans(free, cr, 0.25f, ("Crouch", AnimatorConditionMode.If, 0));
                Trans(aim, cr, 0.25f, ("Crouch", AnimatorConditionMode.If, 0));
                Trans(cr, aim, 0.25f, ("Crouch", AnimatorConditionMode.IfNot, 0), ("Aiming", AnimatorConditionMode.If, 0));
                Trans(cr, free, 0.25f, ("Crouch", AnimatorConditionMode.IfNot, 0));
                log.Add("agachado: 8 direcciones");
            }

            // ---- salto y caida
            var jumpUp = First("A_JumpingUp"); var airborne = First("A_FallingIdle"); var land = First("R_JumpDown"); var hard = First("A_HardLanding");
            if (jumpUp != null && airborne != null && land != null)
            {
                var up = sm.AddState("JumpUp"); up.motion = jumpUp;
                var air = sm.AddState("Airborne"); air.motion = airborne;
                var ld = sm.AddState("Land"); ld.motion = land; ld.speed = 1.3f;
                var tj = sm.AddAnyStateTransition(up); tj.hasExitTime = false; tj.duration = 0.1f; tj.canTransitionToSelf = false;
                tj.AddCondition(AnimatorConditionMode.If, 0, "Jump"); tj.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
                var u2a = up.AddTransition(air); u2a.hasExitTime = true; u2a.exitTime = 0.9f; u2a.duration = 0.15f;
                foreach (var from in new[] { free, aim })
                {
                    var tf = from.AddTransition(air); tf.hasExitTime = false; tf.duration = 0.25f; tf.AddCondition(AnimatorConditionMode.If, 0, "Falling");
                }
                var a2l = air.AddTransition(ld); a2l.hasExitTime = false; a2l.duration = 0.1f; a2l.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
                var l2f = ld.AddTransition(free); l2f.hasExitTime = true; l2f.exitTime = 0.7f; l2f.duration = 0.25f;
                if (hard != null)
                {
                    var hl = sm.AddState("HardLand"); hl.motion = hard;
                    var th = sm.AddAnyStateTransition(hl); th.hasExitTime = false; th.duration = 0.1f; th.canTransitionToSelf = false;
                    th.AddCondition(AnimatorConditionMode.If, 0, "HardLand"); th.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
                    var hb = hl.AddTransition(free); hb.hasExitTime = true; hb.exitTime = 0.85f; hb.duration = 0.3f;
                }
                log.Add("salto y caida");
            }

            // ---- coberturas (la raiz de estos clips no gira: el codigo pone al personaje de espaldas a la pared)
            AnimatorState CoverIdle(string name, string idleClip, string leftClip, string rightClip)
            {
                var s = sm.AddState(name);
                var bt = NewTree(c, s, name);
                bt.blendParameter = "CoverMove"; bt.useAutomaticThresholds = false;
                var rc = First(rightClip); var ic = First(idleClip); var lc = First(leftClip);
                if (rc != null) bt.AddChild(rc, -1f);
                bt.AddChild(ic, 0f);
                if (lc != null) bt.AddChild(lc, 1f);
                return s;
            }
            if (First("L_CoverIdle") != null && First("A_StandToCover") != null)
            {
                var stand = CoverIdle("CoverStand", "L_CoverIdle", "A_LeftCoverSneak", "A_RightCoverSneak");
                var low = CoverIdle("CoverCrouch", "L_CoverIdle1", "A_CrouchedSneakingLeft", "A_CrouchedSneakingRight");
                foreach (var (enterClip, target, type) in new[] { ("A_StandToCover", stand, 1), ("A_StandToCover2", low, 2) })
                {
                    var e = sm.AddState("CoverEnter" + type); e.motion = First(enterClip); e.speed = 1.2f;
                    var te = sm.AddAnyStateTransition(e); te.hasExitTime = false; te.duration = 0.15f; te.canTransitionToSelf = false;
                    te.AddCondition(AnimatorConditionMode.If, 0, "CoverIn"); te.AddCondition(AnimatorConditionMode.Equals, type, "Cover");
                    var tin = e.AddTransition(target); tin.hasExitTime = true; tin.exitTime = 0.9f; tin.duration = 0.2f;
                }
                foreach (var (from, exitClip) in new[] { (stand, "A_CoverToStand2"), (low, "A_CoverToStand") })
                {
                    var x = sm.AddState(from.name + "Exit"); x.motion = First(exitClip); x.speed = 1.4f;
                    var tx = from.AddTransition(x); tx.hasExitTime = false; tx.duration = 0.15f; tx.AddCondition(AnimatorConditionMode.If, 0, "CoverOut");
                    var xb = x.AddTransition(free); xb.hasExitTime = true; xb.exitTime = 0.8f; xb.duration = 0.25f;
                    var mv = x.AddTransition(free); mv.hasExitTime = false; mv.duration = 0.25f; mv.AddCondition(AnimatorConditionMode.Greater, 0.6f, "Speed");
                    var ta = from.AddTransition(aim); ta.hasExitTime = false; ta.duration = 0.2f; ta.AddCondition(AnimatorConditionMode.Equals, 0, "Cover"); ta.AddCondition(AnimatorConditionMode.If, 0, "Aiming");
                    var tf = from.AddTransition(free); tf.hasExitTime = false; tf.duration = 0.3f; tf.AddCondition(AnimatorConditionMode.Equals, 0, "Cover");
                }
                log.Add("coberturas: de pie y agachado");
            }

            // ---- gestos en reposo (solo con pistola; los lanza PlayerAnimation tras un rato quieto)
            for (int i = 0; i < 4; i++)
            {
                var clip = First("A_Idle" + (i + 2));
                if (clip == null) continue;
                var f = sm.AddState("Fidget" + i); f.motion = clip;
                var tf = free.AddTransition(f); tf.hasExitTime = false; tf.duration = 0.4f;
                tf.AddCondition(AnimatorConditionMode.If, 0, "Fidget"); tf.AddCondition(AnimatorConditionMode.Equals, i, "FidgetIndex");
                var back = f.AddTransition(free); back.hasExitTime = true; back.exitTime = 0.95f; back.duration = 0.4f;
                var mv = f.AddTransition(free); mv.hasExitTime = false; mv.duration = 0.2f; mv.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
                var am = f.AddTransition(aim); am.hasExitTime = false; am.duration = 0.2f; am.AddCondition(AnimatorConditionMode.If, 0, "Aiming");
            }

            // ---- muertes segun de donde viene el golpe (0 = la de siempre)
            foreach (var t in sm.anyStateTransitions)
                if (t.destinationState == death && !t.conditions.Any(q => q.parameter == "DeathVariant"))
                    t.AddCondition(AnimatorConditionMode.Equals, 0, "DeathVariant");
            string[] deaths = { "R_DeathFromTheFront", "R_DeathFromTheBack", "R_DeathFromRight", "R_DeathFromFrontHeadshot" };
            for (int i = 0; i < deaths.Length; i++)
            {
                var clip = First(deaths[i]); if (clip == null) continue;
                var d = sm.AddState("Death" + (i + 1)); d.motion = clip;
                var td = sm.AddAnyStateTransition(d); td.hasExitTime = false; td.duration = 0.15f; td.canTransitionToSelf = false;
                td.AddCondition(AnimatorConditionMode.If, 0, "Dead"); td.AddCondition(AnimatorConditionMode.Equals, i + 1, "DeathVariant");
            }
            log.Add("muertes: " + deaths.Length + " mas");
        }

        /// <summary>Override de arma larga: locomocion, salto y reposo del Pro Rifle Pack (con la escopeta en las manos).</summary>
        public static AnimatorOverrideController LongGunOverride(AnimatorController baseCtrl)
        {
            string path = AnimDir + "PlayerHumanoid_Long.overrideController";
            var oc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            bool isNew = oc == null;
            if (isNew) oc = new AnimatorOverrideController(baseCtrl); else oc.runtimeAnimatorController = baseCtrl;
            var map = new Dictionary<string, string>
            {
                ["P_Idle"] = "R_Idle", ["P_BreathingIdle"] = "R_Idle", ["P_PistolWalk"] = "R_WalkForward", ["P_Walking"] = "R_WalkForward",
                ["P_RunForward"] = "R_RunForward", ["P_WalkLeft"] = "R_WalkLeft", ["P_WalkRight"] = "R_WalkRight",
                ["P_PistolWalkBackward"] = "R_WalkBackward", ["P_WalkForwardLeft"] = "R_WalkForwardLeft",
                ["A_JumpingUp"] = "R_JumpUp", ["A_FallingIdle"] = "R_JumpLoop",
            };
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var orig in baseCtrl.animationClips.Distinct())
                pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(orig, map.TryGetValue(orig.name, out var rep) ? First(rep) : null));
            oc.ApplyOverrides(pairs);
            if (isNew) AssetDatabase.CreateAsset(oc, path); else EditorUtility.SetDirty(oc);
            return oc;
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
            pa.animator = an2; pa.handgunController = ctrl; pa.longGunController = LongGunOverride(ctrl);
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
