using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Paso de un solo sentido: una puerta atrancada por uno de sus lados (barricada, trasto contra la hoja) que solo se puede abrir
    /// desde el otro. Mientras el jugador esta en el lado atrancado la puerta esta sellada (no la abre ni el ni los zombis); en
    /// cuanto esta en el lado libre se desbloquea, y cuando se abre desde ahi queda abierta para siempre (tambien al cargar partida).
    /// Va en el mismo objeto que la <see cref="Door"/>.
    /// </summary>
    [RequireComponent(typeof(Door))]
    public class OneWayDoor : MonoBehaviour
    {
        [Tooltip("Un punto del lado LIBRE (desde el que se abre la puerta), en coordenadas de mundo")]
        public Vector3 freePoint;
        [Tooltip("Texto al intentar abrirla desde el lado atrancado")]
        public string blockedMessage = "Esta atrancada por el otro lado. No se abre desde aqui.";
        public string blockedPrompt = "Atrancada";

        Door door;
        Transform player;
        float freeSign = 1f, nextCheck;
        bool released;

        void Awake() => door = GetComponent<Door>();

        void Start()
        {
            door.sealedPrompt = blockedPrompt; door.sealedMessage = blockedMessage;
            freeSign = Mathf.Sign(Vector3.Dot(freePoint - door.Center, door.Normal));
            if (freeSign == 0f) freeSign = 1f;
        }

        void Update()
        {
            if (released) return;
            if (door.IsOpen) { released = true; door.Unseal(); return; }          // abierta desde el lado libre (o partida cargada con ella abierta)
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.2f;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return; player = p.transform; }
            bool onFreeSide = Mathf.Sign(Vector3.Dot(player.position - door.Center, door.Normal)) == freeSign;
            if (onFreeSide) door.Unseal(); else door.Seal();
        }
    }
}
