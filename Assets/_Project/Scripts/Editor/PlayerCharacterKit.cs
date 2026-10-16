#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Personajes jugables del menu oculto (2026-10-17). Importa la chica de Meshy (biped con rig de Mixamo) como Humanoid,
    /// le hace un material URP con sus texturas, monta su prefab de modelo (Animator del protagonista y agarres del arma en la
    /// mano derecha, calculados como en PlayerKit con la pose de apuntar) y lo registra en PlayerModelSwap del prefab Player.
    /// </summary>
    public static class PlayerCharacterKit
    {
        const string Dir = "Assets/_Project/Art/Mixamo/Characters/Chica/";
        const string Fbx = Dir + "Player_Chica.fbx";
        const string ModelPrefab = "Assets/_Project/Prefabs/Characters/PlayerModel_Chica.prefab";
        const string PlayerPrefab = "Assets/_Project/Prefabs/Characters/Player.prefab";

        [MenuItem("Horror/Personajes/Construir chica (menu oculto)")]
        public static void BuildMenu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            var log = new List<string>();
            Import(log);
            var mat = Material(log);
            var model = Prefab(mat, log);
            Register(model, log);
            AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }

        static void Import(List<string> log)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            bool dirty = false;
            if (mi.animationType != ModelImporterAnimationType.Human) { mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; dirty = true; }
            if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }        // trae una carrera de Meshy: se usan las del protagonista
            if (mi.materialImportMode != ModelImporterMaterialImportMode.None) { mi.materialImportMode = ModelImporterMaterialImportMode.None; dirty = true; }
            if (dirty) mi.SaveAndReimport();
            var av = AssetDatabase.LoadAssetAtPath<Avatar>(Fbx);
            log.Add("avatar " + (av != null && av.isValid && av.isHuman ? "humanoide valido" : "NO VALIDO"));
        }

        static Texture2D Tex(string path, bool normal, bool srgb)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ti.textureType != type || ti.sRGBTexture != srgb || ti.maxTextureSize != 4096)
            {
                ti.textureType = type; ti.sRGBTexture = srgb; ti.maxTextureSize = 4096;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Metal en RGB y 1 - rugosidad en alfa (mapa de metal de URP), a partir de los dos mapas de Meshy.</summary>
        static void PackMs(List<string> log)
        {
            string ms = Dir + "Chica_ms.png", mSrc = Dir + "Chica_metal_src.png", rSrc = Dir + "Chica_rough_src.png";
            if (File.Exists(ms) || !File.Exists(mSrc) || !File.Exists(rSrc)) return;
            var m = new Texture2D(2, 2, TextureFormat.RGBA32, false, true); m.LoadImage(File.ReadAllBytes(mSrc));
            var r = new Texture2D(2, 2, TextureFormat.RGBA32, false, true); r.LoadImage(File.ReadAllBytes(rSrc));
            int w = m.width, h = m.height;
            var mp = m.GetPixels32();
            var rp = r.width == w && r.height == h ? r.GetPixels32() : null;
            var o = new Color32[mp.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    byte rough = rp != null ? rp[i].r : (byte)(r.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h).r * 255f);
                    o[i] = new Color32(mp[i].r, mp[i].r, mp[i].r, (byte)(255 - rough));
                }
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            t.SetPixels32(o);
            File.WriteAllBytes(ms, t.EncodeToPNG());
            Object.DestroyImmediate(m); Object.DestroyImmediate(r); Object.DestroyImmediate(t);
            AssetDatabase.DeleteAsset(mSrc); AssetDatabase.DeleteAsset(rSrc);
            AssetDatabase.ImportAsset(ms);
            log.Add("metal-suavidad empaquetado " + w + "x" + h);
        }

        static Material Material(List<string> log)
        {
            PackMs(log);
            string path = Dir + "Chica_mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", Tex(Dir + "Chica_diff.png", false, true));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", Tex(Dir + "Chica_nor.png", true, false));
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            var ms = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Chica_ms.png");
            if (ms != null)
            {
                ms = Tex(Dir + "Chica_ms.png", false, false);
                mat.SetTexture("_MetallicGlossMap", ms);
                mat.SetFloat("_Smoothness", 1f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            EditorUtility.SetDirty(mat);
            log.Add("material " + path);
            return mat;
        }

        static GameObject Prefab(Material mat, List<string> log)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            var pa = player.GetComponent<PlayerAnimation>();
            var soldier = player.transform.Find("Model");

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = "Model";
            inst.transform.position = Vector3.zero; inst.transform.rotation = Quaternion.identity;
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mats = new Material[smr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                smr.sharedMaterials = mats;
                smr.updateWhenOffscreen = false;
            }
            var an = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            an.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(Fbx);
            an.runtimeAnimatorController = pa.handgunController;
            an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // altura: cabeza sobre la raiz con la misma animacion de reposo (la pose del FBX no vale: el rig de Meshy va en cm);
            // la chica queda al 95 % del soldado
            float h = HeadHeight(inst, an), hs = soldier != null ? HeadHeight(soldier.gameObject, soldier.GetComponent<Animator>()) : 1.48f;
            float k = hs * 0.95f / h;
            inst.transform.localScale = Vector3.one * k;
            log.Add("cabeza chica " + h.ToString("0.00") + " m, soldado " + hs.ToString("0.00") + " m, escala x" + k.ToString("0.000"));

            // agarre de las armas (como PlayerKit.Build): en la pose de apuntar, el cañon va de la mano derecha a la izquierda
            Vector3 lp; Quaternion lr;
            var aimClip = PlayerKit.First("P_RifleAimingIdle");
            AnimationMode.StartAnimationMode();
            AnimationMode.SampleAnimationClip(inst, aimClip, 0.5f);
            var rh = an.GetBoneTransform(HumanBodyBones.RightHand); var lh = an.GetBoneTransform(HumanBodyBones.LeftHand); var rl = an.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Vector3 dir = (lh.position - rh.position).normalized;
            Vector3 handDir = (rh.position - rl.position).normalized;
            Vector3 worldPos = rh.position + handDir * 0.06f * inst.transform.localScale.x;
            Quaternion worldRot = Quaternion.LookRotation(dir, inst.transform.up);
            lp = rh.InverseTransformPoint(worldPos);                                 // el rig de Meshy lleva los huesos en cm (escala x100)
            lr = Quaternion.Inverse(rh.rotation) * worldRot;
            AnimationMode.StopAnimationMode();
            foreach (var n in new[] { "WeaponHolder", "LongWeaponHolder" })
            {
                var t = new GameObject(n).transform;
                t.SetParent(rh, false);
                t.localPosition = lp; t.localRotation = lr;
                t.localScale = new Vector3(1f / rh.lossyScale.x, 1f / rh.lossyScale.y, 1f / rh.lossyScale.z);   // el arma a su tamaño aunque el rig venga en cm
            }
            var go = PrefabUtility.SaveAsPrefabAsset(inst, ModelPrefab);
            Object.DestroyImmediate(inst);
            log.Add("prefab " + ModelPrefab);
            return go;
        }

        /// <summary>Altura del hueso de la cabeza sobre la raiz en la pose de reposo del protagonista (P_Idle).</summary>
        static float HeadHeight(GameObject model, Animator an)
        {
            var go = model.scene.IsValid() && model.transform.parent == null ? model : (GameObject)Object.Instantiate(model);
            go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
            AnimationMode.StartAnimationMode();
            AnimationMode.SampleAnimationClip(go, PlayerKit.First("P_Idle", "P_BreathingIdle"), 0.1f);
            float y = go.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).position.y - go.transform.position.y;
            AnimationMode.StopAnimationMode();
            if (go != model) Object.DestroyImmediate(go);
            return y;
        }

        static void Register(GameObject model, List<string> log)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            var sw = root.GetComponent<PlayerModelSwap>() ?? root.AddComponent<PlayerModelSwap>();
            sw.models = new GameObject[] { null, model };
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            PrefabUtility.UnloadPrefabContents(root);
            log.Add("PlayerModelSwap en Player.prefab");
        }
    }
}
#endif
