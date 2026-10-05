using UnityEngine;

namespace Horror
{
    /// <summary>Puerta de salida del edificio: con la llave correcta, termina la partida con victoria.</summary>
    public class ExitDoor : MonoBehaviour, IInteractable
    {
        public ItemData requiredKey;

        public string Prompt => "E  Abrir puerta de salida";

        public void Interact(GameObject who)
        {
            var inv = who.GetComponent<Inventory>();
            if (requiredKey != null && (inv == null || !inv.Has(requiredKey)))
            {
                Hud.Message($"Cerrada. Necesitas: {requiredKey.displayName}");
                GameAudio.Play(Sfx.DoorLocked, transform.position);
                return;
            }
            if (requiredKey != null) inv.Remove(requiredKey);
            GameAudio.Play(Sfx.DoorUnlock, transform.position);
            GameAudio.Play(Sfx.DoorOpen, transform.position);
            Objectives.Set("", false);
            GameState.SetVictory(true);
        }
    }
}
