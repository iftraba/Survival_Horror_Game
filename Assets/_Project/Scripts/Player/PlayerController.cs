using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public ThirdPersonCamera cam;
        public float walkSpeed = 2.4f;
        public float runSpeed = 4.4f;
        public float aimSpeed = 1.3f;
        public float turnSpeed = 12f;
        public float gravity = -20f;

        public bool IsAiming { get; private set; }
        public bool IsRunning { get; private set; }

        CharacterController controller;
        Health health;
        float verticalVelocity;
        [Tooltip("Duracion (s) del giro rapido de 180 grados (tecla Q)")] public float quickTurnTime = 0.22f;
        float turnRemaining;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        // Start y no Awake: asi funciona aunque Health se anada despues de este componente
        void Start()
        {
            health = GetComponent<Health>();
            if (health != null) health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        void OnDied()
        {
            IsAiming = false;
            GameState.SetPlayerDead(true);
        }

        void Update()
        {
            var kb = Keyboard.current;
            var ms = Mouse.current;
            if (kb == null || ms == null) return;

            // Muerto = sin control, tanto por el estado global como por la propia salud
            bool blocked = GameState.InputBlocked || (health != null && health.IsDead);
            Vector2 input = Vector2.zero;
            IsAiming = false;
            IsRunning = false;

            if (!blocked)
            {
                input.x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                input.y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                IsAiming = ms.rightButton.isPressed;
                // Se puede correr en cualquier direccion (atras y de lado incluidos), no solo hacia delante
                IsRunning = kb.leftShiftKey.isPressed && !IsAiming && input.sqrMagnitude > 0.01f;
            }

            // Giro de 180 grados (Q): gira el personaje y la camara a la vez
            if (!blocked && kb.qKey.wasPressedThisFrame && turnRemaining <= 0f) turnRemaining = 180f;
            if (turnRemaining > 0f)
            {
                float step = Mathf.Min(turnRemaining, 180f / Mathf.Max(0.05f, quickTurnTime) * Time.deltaTime);
                transform.Rotate(0f, step, 0f, Space.World);
                if (cam != null) cam.AddYaw(step);
                turnRemaining -= step;
            }

            var camT = cam != null ? cam.transform : Camera.main.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(camT.right, Vector3.up).normalized;
            Vector3 move = fwd * input.y + right * input.x;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = IsAiming ? aimSpeed : IsRunning ? runSpeed : walkSpeed;

            if (turnRemaining > 0f) { }                 // durante el giro rapido no se reorienta
            else if (IsAiming)
                Face(fwd);
            else if (move.sqrMagnitude > 0.01f)
                Face(move);

            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        [Tooltip("Fuerza con la que el jugador empuja cuerpos rigidos al caminar contra ellos")]
        public float pushPower = 2.2f;

        // El CharacterController no mueve rigidbodies por si solo: se les da un empujon al chocar
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic || hit.moveDirection.y < -0.3f) return;
            var dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            float speed = Mathf.Max(controller.velocity.magnitude, 0.5f);
            rb.AddForceAtPosition(dir.normalized * (pushPower * speed), hit.point, ForceMode.Impulse);
        }

        void Face(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            var target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
