using UnityEngine;

namespace Horror
{
    /// <summary>Nota en el mundo (un papel sobre una mesa). E la abre a pantalla completa y la anade al Archivo; se queda donde esta.</summary>
    public class ReadableNote : MonoBehaviour, IInteractable
    {
        public NoteData note;

        public string Prompt => note != null ? "E  Leer: " + note.title : "";

        public void Interact(GameObject who)
        {
            if (note == null) return;
            bool first = NoteArchive.Add(note);
            GameAudio.Play(Sfx.Pickup, who.transform.position, 0.6f, 1.3f, false);
            if (first)
            {
                Hud.Message("Nota añadida al Archivo (Tab)");
                if (!string.IsNullOrEmpty(note.objective)) Objectives.Set(note.objective);
            }
            NoteArchive.Open(note);
        }
    }
}
