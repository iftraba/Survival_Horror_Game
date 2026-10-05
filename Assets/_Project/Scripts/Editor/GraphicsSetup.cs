#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Horror.EditorTools
{
    /// <summary>
    /// Calidad grafica: ajustes del pipeline URP (antialiasing, sombras), oclusion ambiental (SSAO) y post-proceso
    /// (ACES, bloom, grano de pelicula, aberracion cromatica, correccion de color). Idempotente: se puede repetir.
    /// </summary>
    public static class GraphicsSetup
    {
        [MenuItem("Horror/Apply Graphics Quality")]
        public static void Apply()
        {
            var report = new List<string>();
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null) { Debug.LogWarning("[Horror] No hay URP activo."); return; }

            ConfigurePipeline(asset, report);
            AddSsao(asset, report);
            ConfigureVolume(report);
            ConfigureCamera(report);

            AssetDatabase.SaveAssets();
            Debug.Log("[Horror] Calidad grafica aplicada:\n - " + string.Join("\n - ", report));
        }

        static void ConfigurePipeline(UniversalRenderPipelineAsset a, List<string> r)
        {
            a.supportsHDR = true;
            a.msaaSampleCount = 4;                                   // bordes suaves (MSAA 4x)
            // Estas dos no tienen setter publico en esta version: se escriben por el objeto serializado
            var so = new SerializedObject(a);
            SetInt(so, "m_SoftShadowsSupported", 1, r);
            SetInt(so, "m_SoftShadowQuality", 3, r);                 // 1 baja, 2 media, 3 alta
            so.ApplyModifiedProperties();
            a.shadowDistance = 40f;
            a.shadowCascadeCount = 4;
            a.mainLightShadowmapResolution = 2048;
            a.additionalLightsShadowmapResolution = 4096;            // atlas de sombras de lamparas y linterna
            a.maxAdditionalLightsCount = 8;
            a.colorGradingMode = ColorGradingMode.HighDynamicRange;
            a.colorGradingLutSize = 32;
            EditorUtility.SetDirty(a);
            r.Add($"URP: MSAA {a.msaaSampleCount}x, sombras suaves calidad alta, atlas de sombras {a.additionalLightsShadowmapResolution}, {a.maxAdditionalLightsCount} luces por objeto");
        }

        /// <summary>Oclusion ambiental en espacio de pantalla: oscurece esquinas, bajos de muebles y juntas.</summary>
        static void AddSsao(UniversalRenderPipelineAsset asset, List<string> r)
        {
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null || list.arraySize == 0) { r.Add("SSAO: sin datos de renderer"); return; }
            var data = list.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
            if (data == null) { r.Add("SSAO: renderer no encontrado"); return; }

            ScreenSpaceAmbientOcclusion ssao = null;
            foreach (var f in data.rendererFeatures)
                if (f is ScreenSpaceAmbientOcclusion s) ssao = s;

            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "ScreenSpaceAmbientOcclusion";
                AssetDatabase.AddObjectToAsset(ssao, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out string _, out long localId);

                var dso = new SerializedObject(data);
                var features = dso.FindProperty("m_RendererFeatures");
                var map = dso.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                dso.ApplyModifiedProperties();
                r.Add("SSAO: anadido al renderer");
            }
            else r.Add("SSAO: ya estaba en el renderer, se reajusta");

            // 'settings' no es publico: se ajusta por el objeto serializado
            var sso = new SerializedObject(ssao);
            SetFloat(sso, "m_Settings.Intensity", 1.1f, r);
            SetFloat(sso, "m_Settings.Radius", 0.4f, r);
            SetFloat(sso, "m_Settings.DirectLightingStrength", 0.25f, r);
            SetInt(sso, "m_Settings.AfterOpaque", 0, r);
            sso.ApplyModifiedProperties();
            ssao.SetActive(true);
            EditorUtility.SetDirty(ssao);
            EditorUtility.SetDirty(data);
        }

        static void SetInt(SerializedObject so, string path, int value, List<string> r)
        {
            var p = so.FindProperty(path);
            if (p == null) { r.Add("(no existe la propiedad " + path + ")"); return; }
            if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value != 0;
            else if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = value;
            else p.intValue = value;
        }

        static void SetFloat(SerializedObject so, string path, float value, List<string> r)
        {
            var p = so.FindProperty(path);
            if (p == null) { r.Add("(no existe la propiedad " + path + ")"); return; }
            p.floatValue = value;
        }

        static void ConfigureVolume(List<string> r)
        {
            var vol = Object.FindFirstObjectByType<Volume>();
            if (vol == null || vol.sharedProfile == null) { r.Add("Post-proceso: no hay Global Volume"); return; }
            var p = vol.sharedProfile;

            T Get<T>() where T : VolumeComponent
            {
                if (!p.TryGet(out T c)) c = p.Add<T>(true);
                return c;
            }

            var tone = Get<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);                        // curva de pelicula: altas luces suaves, negros densos

            var bloom = Get<Bloom>();
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1f, 0.92f, 0.82f));
            bloom.highQualityFiltering.Override(true);

            var color = Get<ColorAdjustments>();
            color.postExposure.Override(0.35f);
            color.contrast.Override(14f);
            color.saturation.Override(-14f);                                 // look desaturado y sucio
            color.colorFilter.Override(new Color(0.93f, 0.97f, 1f));

            var wb = Get<WhiteBalance>();
            wb.temperature.Override(-6f);                                    // algo frio en las sombras
            wb.tint.Override(2f);

            var vig = Get<Vignette>();
            vig.intensity.Override(0.36f);
            vig.smoothness.Override(0.5f);

            var grain = Get<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.28f);
            grain.response.Override(0.8f);

            var ca = Get<ChromaticAberration>();
            ca.intensity.Override(0.08f);                                    // sutil: separacion de color en los bordes

            var mb = Get<MotionBlur>();
            mb.quality.Override(MotionBlurQuality.Low);
            mb.intensity.Override(0.12f);

            EditorUtility.SetDirty(p);
            r.Add("Post-proceso: ACES, bloom, correccion de color, balance de blancos, vineta, grano, aberracion cromatica, desenfoque de movimiento ligero");
        }

        static void ConfigureCamera(List<string> r)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var d = cam.GetUniversalAdditionalCameraData();
            d.renderPostProcessing = true;
            d.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;     // SMAA ademas del MSAA
            d.antialiasingQuality = AntialiasingQuality.High;
            d.renderShadows = true;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.nearClipPlane = 0.1f;
            EditorUtility.SetDirty(cam);
            r.Add("Camara: SMAA alta calidad, HDR, post-proceso activo");
        }
    }
}
#endif
