using UnityEngine;

namespace Horror
{
    /// <summary>Lista de todos los objetos del juego, para poder reconstruirlos por nombre al cargar una partida.</summary>
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        public ItemData[] items;
        [Tooltip("Todas las notas del juego, para reconstruir el Archivo por id al cargar")] public NoteData[] notes;

        public static NoteData FindNote(string id)
        {
            if (Instance == null || Instance.notes == null || string.IsNullOrEmpty(id)) return null;
            foreach (var n in Instance.notes)
                if (n != null && n.id == id) return n;
            return null;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static ItemData Find(string displayName)
        {
            if (Instance == null || Instance.items == null) return null;
            foreach (var i in Instance.items)
                if (i != null && i.displayName == displayName) return i;
            return null;
        }
    }
}
