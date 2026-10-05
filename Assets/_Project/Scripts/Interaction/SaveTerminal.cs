using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Terminal de guardado de una sala segura: abre el menu de guardado (el HUD lo dibuja y confirma).
    /// Solo hay terminales en las salas seguras, asi que solo se puede guardar alli.
    /// </summary>
    public class SaveTerminal : MonoBehaviour, IInteractable
    {
        public string Prompt => "E  Usar terminal (guardar partida)";

        public void Interact(GameObject who)
        {
            GameAudio.Play(Sfx.Switch, transform.position, 0.9f, 0.8f, false);
            GameState.SetSaveMenuOpen(true);
        }
    }
}
