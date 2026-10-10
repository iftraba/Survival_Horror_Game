using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>
    /// Secuencia de "revelado" (2026-10-10): al cumplirse una marca de progreso (el tercer medallon, el tercer fusible) la camara deja al jugador
    /// un momento viendo lo que ha colocado, recorre rapido el camino hasta lo que se abre (la reja del archivo, la puerta de las calderas), se queda
    /// viendolo abrirse en directo y vuelve. El jugador no se mueve ni recibe dano durante la secuencia; se puede saltar con E, Espacio o Esc.
    /// Al cargar una partida con la marca ya puesta no se reproduce. El efecto (<see cref="ServiceGate"/>, <see cref="ProgressSeal"/>) espera a que la camara
    /// llegue: lo consulta con <see cref="DelayFor"/>.
    /// </summary>
    public class ProgressCutscene : MonoBehaviour
    {
        [Tooltip("Marca de progreso que lo dispara")] public string flag = "memorial";
        [Tooltip("Recorrido de la camara en coordenadas de mundo (del sitio donde se coloca la pieza hasta delante de lo que se abre); el ultimo punto es donde se queda")]
        public Vector3[] path;
        [Tooltip("Lo que se mira al llegar (la reja, la puerta)")] public Transform focus;
        public Vector3 focusOffset = new Vector3(0f, 1.2f, 0f);
        public float travelSpeed = 14f;
        [Tooltip("Segundos viendo la pieza recien colocada antes de partir")] public float startHold = 0.9f;
        [Tooltip("Segundos que se queda mirando como se abre")] public float openHold = 5.5f;
        public float returnSpeedFactor = 1.8f;

        static readonly Dictionary<string, ProgressCutscene> registry = new Dictionary<string, ProgressCutscene>();
        bool played, running, skip, arrived;
        float startedAt;
        float loadedAt;

        void OnEnable() { registry[flag] = this; Progress.Changed += Check; loadedAt = Time.time; }
        void OnDisable() { if (registry.TryGetValue(flag, out var c) && c == this) registry.Remove(flag); Progress.Changed -= Check; }
        // No se decide nada en Start: en el editor (sin recarga de dominio) Progress conserva las marcas de la partida anterior hasta que GameFlow las limpia.

        /// <summary>Segundos que tarda la camara en llegar (lo que debe esperar el efecto para abrirse a la vista). 0 si no hay secuencia.</summary>
        public static float DelayFor(string flag)
        {
            if (!registry.TryGetValue(flag, out var c) || c == null) return 0f;
            return c.Begin() ? c.startHold + c.PathLength() / Mathf.Max(1f, c.travelSpeed) + 0.4f : 0f;
        }

        void Check() => Begin();

        /// <summary>True mientras la camara todavia viaja hacia lo que se abre (el efecto debe esperar a que llegue para que se vea).</summary>
        public static bool Travelling(string flag) => registry.TryGetValue(flag, out var c) && c != null && c.running && !c.arrived;

        /// <summary>Empieza la secuencia si toca (una sola vez). Devuelve true si se esta reproduciendo.</summary>
        bool Begin()
        {
            if (running) return true;
            if (!Progress.Has(flag)) { played = false; return false; }                   // partida nueva: la secuencia vuelve a estar disponible
            if (played || path == null || path.Length == 0) return false;
            if (Progress.Restoring || Time.time - loadedAt < 2f) { played = true; return false; }   // la marca llega al cargar una partida o empezar una
            var pc = FindFirstObjectByType<PlayerController>(); var cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (pc == null || cam == null) { played = true; return false; }
            played = true; running = true; skip = false; arrived = false; startedAt = Time.unscaledTime;
            StartCoroutine(Run(pc, cam));
            return true;
        }

        float PathLength()
        {
            float L = 0f; if (path == null) return 0f;
            for (int i = 1; i < path.Length; i++) L += Vector3.Distance(path[i - 1], path[i]);
            return L + 8f;      // del punto de partida de la camara al primer punto
        }

        IEnumerator Run(PlayerController pc, ThirdPersonCamera cam)
        {
            var hp = pc.GetComponent<Health>(); float oldMult = hp != null ? hp.damageTakenMultiplier : 1f;
            if (hp != null) hp.damageTakenMultiplier = 0f;
            pc.enabled = false;
            Hud.Message("E: saltar");
            var c = cam.GetComponent<Camera>();
            Vector3 startPos = cam.transform.position; Quaternion startRot = cam.transform.rotation; float fov = c.fieldOfView;
            cam.SetCinematic(startPos, startRot, fov);

            // 1) ver la pieza recien colocada
            yield return Wait(startHold);

            // 2) recorrido hasta lo que se abre
            var pts = new List<Vector3> { startPos };
            pts.AddRange(path);
            Vector3 look = focus != null ? focus.position + focusOffset : pts[pts.Count - 1] + Vector3.forward;
            yield return Fly(cam, pts, travelSpeed, fov, look, true);
            // 3) mirar como se abre
            arrived = true;
            if (!skip) { float t = 0f; var endPos = pts[pts.Count - 1]; while (t < openHold && !skip) { t += Time.deltaTime; cam.SetCinematic(endPos, Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(look - endPos), 5f * Time.deltaTime), fov); PollSkip(); yield return null; } }
            // 4) volver
            pts.Reverse();
            if (!skip) yield return Fly(cam, pts, travelSpeed * returnSpeedFactor, fov, Vector3.zero, false);

            arrived = true;
            cam.EndCinematic();
            pc.enabled = true;
            if (hp != null) hp.damageTakenMultiplier = oldMult;
            running = false;
        }

        IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !skip; t += Time.deltaTime) { PollSkip(); yield return null; }
        }

        void PollSkip()
        {
            if (Time.unscaledTime - startedAt < 0.7f) return;          // la E que coloca el ultimo medallon es la misma que saltaria la secuencia en el mismo fotograma
            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) skip = true;
        }

        /// <summary>Mueve la camara por la polilinea a velocidad constante mirando un poco por delante; al final del viaje de ida, gira a mirar 'finalLook'.</summary>
        IEnumerator Fly(ThirdPersonCamera cam, List<Vector3> pts, float speed, float fov, Vector3 finalLook, bool lookAtEnd)
        {
            float total = 0f; var seg = new List<float>();
            for (int i = 1; i < pts.Count; i++) { float d = Vector3.Distance(pts[i - 1], pts[i]); seg.Add(d); total += d; }
            float dist = 0f; Quaternion rot = cam.transform.rotation;
            while (dist < total && !skip)
            {
                PollSkip();
                dist = Mathf.Min(total, dist + speed * Time.deltaTime);
                Vector3 pos = Along(pts, seg, dist), ahead = Along(pts, seg, Mathf.Min(total, dist + 3.5f));
                Vector3 dir = ahead - pos;
                if (lookAtEnd && total - dist < 6f) dir = Vector3.Lerp(dir.sqrMagnitude > 0.01f ? dir : (finalLook - pos), finalLook - pos, 1f - (total - dist) / 6f);
                if (dir.sqrMagnitude > 0.01f) rot = Quaternion.Slerp(rot, Quaternion.LookRotation(dir.normalized), 6f * Time.deltaTime);
                cam.SetCinematic(pos, rot, fov);
                yield return null;
            }
        }

        static Vector3 Along(List<Vector3> pts, List<float> seg, float dist)
        {
            for (int i = 0; i < seg.Count; i++)
            {
                if (dist <= seg[i] || i == seg.Count - 1) return Vector3.Lerp(pts[i], pts[i + 1], seg[i] > 0.0001f ? Mathf.Clamp01(dist / seg[i]) : 1f);
                dist -= seg[i];
            }
            return pts[pts.Count - 1];
        }
    }
}
