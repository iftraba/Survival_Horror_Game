using UnityEngine;
using UnityEngine.AI;

namespace Horror
{
    /// <summary>
    /// Reja que corta un paso hasta que se cumple una marca de progreso (comisaria v2: la escalera de servicio del archivo,
    /// que abre el monumento del memorial). Cerrada bloquea al jugador y a los zombis (NavMeshObstacle que recorta); al abrirse
    /// sube despacio y deja pasar. Al cargar una partida con la marca ya puesta aparece abierta.
    /// </summary>
    public class ServiceGate : MonoBehaviour, IInteractable
    {
        public string flag = "memorial";
        [Tooltip("Barrotes que suben al abrir")] public Transform bars;
        public float liftHeight = 2.5f;
        public float liftTime = 2.2f;
        [TextArea] public string lockedMessage = "Una reja de seguridad cierra la escalera. Tiene una placa: \"Acceso al archivo. Se abre desde el memorial.\"";

        Vector3 closedPos;
        float t = -1f;          // -1 cerrada; 0..1 subiendo; 1 abierta
        Collider[] cols;
        NavMeshObstacle obstacle;

        public bool IsOpen => t >= 1f;
        public string Prompt => t < 0f ? "E  Examinar reja" : "";

        void Awake()
        {
            if (bars == null) bars = transform;
            closedPos = bars.localPosition;
            cols = GetComponentsInChildren<Collider>();
            obstacle = GetComponentInChildren<NavMeshObstacle>();
        }

        void OnEnable() { Progress.Changed += Check; }
        void OnDisable() => Progress.Changed -= Check;
        void Start() { if (Progress.Has(flag)) SetOpen(true); }

        void Check()
        {
            if (!Progress.Has(flag)) { if (t >= 0f) SetOpen(false); return; }
            if (t < 0f) { t = 0f; GameAudio.Play(Sfx.DoorOpen, transform.position, 1f, 0.6f); }
        }

        void SetOpen(bool open)
        {
            t = open ? 1f : -1f;
            bars.localPosition = closedPos + (open ? Vector3.up * liftHeight : Vector3.zero);
            foreach (var c in cols) if (c != null && !(c.isTrigger)) c.enabled = !open;
            if (obstacle != null) obstacle.enabled = !open;
        }

        void Update()
        {
            if (t < 0f || t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / liftTime);
            bars.localPosition = closedPos + Vector3.up * liftHeight * Mathf.SmoothStep(0f, 1f, t);
            if (t >= 1f) SetOpen(true);
        }

        public void Interact(GameObject who)
        {
            if (t >= 0f) return;
            GameAudio.Play(Sfx.DoorLocked, transform.position);
            Hud.Message(lockedMessage);
        }
    }
}
