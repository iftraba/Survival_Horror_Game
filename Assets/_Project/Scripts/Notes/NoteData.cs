using UnityEngine;

namespace Horror
{
    public enum NoteCategory { Story, Puzzle }

    /// <summary>Una nota leible del juego. Se lee en el mundo y queda registrada en el Archivo (pestana de Tab).</summary>
    [CreateAssetMenu(menuName = "Horror/Note", fileName = "NewNote")]
    public class NoteData : ScriptableObject
    {
        [Tooltip("Identificador estable para el guardado: no lo cambies")] public string id;
        public string title = "Nota";
        public NoteCategory category = NoteCategory.Story;
        [TextArea(4, 20)] public string body;
        [Tooltip("Linea destacada (a mano, en grande), por ejemplo un codigo")] public string highlight;
        [Tooltip("Dibujo opcional bajo el texto")] public Sprite image;
        [Tooltip("Si no esta vacio, al leerla por primera vez el objetivo pasa a este texto")] public string objective;
    }
}
