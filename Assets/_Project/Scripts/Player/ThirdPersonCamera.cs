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

        // Camara cinematica (ProgressCutscene): sustituye a la camara del jugador mientras dura y vuelve con un fundido de 0,5 s.
        bool cinematic; Vector3 cinePos; Quaternion cineRot; float cineFov, blendT = 1f;
        public bool Cinematic => cinematic;
        public void SetCinematic(Vector3 pos, Quaternion rot, float fieldOfView) { cinematic = true; cinePos = pos; cineRot = rot; cineFov = fieldOfView; blendT = 1f; }
        public void EndCinematic() { if (!cinematic) return; cinematic = false; blendT = 0f; }

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
            if (cinematic) { transform.SetPositionAndRotation(cinePos, cineRot); cam.fieldOfView = cineFov; return; }

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

            var finalPos = pivot + dir / dist * closest;
            if (blendT < 1f)                                                          // vuelta de una secuencia: de donde estaba la camara cinematica a la del jugador
            {
                blendT = Mathf.Min(1f, blendT + Time.unscaledDeltaTime / 0.5f);
                float k = Mathf.SmoothStep(0f, 1f, blendT);
                finalPos = Vector3.Lerp(cinePos, finalPos, k); rot = Quaternion.Slerp(cineRot, rot, k);
            }
            transform.SetPositionAndRotation(finalPos, rot);
        }
    }
}
