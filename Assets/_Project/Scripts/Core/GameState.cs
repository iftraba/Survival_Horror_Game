using UnityEngine;

namespace Horror
{
    /// <summary>Estado global: pausa por inventario, baul o menu, muerte, victoria y cursor.</summary>
    public static class GameState
    {
        public static bool InventoryOpen { get; private set; }
        /// <summary>Pantalla del baul de objetos (sala segura).</summary>
        public static bool BoxOpen { get; private set; }
        /// <summary>Menu de guardado de la maquina de escribir/terminal.</summary>
        public static bool SaveMenuOpen { get; private set; }
        /// <summary>Lectura de una nota a pantalla completa.</summary>
        public static bool NoteOpen { get; private set; }
        /// <summary>Teclado numerico de una taquilla con codigo.</summary>
        public static bool KeypadOpen { get; private set; }
        /// <summary>Pantalla de objeto conseguido (riñonera...).</summary>
        public static bool ShowcaseOpen { get; private set; }
        public static bool PlayerDead { get; private set; }
        public static bool Paused { get; private set; }
        public static bool Victory { get; private set; }
        public static bool InputBlocked => InventoryOpen || BoxOpen || SaveMenuOpen || NoteOpen || KeypadOpen || ShowcaseOpen || PlayerDead || Paused || Victory;
        /// <summary>Fotograma en que se abrio un menu: la tecla que lo abrio no debe actuar dentro de el.</summary>
        public static int MenuOpenedFrame { get; private set; } = -1;

        public static event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            InventoryOpen = false;
            BoxOpen = false;
            SaveMenuOpen = false;
            NoteOpen = false;
            KeypadOpen = false;
            ShowcaseOpen = false;
            PlayerDead = false;
            Paused = false;
            Victory = false;
        }

        /// <summary>Deja todo limpio (al reiniciar o cargar una partida) y devuelve el control al jugador.</summary>
        public static void ResetAll()
        {
            Reset();
            Apply();
        }

        public static void SetInventoryOpen(bool open) { InventoryOpen = open; Opened(open); Apply(); }
        public static void SetBoxOpen(bool open) { BoxOpen = open; Opened(open); Apply(); }
        public static void SetSaveMenuOpen(bool open) { SaveMenuOpen = open; Opened(open); Apply(); }
        public static void SetNoteOpen(bool open) { NoteOpen = open; Opened(open); Apply(); }
        public static void SetKeypadOpen(bool open) { KeypadOpen = open; Opened(open); Apply(); }
        public static void SetShowcaseOpen(bool open) { ShowcaseOpen = open; Opened(open); Apply(); }
        public static void SetPlayerDead(bool dead) { PlayerDead = dead; Apply(); }
        public static void SetPaused(bool paused) { Paused = paused; Apply(); }
        public static void SetVictory(bool victory) { Victory = victory; Apply(); }

        static void Opened(bool open) { if (open) MenuOpenedFrame = Time.frameCount; }

        static void Apply()
        {
            Time.timeScale = (InventoryOpen || BoxOpen || SaveMenuOpen || NoteOpen || KeypadOpen || ShowcaseOpen || Paused || Victory) ? 0f : 1f;
            Cursor.lockState = InputBlocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = InputBlocked;
            Changed?.Invoke();
        }
    }
}
