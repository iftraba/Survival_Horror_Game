using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>
    /// Escena de camara del principio (comisaria v2, 2026-10-08): la camara gira alrededor de la comisaria mostrando la ciudad en
    /// caos (edificios, el coche patrulla ardiendo, los zombis tras la verja), aparece el titulo y funde a negro; al volver, el
    /// jugador esta en el vestibulo con la puerta principal atrancada detras. Se salta con Espacio, E, Esc o clic.
    /// Mientras dura, el jugador no se mueve y el HUD no se dibuja (IntroCutscene.Active).
    /// </summary>
    public class IntroCutscene : MonoBehaviour
    {
        public static bool Active { get; private set; }

        [Tooltip("Punto al que mira la camara (la entrada de la comisaria)")] public Vector3 focus = new Vector3(0f, 4f, 6f);
        public float radius = 40f, height = 25f, duration = 14f;
        [Tooltip("Angulo inicial y final del giro (grados, 0 = mirando al norte desde el sur)")] public float fromAngle = -70f, toAngle = 35f;
        [Tooltip("Al final la camara baja hasta la puerta principal")] public Vector3 endPoint = new Vector3(0f, 2.2f, -7f);
        public string title = "SECTOR 7: GRIMHEIM";
        public string subtitle = "Comisaría de policía del distrito 7";
        [Tooltip("Puertas que quedan atrancadas al terminar (la principal: ya no se puede salir)")] public Door[] sealOnEnd;

        Camera cam;
        ThirdPersonCamera follow;
        float fade = 1f, titleAlpha;
        bool skip;
        GUIStyle big, small;

        void Start()
        {
            cam = Camera.main;
            follow = cam != null ? cam.GetComponent<ThirdPersonCamera>() : null;
            StartCoroutine(Run());
        }

        void Update()
        {
            if (!Active) return;
            var kb = Keyboard.current; var ms = Mouse.current;
            if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) || (ms != null && ms.leftButton.wasPressedThisFrame))
                skip = true;
        }

        IEnumerator Run()
        {
            Active = true;
            if (follow != null) follow.enabled = false;
            float t = 0f;
            while (t < duration && !skip)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                float a = Mathf.Lerp(fromAngle, toAngle, k) * Mathf.Deg2Rad;
                var orbit = focus + new Vector3(Mathf.Sin(a) * -radius, height, Mathf.Cos(a) * -radius);
                // el ultimo tercio baja hacia la puerta principal
                float down = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 1f, t / duration));
                var pos = Vector3.Lerp(orbit, endPoint, down);
                var look = Vector3.Lerp(focus, new Vector3(endPoint.x, 1.6f, focus.z), down);
                if (cam != null) cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
                fade = Mathf.Max(0f, 1f - t / 1.5f) + Mathf.Clamp01((t - (duration - 1.2f)) / 1.2f);
                titleAlpha = Mathf.Clamp01((t - 1.5f) / 1.2f) * Mathf.Clamp01((duration * 0.6f - t) / 1.2f);
                yield return null;
            }
            // negro, el jugador ya dentro y la puerta atrancada
            titleAlpha = 0f;
            for (float f = fade; f < 1f; f += Time.deltaTime * 3f) { fade = f; yield return null; }
            fade = 1f;
            if (sealOnEnd != null) foreach (var d in sealOnEnd) if (d != null) d.Seal();
            if (follow != null)
            {
                follow.enabled = true;
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) follow.SetYaw(pc.transform.eulerAngles.y);
            }
            Active = false;
            for (float f = 1f; f > 0f; f -= Time.deltaTime * 1.5f) { fade = f; yield return null; }
            fade = 0f;
            enabled = false;
        }

        void OnGUI()
        {
            if (fade <= 0.001f && titleAlpha <= 0.001f) return;
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 54, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                small = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            }
            var old = GUI.color;
            if (fade > 0.001f) { GUI.color = new Color(0, 0, 0, fade); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); }
            if (titleAlpha > 0.001f)
            {
                big.normal.textColor = new Color(0.85f, 0.1f, 0.08f, titleAlpha);
                small.normal.textColor = new Color(0.9f, 0.9f, 0.9f, titleAlpha);
                GUI.color = Color.white;
                GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 80), title, big);
                GUI.Label(new Rect(0, Screen.height * 0.38f + 70, Screen.width, 40), subtitle, small);
                if (Active) { small.normal.textColor = new Color(0.7f, 0.7f, 0.7f, titleAlpha * 0.7f); GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 30), "Espacio para saltar", small); }
            }
            GUI.color = old;
        }

        void OnDestroy() { if (Active) Active = false; }
    }
}
