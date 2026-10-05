#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Importacion de personajes y animaciones de Mixamo como Humanoid.
    /// - Personaje (con piel): avatar Humanoid propio y material del proyecto (Mixamo conserva las UV del modelo).
    /// - Animaciones (sin piel): Humanoid, raiz fija (el movimiento lo pone el NavMesh/CharacterController),
    ///   bucle en las ciclicas, y se mide la velocidad real de la zancada para que los pies no patinen.
    /// </summary>
    public static class MixamoImport
    {
        static readonly string[] LoopWords = { "idle", "walk", "run", "crawl", "strafe", "aiming" };

        public static string ConfigureCharacter(string fbxPath, string materialPath)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            if (imp == null) return "no existe " + fbxPath;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            imp.SaveAndReimport();
            if (mat != null)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                    if (o is Material m) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
                imp.SaveAndReimport();
            }
            var avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
            return $"{Path.GetFileName(fbxPath)}: avatar humano valido={(avatar != null && avatar.isHuman && avatar.isValid)}";
        }

        /// <summary>Configura todas las animaciones de una carpeta. Devuelve nombre de clip -> velocidad de zancada (m/s).</summary>
        public static Dictionary<string, float> ConfigureAnimations(string folder, string measureAvatarFbx)
        {
            var speeds = new Dictionary<string, float>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = true;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.SaveAndReimport();

                string clipName = Path.GetFileNameWithoutExtension(path);
                bool loop = LoopWords.Any(w => clipName.ToLowerInvariant().Contains(w));
                var clips = imp.defaultClipAnimations;
                foreach (var c in clips)
                {
                    c.name = clipName;
                    c.loopTime = loop;
                    c.loopPose = loop;
                    // raiz: sin giro ni desplazamiento (el juego mueve al personaje); altura segun los pies
                    c.lockRootRotation = true;
                    c.keepOriginalOrientation = true;
                    c.lockRootHeightY = true;
                    c.heightFromFeet = true;
                    c.keepOriginalPositionY = false;
                    c.lockRootPositionXZ = true;
                    c.keepOriginalPositionXZ = true;
                }
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
            }
            // velocidad de la zancada: con la raiz fija, el pie apoyado se desliza hacia atras a la velocidad de la marcha
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(measureAvatarFbx);
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__"));
                if (clip == null || !clip.isLooping || model == null) continue;
                speeds[clip.name] = MeasureStride(model, clip);
            }
            return speeds;
        }

        static float MeasureStride(GameObject modelPrefab, AnimationClip clip)
        {
            var inst = Object.Instantiate(modelPrefab);
            try
            {
                var anim = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
                var lf = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                var rf = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                if (lf == null || rf == null) return 0f;
                const int n = 60;
                var speeds = new List<float>();
                Vector3 pl = default, pr = default;
                for (int i = 0; i <= n; i++)
                {
                    float t = clip.length * i / n;
                    AnimationMode.StartAnimationMode();
                    AnimationMode.SampleAnimationClip(inst, clip, t);
                    Vector3 l = inst.transform.InverseTransformPoint(lf.position);
                    Vector3 r = inst.transform.InverseTransformPoint(rf.position);
                    AnimationMode.StopAnimationMode();
                    if (i > 0)
                    {
                        float dt = clip.length / n;
                        // el pie mas bajo es el que apoya: su retroceso (eje -Z) es la velocidad de avance
                        Vector3 low = l.y < r.y ? l : r, lowPrev = l.y < r.y ? pl : pr;
                        float v = -(low.z - lowPrev.z) / dt;
                        if (v > 0f) speeds.Add(v);
                    }
                    pl = l; pr = r;
                }
                if (speeds.Count == 0) return 0f;
                speeds.Sort();
                return speeds[speeds.Count / 2];   // mediana: ignora los saltos en el cambio de pie
            }
            finally { Object.DestroyImmediate(inst); }
        }
    }
}
#endif
