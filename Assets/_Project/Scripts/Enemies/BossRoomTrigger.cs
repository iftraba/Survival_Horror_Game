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
        [Tooltip("Puertas que se atrancan al empezar el combate (no se puede huir) y se abren al morir el jefe")] public Door[] sealDoors;
        bool subscribed;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (boss == null || !boss.IsDormant) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            boss.Wake();
            if (sealDoors != null && sealDoors.Length > 0)
            {
                foreach (var d in sealDoors) if (d != null) d.Seal();
                Hud.Message("La puerta se ha atrancado tras de ti");
                if (!subscribed && boss.Hp != null) { subscribed = true; boss.Hp.Died += OnBossDied; }
            }
        }

        void OnBossDied()
        {
            if (sealDoors != null) foreach (var d in sealDoors) if (d != null) d.Unseal();
        }
    }
}
