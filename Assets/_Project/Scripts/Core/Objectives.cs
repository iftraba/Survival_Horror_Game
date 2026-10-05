using System;
using UnityEngine;

namespace Horror
{
    /// <summary>Objetivo actual de la partida. Se muestra en el HUD y se guarda con la partida.</summary>
    public static class Objectives
    {
        public static string Current { get; private set; } = "";
        public static event Action<string> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Current = "";
            Changed = null;
        }

        public static void Set(string text, bool announce = true)
        {
            if (Current == text) return;
            Current = text ?? "";
            Changed?.Invoke(Current);
            if (announce && !string.IsNullOrEmpty(Current)) Hud.Message("Nuevo objetivo");
        }
    }
}
