using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>Camara al hombro estilo RE2 Remake. Se acerca y se desplaza al apuntar.</summary>
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public PlayerController player;
        public float sensitivity = 0.12f;
        public float pivotHeight = 0.7f;

        [Header("Normal")]
        public float distance = 3.2f;
        public float shoulder = 0.45f;
        public float fov = 62f;

        [Header("Apuntando")]
        public float aimDistance = 1.2f;
        public float aimShoulder = 0.6f;
        public float aimFov = 44f;

        public float minPitch = -40f;
        public float maxPitch = 60f;

        float yaw, pitch = 12f;
        float crouchDrop;
        float curDistance, curShoulder, curFov;
        Camera cam;

        public float Yaw => yaw;
        /// <summary>Gira la camara (el giro rapido del jugador la mueve junto al personaje).</summary>
        public void AddYaw(float degrees) => yaw += degrees;
        public void SetYaw(float newYaw, float newPitch = 12f)
        {
            yaw = newYaw;
            pitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            curDistance = distance;
            curShoulder = shoulder;
            curFov = fov;
        }

        void Start()
        {
            if (target != null) yaw = target.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (!GameState.InputBlocked && Mouse.current != null)
            {
                Vector2 d = Mouse.current.delta.ReadValue() * sensitivity * GameSettings.Sensitivity;
                yaw += d.x;
                pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
            }

            bool aiming = player != null && player.IsAiming;
            float t = 10f * Time.unscaledDeltaTime;
            curDistance = Mathf.Lerp(curDistance, aiming ? aimDistance : distance, t);
            curShoulder = Mathf.Lerp(curShoulder, aiming ? aimShoulder : shoulder, t);
            curFov = Mathf.Lerp(curFov, aiming ? aimFov : fov, t);
            cam.fieldOfView = curFov;

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            // agachado: el punto de mira de la camara baja con el personaje
            crouchDrop = Mathf.Lerp(crouchDrop, player != null && player.IsCrouching ? 0.45f : 0f, 8f * Time.unscaledDeltaTime);
            Vector3 pivot = target.position + Vector3.up * (pivotHeight - crouchDrop);
            Vector3 desired = pivot + rot * new Vector3(curShoulder, 0f, -curDistance);

            // Evita atravesar paredes
            Vector3 dir = desired - pivot;
            float dist = dir.magnitude;
            var hits = Physics.SphereCastAll(pivot, 0.2f, dir / dist, dist, ~0, QueryTriggerInteraction.Ignore);
            float closest = dist;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(target) || h.distance <= 0f) continue;
                if (h.collider.GetComponentInParent<Pickup>() != null) continue;   // objetos del suelo no empujan la camara
                closest = Mathf.Min(closest, h.distance);
            }

            transform.SetPositionAndRotation(pivot + dir / dist * closest, rot);
        }
    }
}
