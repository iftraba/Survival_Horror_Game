using UnityEngine;

namespace Horror
{
    /// <summary>Opciones del jugador (volumen y sensibilidad del raton), guardadas en PlayerPrefs.</summary>
    public static class GameSettings
    {
        public const string SceneV1 = "Comisaria", SceneV2 = "Comisaria_v2";

        /// <summary>Escena de juego (2026-10-08): la comisaria nueva si la build la incluye, si no la original. Asi la misma
        /// version del codigo sirve para las dos builds: "Empezar partida", cargar y volver del menu van siempre a la de la build.</summary>
        public static string GameScene => Application.CanStreamedLevelBeLoaded(SceneV2) ? SceneV2 : SceneV1;

        /// <summary>True en la build de la comisaria nueva (sus guardados van aparte de los de la original).</summary>
        public static bool IsV2 => GameScene == SceneV2;
        public const string MenuScene = "MainMenu";

        /// <summary>Personaje jugable (menu oculto: F3 siete veces en el menu principal, 2026-10-17). 0 = soldado, 1 = chica.
        /// Se elige antes de cargar la escena; PlayerModelSwap lo aplica al aparecer el jugador y va en la partida guardada.</summary>
        public static int Character;
        public static readonly string[] CharacterNames = { "Soldado", "Chica" };

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
