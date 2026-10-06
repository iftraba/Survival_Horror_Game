using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Punto de guardado de una sala segura: un telefono antiguo de disco. Abre el menu de guardado (el HUD lo dibuja y confirma).
    /// Solo hay telefonos en las salas seguras, asi que solo se puede guardar alli.
    /// </summary>
    public class SaveTerminal : MonoBehaviour, IInteractable
    {
        public string Prompt => "E  Usar el telefono (guardar partida)";

        public void Interact(GameObject who)
        {
            GameAudio.Play(Sfx.Switch, transform.position, 0.9f, 0.8f, false);
            GameState.SetSaveMenuOpen(true);
        }
    }
}
