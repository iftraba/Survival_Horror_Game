using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Punto de guardado de una sala segura: un telefono antiguo de disco. Abre el menu de guardado (el HUD lo dibuja y confirma).
    /// Solo hay telefonos en las salas seguras, asi que solo se puede guardar alli.
    /// Solo suena al usarlo (descolgar al abrir el menu y marcar al guardar). El timbre por el mapa queda apagado
    /// (ringsNearby = false); si se activa, suena de vez en cuando cuando el jugador esta cerca.
    /// </summary>
    public class SaveTerminal : MonoBehaviour, IInteractable
    {
        [Tooltip("Distancia a la que el telefono suena")] public float ringRange = 14f;
        public Vector2 ringInterval = new Vector2(30f, 55f);
        [Tooltip("Si esta activo, el telefono suena solo por el mapa como señal. Apagado (por defecto): solo suena al usarlo, en el menu de guardado")]
        public bool ringsNearby = false;
        [Range(0f, 1f)] public float ringVolume = 1f;
        [Tooltip("Alcance del sonido (m): es una señal para encontrar el telefono, se oye mas lejos que un efecto normal")] public float hearRange = 30f;

        float nextRing;
        Transform player;

        public string Prompt => "E  Usar el telefono (guardar partida)";

        void Start() => nextRing = Time.time + Random.Range(4f, 12f);

        void Update()
        {
            if (!ringsNearby || Time.time < nextRing || GameState.InputBlocked) return;
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc == null) return;
                player = pc.transform;
            }
            nextRing = Time.time + Random.Range(ringInterval.x, ringInterval.y);
            if ((player.position - transform.position).sqrMagnitude < ringRange * ringRange)
                GameAudio.Play(Sfx.PhoneRing, transform.position, ringVolume, 1f, true, hearRange);
        }

        public void Interact(GameObject who)
        {
            GameAudio.Play(Sfx.PhonePickup, transform.position, 0.9f, 1f, false);
            GameState.SetSaveMenuOpen(true);
        }
    }
}
