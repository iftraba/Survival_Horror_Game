using UnityEngine;

namespace Horror
{
    /// <summary>Pasos segun la distancia recorrida: mas rapidos y fuertes al correr, sigilosos al apuntar.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        public float walkStride = 0.85f;
        public float runStride = 1.15f;

        CharacterController body;
        PlayerController player;
        Health health;
        float travelled;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            player = GetComponent<PlayerController>();
            health = GetComponent<Health>();
        }

        void Update()
        {
            if (GameState.InputBlocked || (health != null && health.IsDead) || !body.isGrounded) return;

            var v = body.velocity;
            v.y = 0f;
            float speed = v.magnitude;
            if (speed < 0.3f) return;

            travelled += speed * Time.deltaTime;
            float stride = player != null && player.IsRunning ? runStride : walkStride;
            if (travelled < stride) return;
            travelled -= stride;

            float volume = player != null && player.IsRunning ? 0.9f : player != null && player.IsAiming ? 0.35f : 0.55f;
            // en la escalera suenan peldaños (macizos y mas graves); el suelo lo decide el collider que hay bajo los pies
            bool stairs = Physics.Raycast(transform.position, Vector3.down, out var hit, body.height * 0.5f + 0.4f, ~0, QueryTriggerInteraction.Ignore)
                          && (hit.collider.name.StartsWith("Step_") || hit.collider.name == "Stair_NavRamp");
            GameAudio.Play(stairs ? Sfx.StairStep : Sfx.Footstep, transform.position, volume, Random.Range(0.9f, 1.1f), false);
        }
    }
}
