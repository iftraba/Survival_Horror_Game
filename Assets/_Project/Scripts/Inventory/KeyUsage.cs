using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Cuando un objeto clave ya esta "usado por completo": todas las puertas que se abren con el (<see cref="Door.originalKey"/>) estan desbloqueadas.
    /// Las llaves que se gastan al usarlas (medallones, fusibles, llave del ascensor, llave final) desaparecen solas del inventario. Un objeto clave
    /// que nadie usa no cuenta como usado. Mientras no lo este, el inventario no deja tirarlo (solo guardarlo en un baul) y, cuando lo esta,
    /// muestra un check rojo en su casilla y habilita "Tirar" (que lo hace desaparecer del todo).
    /// </summary>
    public static class KeyUsage
    {
        static readonly Dictionary<ItemData, bool> cache = new Dictionary<ItemData, bool>();
        static float cacheUntil;

        /// <summary>Olvida las respuestas guardadas (una puerta acaba de desbloquearse o cargarse).</summary>
        public static void Invalidate() { cache.Clear(); cacheUntil = 0f; }

        public static bool IsSpent(ItemData item)
        {
            if (item == null || !item.IsKey) return false;
            if (Time.unscaledTime >= cacheUntil) { cache.Clear(); cacheUntil = Time.unscaledTime + 0.4f; }     // se consulta por casilla y por fotograma
            if (cache.TryGetValue(item, out bool v)) return v;
            bool any = false, allUnlocked = true;
            foreach (var d in Door.All)
            {
                if (d == null || d.originalKey != item) continue;
                any = true;
                if (!d.IsUnlocked) { allUnlocked = false; break; }
            }
            v = any && allUnlocked;
            cache[item] = v;
            return v;
        }

        /// <summary>Texto para la descripcion del objeto en el inventario ("" si no es un objeto clave).</summary>
        public static string Note(ItemData item)
        {
            if (item == null || !item.IsKey) return "";
            return IsSpent(item) ? "Usado por completo: ya no hace falta. Puedes tirarlo." : "Objeto clave: no se puede tirar. Guardalo en un baul si te estorba.";
        }
    }
}
