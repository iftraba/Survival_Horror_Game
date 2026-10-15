#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, calidad AA, fase A (menu Horror/Comisaria grande/18 Etalonaje y postproceso): ajusta el volumen global con un aspecto de cine de
    /// terror: ACES, exposicion, contraste en sombras con un tono frio verdoso (sombras azul verdosas, luces calidas), saturacion algo baja, viñeta, grano de
    /// pelicula fino, aberracion cromatica minima en los bordes y bloom suave con umbral alto (solo bombillas y fuego). Repetible.
    /// </summary>
    public static class ComisariaGrandePost
    {
        [MenuItem("Horror/Comisaria grande/18 Etalonaje y postproceso")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        static T Get<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (!p.TryGet<T>(out var c)) c = p.Add<T>(true);
            c.active = true;
            return c;
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var vol = Object.FindFirstObjectByType<Volume>();
            if (vol == null || vol.sharedProfile == null) return "no hay volumen global";
            var p = vol.sharedProfile;
            var tm = Get<Tonemapping>(p); tm.mode.Override(TonemappingMode.ACES);
            var ca = Get<ColorAdjustments>(p);
            ca.postExposure.Override(0.35f); ca.contrast.Override(14f); ca.saturation.Override(-10f); ca.colorFilter.Override(new Color(0.96f, 0.98f, 1f));
            var smh = Get<ShadowsMidtonesHighlights>(p);
            smh.shadows.Override(new Vector4(0.96f, 1.0f, 1.03f, 0.02f));
            smh.midtones.Override(new Vector4(1.0f, 1.0f, 0.99f, 0f));
            smh.highlights.Override(new Vector4(1.04f, 1.0f, 0.94f, 0.02f));
            var wb = Get<WhiteBalance>(p); wb.temperature.Override(-4f); wb.tint.Override(2f);
            var bl = Get<Bloom>(p); bl.threshold.Override(1.15f); bl.intensity.Override(0.55f); bl.scatter.Override(0.6f); bl.highQualityFiltering.Override(true);
            var vg = Get<Vignette>(p); vg.intensity.Override(0.26f); vg.smoothness.Override(0.45f); vg.color.Override(Color.black);
            var fg = Get<FilmGrain>(p); fg.type.Override(FilmGrainLookup.Thin1); fg.intensity.Override(0.22f); fg.response.Override(0.8f);
            var cab = Get<ChromaticAberration>(p); cab.intensity.Override(0.08f);
            var lg = Get<LiftGammaGain>(p); lg.lift.Override(new Vector4(0.98f, 1.0f, 1.02f, 0.01f));
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return "postproceso: ACES, etalonaje frio en sombras y calido en luces, vineta, grano fino, aberracion minima y bloom de umbral alto";
        }
    }
}
#endif
