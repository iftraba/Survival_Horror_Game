#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, calidad AA, fase A (menus Horror/Comisaria grande/16 Preparar iluminacion global y 17 Hornear iluminacion global).
    /// Antes la escena no tenia lightmaps, ni sondas de luz, ni de reflexion: toda la luz indirecta era un color plano. Esto prepara y hornea:
    ///  - Ajustes de iluminacion (LightingSettings propio): GPU progresivo, rebotes de luz horneados con luz directa en tiempo real (Baked Indirect),
    ///    6 texeles por metro, 3 rebotes, denoiser, ambiente del cielo.
    ///  - Mallas: UV2 para el lightmap (las cajas de la escena con Unwrapping; los FBX con generateSecondaryUV). Solo las superficies grandes reciben lightmap;
    ///    lo pequeno (menos de 1,2 m) contribuye al rebote pero se ilumina con sondas. Lo transparente (calcomanias, cristales) no participa.
    ///    Los edificios de la ciudad usan un cuarto de resolucion.
    ///  - Luces: lamparas, rellenos y luna en modo Mixed (directa en tiempo real con sus sombras, rebote horneado). Las lamparas muertas, las de salas con
    ///    interruptor y los fuegos (parpadean) siguen en tiempo real sin rebote horneado: si no, la sala apagada brillaria con el rebote.
    ///  - Sondas de luz en rejilla por sala (2,5 m, tres alturas) y en la calle, y una sonda de reflexion de caja por sala y en la calle.
    /// Repetible: rehace "GI_Sondas" y vuelve a poner los modos. Hay que repetir 16 y 17 despues de rehacer el nivel o mover muebles estaticos.
    /// </summary>
    public static class ComisariaGrandeGI
    {
        const string SettingsPath = "Assets/Scenes/Comisaria_v2/Iluminacion_GI.lighting";

        [MenuItem("Horror/Comisaria grande/16 Preparar iluminacion global")]
        public static void MenuPrepare() { Debug.Log("[Horror] " + Prepare()); }

        [MenuItem("Horror/Comisaria grande/17 Hornear iluminacion global")]
        public static void MenuBake() { Debug.Log("[Horror] " + Bake()); }

        public static string Prepare()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---"); if (level == null) return "falta el nivel";
            var log = new List<string>();

            // ---- ajustes de iluminacion
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (ls == null) { ls = new LightingSettings { name = "Iluminacion_GI" }; AssetDatabase.CreateAsset(ls, SettingsPath); }
            ls.bakedGI = true; ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.lightmapResolution = 6f; ls.lightmapPadding = 4; ls.lightmapMaxSize = 2048;
            ls.directSampleCount = 32; ls.indirectSampleCount = 256; ls.environmentSampleCount = 128; ls.maxBounces = 3;
            ls.ao = true; ls.aoMaxDistance = 0.8f; ls.aoExponentIndirect = 1f; ls.aoExponentDirect = 0f;
            ls.filteringMode = LightingSettings.FilterMode.Auto;
            ls.lightmapCompression = LightmapCompression.NormalQuality;
            ls.directionalityMode = LightmapsMode.CombinedDirectional;
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;
            RenderSettings.ambientMode = AmbientMode.Skybox; RenderSettings.ambientIntensity = 1f;
            log.Add("ajustes listos");

            // ---- 88 luces de relleno fuera: el rebote horneado las sustituye (si alguna sala queda oscura, el menu 15 vuelve a medir y anadir)
            var fill = level.transform.Find("Luz_Relleno"); if (fill != null) { Object.DestroyImmediate(fill.gameObject); log.Add("relleno retirado"); }

            // ---- modos de las luces
            var switched = new HashSet<CeilingLamp>(Object.FindObjectsByType<LightSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None).SelectMany(s => s.lamps ?? new CeilingLamp[0]).Where(l => l != null));
            int mixed = 0, rt = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var lamp = l.GetComponentInParent<CeilingLamp>();
                bool realtimeOnly = l.GetComponentInParent<FireLight>() != null || (lamp != null && (lamp.dead || switched.Contains(lamp))) || l.transform.root.name != level.name && l.type != LightType.Directional;
                if (l.GetComponent<FireLight>() != null) realtimeOnly = true;
                l.lightmapBakeType = realtimeOnly ? LightmapBakeType.Realtime : LightmapBakeType.Mixed;
                if (!realtimeOnly) { l.bounceIntensity = 1.2f; mixed++; } else rt++;
                EditorUtility.SetDirty(l);
            }
            log.Add("luces mixed=" + mixed + " tiempo real=" + rt);

            // ---- mallas: quien recibe lightmap, quien sondas, quien no participa; UV2
            var fbx = new HashSet<string>(); int lm = 0, probe = 0, none = 0, unwrapped = 0;
            foreach (var mr in level.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = mr.gameObject;
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                bool isStatic = (flags & StaticEditorFlags.ContributeGI) != 0;
                bool transparent = mr.sharedMaterials.Any(m => m != null && m.renderQueue >= 2450);
                if (!isStatic) continue;
                if (transparent || mr.GetComponentInParent<CeilingLamp>() != null)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, flags & ~StaticEditorFlags.ContributeGI);
                    none++; continue;
                }
                var b = mr.bounds; float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                if (big < 1.2f) { mr.receiveGI = ReceiveGI.LightProbes; probe++; continue; }
                mr.receiveGI = ReceiveGI.Lightmaps; lm++;
                bool city = go.name.StartsWith("Edificio_") || go.transform.parent != null && (go.transform.parent.name == "Ciudad_Detalle");
                mr.scaleInLightmap = city ? 0.2f : 1f;
                var mesh = mf.sharedMesh; string path = AssetDatabase.GetAssetPath(mesh);
                if (mesh.uv2 != null && mesh.uv2.Length > 0) continue;
                if (string.IsNullOrEmpty(path)) { Unwrapping.GenerateSecondaryUVSet(mesh); unwrapped++; }
                else if (path.EndsWith(".fbx") || path.EndsWith(".FBX")) fbx.Add(path);
                else if (path.EndsWith(".asset")) { Unwrapping.GenerateSecondaryUVSet(mesh); EditorUtility.SetDirty(mesh); unwrapped++; }
            }
            foreach (var p in fbx)
            {
                var imp = AssetImporter.GetAtPath(p) as ModelImporter; if (imp == null || imp.generateSecondaryUV) continue;
                imp.generateSecondaryUV = true; imp.SaveAndReimport();
            }
            log.Add("lightmap=" + lm + " sondas=" + probe + " fuera=" + none + " UV2 escena=" + unwrapped + " UV2 FBX=" + fbx.Count);

            // ---- sondas de luz y de reflexion
            var old = level.transform.Find("GI_Sondas"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("GI_Sondas").transform; root.SetParent(level.transform);
            ComisariaGrande.Define();
            var pts = new List<Vector3>(); int refl = 0;
            foreach (var r in ComisariaGrande.Rooms)
            {
                if (r.kind == ComisariaGrande.Kind.Shaft) continue;
                float h = Mathf.Max(2.2f, r.ceil - r.floor);
                for (float x = r.r.xMin + 0.6f; x <= r.r.xMax - 0.6f + 0.01f; x += 2.5f)
                    for (float z = r.r.yMin + 0.6f; z <= r.r.yMax - 0.6f + 0.01f; z += 2.5f)
                        foreach (float y in new[] { 0.4f, 1.6f, h - 0.4f }) pts.Add(new Vector3(x, r.floor + y, z));
                if (!r.Indoor) continue;
                var rg = new GameObject("Reflexion_" + r.id); rg.transform.SetParent(root); rg.transform.position = new Vector3(r.r.center.x, r.floor + Mathf.Min(1.7f, h * 0.5f), r.r.center.y);
                var rp = rg.AddComponent<ReflectionProbe>(); rp.mode = ReflectionProbeMode.Baked; rp.boxProjection = true; rp.resolution = 128; rp.importance = 1;
                rp.size = new Vector3(r.r.width, h, r.r.height); rp.center = new Vector3(0, r.floor + h / 2f - rg.transform.position.y, 0); rp.blendDistance = 0.5f; rp.hdr = true;
                refl++;
            }
            // la calle y el patio
            for (float x = -80f; x <= 80f; x += 4f) for (float z = -19f; z <= -1f; z += 4f) foreach (float y in new[] { 0.5f, 2.5f, 6f }) pts.Add(new Vector3(x, y, z));
            var sg = new GameObject("Reflexion_Calle"); sg.transform.SetParent(root); sg.transform.position = new Vector3(0, 3f, -12f);
            var sp = sg.AddComponent<ReflectionProbe>(); sp.mode = ReflectionProbeMode.Baked; sp.boxProjection = true; sp.resolution = 256; sp.size = new Vector3(170f, 14f, 24f); sp.center = new Vector3(0, 4f, 0); sp.hdr = true;
            var lpg = root.gameObject.AddComponent<LightProbeGroup>(); lpg.probePositions = pts.Select(p => root.InverseTransformPoint(p)).ToArray();
            log.Add("sondas de luz=" + pts.Count + " de reflexion=" + (refl + 1));

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "GI preparada: " + string.Join("; ", log);
        }

        public static string Bake()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            if (Lightmapping.isRunning) return "ya se esta horneando";
            EditorSceneManager.SaveOpenScenes();
            return Lightmapping.BakeAsync() ? "horneando en segundo plano (Lightmapping.isRunning; al acabar se guarda solo)" : "no se pudo empezar";
        }
    }
}
#endif
