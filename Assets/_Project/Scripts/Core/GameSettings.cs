using UnityEngine;

namespace Horror
{
    /// <summary>Opciones del jugador (volumen y sensibilidad del raton), guardadas en PlayerPrefs.</summary>
    public static class GameSettings
    {
        public const string GameScene = "Comisaria";
        public const string MenuScene = "MainMenu";

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("opt_volume", 1f);
            set { PlayerPrefs.SetFloat("opt_volume", Mathf.Clamp01(value)); ApplyAudio(); }
        }

        /// <summary>Multiplicador de la sensibilidad base de la camara (0.3 - 2.5).</summary>
        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat("opt_sens", 1f);
            set => PlayerPrefs.SetFloat("opt_sens", Mathf.Clamp(value, 0.3f, 2.5f));
        }

        public static void ApplyAudio() => AudioListener.volume = Volume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() => ApplyAudio();

        public static void Save() => PlayerPrefs.Save();
    }
}
