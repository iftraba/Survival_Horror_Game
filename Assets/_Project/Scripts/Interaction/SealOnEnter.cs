using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Volumen al pasar la puerta principal de la comisaria (v2): cuando el jugador entra, las puertas se cierran y se atrancan
    /// detras de el (ya no se puede volver al patio con los zombis de la verja).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SealOnEnter : MonoBehaviour
    {
        public Door[] doors;
        [TextArea] public string message = "La puerta se ha atrancado tras de ti";
        [TextArea] public string objective = "Busca la forma de llegar al archivo de la segunda planta.";
        bool done;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (done || other.GetComponentInParent<PlayerController>() == null) return;
            done = true;
            if (doors != null) foreach (var d in doors) if (d != null) d.Seal();
            if (!string.IsNullOrEmpty(message)) Hud.Message(message);
            if (!string.IsNullOrEmpty(objective)) Objectives.Set(objective);
        }
    }
}
