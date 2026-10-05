using UnityEngine;

namespace Horror
{
    /// <summary>Baul de objetos de una sala segura. Abre la pantalla de intercambio con el almacen global (ItemStorage).</summary>
    public class ItemBox : MonoBehaviour, IInteractable
    {
        [Tooltip("Tapa que se levanta mientras el baul esta abierto")] public Transform lid;
        public float lidOpenAngle = -75f;

        public string Prompt => "E  Abrir baul de objetos";

        void OnEnable() => GameState.Changed += SyncLid;
        void OnDisable() => GameState.Changed -= SyncLid;

        public void Interact(GameObject who)
        {
            GameAudio.Play(Sfx.DoorOpen, transform.position, 0.5f, 0.75f);
            GameState.SetBoxOpen(true);
            SyncLid();
        }

        void SyncLid()
        {
            if (lid != null) lid.localRotation = Quaternion.Euler(GameState.BoxOpen ? lidOpenAngle : 0f, 0f, 0f);
        }
    }
}
