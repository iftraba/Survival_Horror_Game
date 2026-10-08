#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Zombis "de ambiente" y reparto v2 (menu Horror/Zombis extra). Edita la ESCENA. Idempotente (rehace los objetos "X_*"):
    /// - X_Reptante: Zombie_OficialReptante ya arrastrandose, en el pasillo central de la zona 2.
    /// - X_Carronero: zombi (modelo de la chica) que empieza mordiendo un cadaver (X_Cadaver, modelo del civil tumbado) sobre un
    ///   charco de sangre con textura (X_Sangre). Con 2 disparos en las piernas cae y se arrastra (ZombieCripple).
    /// - Quita los zombis del Yaku (los de la katana) y pone otros modelos en su sitio.
    /// </summary>
    public static class ExtraZombiesKit
    {
        const string M = "Assets/_Project/Art/Mixamo/";
        const string AnimDir = "Assets/_Project/Animation/";
        const string PrefabDir = "Assets/_Project/Prefabs/Characters/";
        const string MatDir = "Assets/_Project/Materials/";

        static float Smooth(float e0, float e1, float x) { float k = Mathf.Clamp01((x - e0) / (e1 - e0)); return k * k * (3f - 2f * k); }

        // ------------------------------------------------------------------ sangre
        static Material BuildBlood()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var rnd = new System.Random(21);
            var sx = new float[14]; var sy = new float[14]; var sr = new float[14];
            for (int i = 0; i < sx.Length; i++)
            {
                float ang = (float)rnd.NextDouble() * 6.283f, rad = 0.62f + (float)rnd.NextDouble() * 0.3f;
                sx[i] = Mathf.Cos(ang) * rad; sy[i] = Mathf.Sin(ang) * rad; sr[i] = 0.02f + (float)rnd.NextDouble() * 0.05f;
            }
            for (int py = 0; py < N; py++)
                for (int px = 0; px < N; px++)
                {
                    float u = px / (N - 1f) * 2f - 1f, v = py / (N - 1f) * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v), th = Mathf.Atan2(v, u);
                    float e1 = Mathf.PerlinNoise(Mathf.Cos(th) * 2.2f + 9f, Mathf.Sin(th) * 2.2f + 9f);
                    float e2 = Mathf.PerlinNoise(Mathf.Cos(th * 3f) * 3f + 4f, Mathf.Sin(th * 3f) * 3f + 4f);
                    float edge = 0.55f + (e1 - 0.5f) * 0.3f + (e2 - 0.5f) * 0.12f;              // charco central de borde irregular
                    float a = 1f - Smooth(edge - 0.06f, edge, r);
                    for (int i = 0; i < sx.Length; i++)                                          // salpicaduras sueltas alrededor
                    {
                        float d = Mathf.Sqrt((u - sx[i]) * (u - sx[i]) + (v - sy[i]) * (v - sy[i]));
                        a = Mathf.Max(a, 1f - Smooth(sr[i] * 0.6f, sr[i], d));
                    }
                    if (a <= 0f) { tex.SetPixel(px, py, new Color(0.3f, 0.02f, 0.02f, 0f)); continue; }
                    float n = Mathf.PerlinNoise(u * 4f + 2f, v * 4f + 2f);
                    Color col = Color.Lerp(new Color(0.16f, 0.01f, 0.01f), new Color(0.38f, 0.03f, 0.03f), Smooth(0.2f, 0.9f, n));
                    col = Color.Lerp(col, new Color(0.5f, 0.05f, 0.04f), Smooth(edge - 0.2f, edge - 0.03f, r) * 0.7f);   // borde mas claro (sangre fina)
                    tex.SetPixel(px, py, new Color(col.r, col.g, col.b, a * 0.95f));
                }
            tex.Apply();
            string path = MatDir + "BloodPool_tex.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = true; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
            var t2 = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            string mp = MatDir + "BloodPool.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, mp); }
            mat.SetTexture("_BaseMap", t2); mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.3f); mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_SpecularHighlights", 0f); mat.SetFloat("_EnvironmentReflections", 0f);   // sin reflejos: en una quad transparente se veria un cuadrado blanco
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.SetFloat("_Cull", 0f);
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", 5f); mat.SetFloat("_DstBlend", 10f); mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ animaciones
        static void LoopClip(string clipName)
        {
            string path = M + "ZombieAnims/" + clipName + ".fbx";
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) return;
            foreach (var c in clips) { c.loopTime = true; c.loopPose = true; }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ prefabs
        static GameObject BuildCorpse()
        {
            // controlador de un solo estado: la caida de la muerte; HoldLastFrame lo deja ya tumbado
            string cp = AnimDir + "Corpse.controller";
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(cp) ?? AnimatorController.CreateAnimatorControllerAtPath(cp);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var st in sm.states.ToArray()) sm.RemoveState(st.state);
            var s = sm.AddState("Dead"); s.motion = ZombieKit.Clip("Z_ZombieDying"); sm.defaultState = s;
            EditorUtility.SetDirty(ctrl);

            var root = new GameObject("Cadaver_Civil");
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Characters/Zombie_Civil_Mixamo.fbx");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            model.name = "Model";
            var an = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl; an.applyRootMotion = false;
            root.AddComponent<HoldLastFrame>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "Cadaver_Civil.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject BuildCarronero(AnimatorController baseCtrl)
        {
            var girl = ZombieKit.Kinds.First(k => k.name == "Girl");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Characters/Zombie_Girl.fbx");
            // ---- de pie: empieza en el bucle de morder (el idle de su locomocion), luego anda/corre/ataca como la chica
            var oc = ZombieKit.Override("Zombie_Carronero", baseCtrl, new Dictionary<string, string>
            {
                ["Z_ZombieIdle"] = "Z_ZombieBiting2", ["Z_ZombieWalk"] = girl.walk[0], ["Z_ZombieRun"] = girl.run[0],
                ["Z_ZombieAttack"] = girl.attacks[0], ["Z_ZombieBiting"] = girl.attacks[1], ["Z_ZombieNeckBite"] = girl.attacks[2],
                ["G_ZombieReactionHit"] = "Z_ZombieBiting2", ["Z_ZombieDying"] = girl.death,   // al recibir un disparo sigue en el bucle de morder
            });
            // ---- arrastrandose (cuando le rompen las piernas)
            var crawlAttacks = new[] { "Z_ZombieBiting", "Z_ZombieBiting2", "Z_ZombieNeckBite" };
            var ocCrawl = ZombieKit.Override("Zombie_Carronero_Reptante", baseCtrl, new Dictionary<string, string>
            {
                ["Z_ZombieIdle"] = "Z_ZombieCrawl", ["Z_ZombieWalk"] = "Z_Crawling", ["Z_ZombieRun"] = "Z_RunningCrawl",
                ["Z_ZombieAttack"] = crawlAttacks[0], ["Z_ZombieBiting"] = crawlAttacks[1], ["Z_ZombieNeckBite"] = crawlAttacks[2],
                ["G_ZombieReactionHit"] = "Z_ZombieCrawl", ["Z_ZombieScream"] = "Z_ZombieCrawl", ["Z_ZombieDying"] = girl.death,
            });
            var variants = new List<ZombieAI.AttackVariant>();
            foreach (var a in girl.attacks)
            {
                var clip = ZombieKit.Clip(a);
                float eff = clip.length * 0.62f / 1.5f;
                variants.Add(new ZombieAI.AttackVariant { hitDelay = Mathf.Clamp(ZombieKit.ImpactTime(model, clip) / 1.5f, 0.2f, eff), damageMultiplier = 1f, cooldown = Mathf.Max(girl.cooldownMin, eff + 0.35f) });
            }
            var crawlVariants = new List<ZombieAI.AttackVariant>();
            foreach (var a in crawlAttacks)
            {
                var clip = ZombieKit.Clip(a);
                float eff = clip.length * 0.62f / 1.5f;
                crawlVariants.Add(new ZombieAI.AttackVariant { hitDelay = Mathf.Clamp(ZombieKit.ImpactTime(model, clip) / 1.5f, 0.2f, eff), damageMultiplier = 1f, cooldown = Mathf.Max(1.2f, eff + 0.35f) });
            }
            var prefab = ZombieKit.BuildPrefab(new ZombieKit.Spec
            {
                name = "Zombie_Carronero", fbx = girl.fbx, scale = girl.scale, controller = oc, height = 2f, radius = 0.4f,
                hp = ZombieKit.GenericHp, chase = ZombieKit.BaseChase, damage = girl.damage, attackRange = 1.6f, cooldown = 1.4f, stagger = 0.5f, alertTime = 0.8f,
                walkClip = girl.walkClip, runClip = girl.runClip, runAbove = girl.runAbove, variants = variants.ToArray(),
            });
            string path = AssetDatabase.GetAssetPath(prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            var cr = root.GetComponent<ZombieCripple>() ?? root.AddComponent<ZombieCripple>();
            cr.crawlController = ocCrawl; cr.crawlVariants = crawlVariants.ToArray();
            cr.legHitsToCripple = 2; cr.fallTime = 1.0f; cr.crawlChase = ZombieKit.BaseChase;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return prefab;
        }

        // ------------------------------------------------------------------ escena
        static GameObject Place(string prefabName, string name, Vector3 pos, float yaw, Transform parent)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + prefabName + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            go.name = name;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        [MenuItem("Horror/Zombis extra (reptante, carronero, sin katana)")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "ZombieHumanoid.controller");
            if (baseCtrl == null) return "falta ZombieHumanoid.controller";
            var log = new List<string>();
            LoopClip("Z_ZombieBiting2");
            var blood = BuildBlood();
            BuildCorpse();
            BuildCarronero(baseCtrl);
            AssetDatabase.SaveAssets();

            var zr = GameObject.Find("Zombies").transform;
            foreach (var n in new[] { "X_Reptante", "X_Carronero" }) { var o = GameObject.Find(n); if (o != null) Object.DestroyImmediate(o); }
            foreach (var n in new[] { "X_Cadaver", "X_Sangre" }) { var o = GameObject.Find(n); if (o != null) Object.DestroyImmediate(o); }

            // sin katana: los zombis del Yaku salen y entran otros modelos en su sitio
            var swaps = new[] { ("Zombie_Yaku", "Zombie_Oficial", "Zombie_Oficial_Hall"), ("Z2_Yaku_Almacen", "Zombie_Pxl3", "Z2_Pxl3_Almacen") };
            foreach (var (oldName, prefab, newName) in swaps)
            {
                var old = GameObject.Find(oldName);
                if (old == null) { log.Add(oldName + ": no estaba"); continue; }
                var t = old.transform; var pos = t.position; var yaw = t.eulerAngles.y;
                Object.DestroyImmediate(old);
                Place(prefab, newName, pos, yaw, zr);
                log.Add(oldName + " -> " + newName);
            }

            // reptante: ya arrastrandose, en el pasillo central de la zona 2 (z 54-66, x -1.5..1.5)
            Place("Zombie_OficialReptante", "X_Reptante", new Vector3(0.4f, 0.5f, 60.5f), 180f, zr);

            // carronero + cadaver + sangre, en la sala de maquinas (zona 2, z 44-54), lado este
            var fp = new Vector3(6.4f, 1.0f, 46.6f);
            var feeder = Place("Zombie_Carronero", "X_Carronero", fp, 0f, zr);
            var corpse = Place("Cadaver_Civil", "X_Cadaver", new Vector3(5.3f, 0f, 47.18f), 90f, GameObject.Find("--- LEVEL ---").transform);   // el pecho queda bajo la cabeza del carronero al morder
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "X_Sangre"; Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(GameObject.Find("--- LEVEL ---").transform);
            q.transform.SetPositionAndRotation(new Vector3(6.2f, 0.015f, 47.1f), Quaternion.Euler(90f, 20f, 0f));
            q.transform.localScale = new Vector3(3.4f, 3.4f, 1f);
            var rr = q.GetComponent<Renderer>(); rr.sharedMaterial = blood; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            log.Add("X_Reptante, X_Carronero (+ cadaver y sangre) colocados");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return string.Join(" | ", log);
        }
    }
}
#endif
