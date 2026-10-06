using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Punto de guardado de una sala segura: un telefono antiguo de disco. Abre el menu de guardado (el HUD lo dibuja y confirma).
    /// Solo hay telefonos en las salas seguras, asi que solo se puede guardar alli.
    /// Si el jugador esta cerca, el telefono suena de vez en cuando: es la señal de que ahi se puede guardar.
    /// </summary>
    public class SaveTerminal : MonoBehaviour, IInteractable
    {
        [Tooltip("Distancia a la que el telefono suena")] public float ringRange = 10f;
        public Vector2 ringInterval = new Vector2(30f, 55f);
        [Range(0f, 1f)] public float ringVolume = 0.55f;

        float nextRing;
        Transform player;

        public string Prompt => "E  Usar el telefono (guardar partida)";

        void Start() => nextRing = Time.time + Random.Range(4f, 12f);

        void Update()
        {
            if (Time.time < nextRing || GameState.InputBlocked) return;
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc == null) return;
                player = pc.transform;
            }
            nextRing = Time.time + Random.Range(ringInterval.x, ringInterval.y);
            if ((player.position - transform.position).sqrMagnitude < ringRange * ringRange)
                GameAudio.Play(Sfx.PhoneRing, transform.position, ringVolume, 1f, true);
        }

        public void Interact(GameObject who)
        {
            GameAudio.Play(Sfx.PhonePickup, transform.position, 0.9f, 1f, false);
            GameState.SetSaveMenuOpen(true);
        }
    }
}
