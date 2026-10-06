using System.Collections.Generic;
using UnityEngine;

namespace Horror
{
    /// <summary>Notas leidas por el jugador (el Archivo) y la nota que se esta leyendo ahora. Se guarda en la partida.</summary>
    public static class NoteArchive
    {
        static readonly List<NoteData> read = new List<NoteData>();

        public static IReadOnlyList<NoteData> Read => read;
        /// <summary>Nota abierta en pantalla (null si no hay).</summary>
        public static NoteData Reading { get; private set; }

        public static bool Has(NoteData n) => read.Contains(n);

        /// <summary>Registra la nota. Devuelve true si es la primera vez que se lee.</summary>
        public static bool Add(NoteData n)
        {
            if (n == null || read.Contains(n)) return false;
            read.Add(n);
            return true;
        }

        public static void Open(NoteData n) { Reading = n; GameState.SetNoteOpen(n != null); }
        public static void Close() { Reading = null; GameState.SetNoteOpen(false); }

        public static void Clear() { read.Clear(); Reading = null; }

        public static List<string> Ids()
        {
            var ids = new List<string>();
            foreach (var n in read) ids.Add(n.id);
            return ids;
        }

        public static void Restore(IEnumerable<string> ids)
        {
            read.Clear();
            if (ids == null) return;
            foreach (var id in ids)
            {
                var n = ItemDatabase.FindNote(id);
                if (n != null && !read.Contains(n)) read.Add(n);
            }
        }

        public static List<NoteData> Of(NoteCategory c)
        {
            var list = new List<NoteData>();
            foreach (var n in read) if (n.category == c) list.Add(n);
            return list;
        }
    }
}
