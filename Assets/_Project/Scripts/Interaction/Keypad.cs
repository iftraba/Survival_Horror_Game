using UnityEngine;

namespace Horror
{
    /// <summary>Estado del teclado numerico abierto sobre una taquilla con codigo (la pantalla la dibuja el Hud).</summary>
    public static class Keypad
    {
        public static LockerDoor Target { get; private set; }
        public static string Entry { get; private set; } = "";
        public static bool Wrong { get; private set; }
        static float wrongUntil;

        public static bool ShowingWrong => Wrong && Time.realtimeSinceStartup < wrongUntil;

        public static void Open(LockerDoor locker)
        {
            Target = locker; Entry = ""; Wrong = false;
            GameState.SetKeypadOpen(true);
        }

        public static void Close()
        {
            Target = null; Entry = ""; Wrong = false;
            GameState.SetKeypadOpen(false);
        }

        public static void Press(int digit)
        {
            if (Target == null || ShowingWrong) return;
            Wrong = false;
            if (Entry.Length >= Target.code.Length) return;
            Entry += digit.ToString();
            GameAudio.Play(Sfx.Switch, Vector3.zero, 0.5f, 1.6f, false);
            if (Entry.Length == Target.code.Length) Check();
        }

        public static void Backspace()
        {
            if (Entry.Length > 0 && !ShowingWrong) Entry = Entry.Substring(0, Entry.Length - 1);
        }

        static void Check()
        {
            if (Entry == Target.code)
            {
                var locker = Target;
                Close();
                locker.Unlock();
                Hud.Message("Código correcto");
            }
            else
            {
                Wrong = true;
                wrongUntil = Time.realtimeSinceStartup + 0.9f;
                GameAudio.Play(Sfx.DoorLocked, Vector3.zero, 0.7f, 0.8f, false);
            }
        }

        /// <summary>Tras el aviso de error se borra la entrada para volver a probar.</summary>
        public static void Tick()
        {
            if (Wrong && !ShowingWrong) { Wrong = false; Entry = ""; }
        }
    }
}
