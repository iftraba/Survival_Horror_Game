using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Volumen de la sala del jefe, justo pasada la puerta. Al entrar el jugador, el jefe deja de bailar y empieza el combate.
    /// (Abrir la puerta no basta: puedes asomarte, mirar y volver. Una vez dentro, no.)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BossRoomTrigger : MonoBehaviour
    {
        public ZombieAI boss;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (boss == null || !boss.IsDormant) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            boss.Wake();
        }
    }
}
