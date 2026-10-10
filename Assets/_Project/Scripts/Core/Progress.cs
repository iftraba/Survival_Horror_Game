using System;
using System.Collections.Generic;

namespace Horror
{
    /// <summary>
    /// Marcas de progreso de la partida (2026-10-08): medallones colocados, reja del memorial abierta, ascensor en marcha...
    /// Se guardan con la partida (SaveData.flags) y los objetos que dependen de ellas escuchan Changed.
    /// </summary>
    public static class Progress
    {
        static readonly HashSet<string> flags = new HashSet<string>();
        public static event Action Changed;

        public static bool Has(string id) => !string.IsNullOrEmpty(id) && flags.Contains(id);

        public static void Set(string id)
        {
            if (!string.IsNullOrEmpty(id) && flags.Add(id)) Changed?.Invoke();
        }

        public static List<string> All => new List<string>(flags);

        /// <summary>True mientras se limpian o restauran las marcas (partida nueva o cargada): las secuencias no deben reproducirse por ellas.</summary>
        public static bool Restoring { get; private set; }

        public static void Clear() { flags.Clear(); Restoring = true; try { Changed?.Invoke(); } finally { Restoring = false; } }

        public static void Restore(IEnumerable<string> ids)
        {
            flags.Clear();
            if (ids != null) foreach (var i in ids) if (!string.IsNullOrEmpty(i)) flags.Add(i);
            Restoring = true; try { Changed?.Invoke(); } finally { Restoring = false; }
        }
    }
}
