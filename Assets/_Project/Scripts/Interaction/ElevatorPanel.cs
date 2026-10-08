using System.Collections;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Botonera del ascensor de carga (comisaria v2). Hasta ponerlo en marcha con la llave (marca de progreso 'flag') no
    /// funciona; despues, al usarla el jugador "viaja": funde a negro, suena la maquinaria y aparece en el punto de llegada
    /// de la otra planta. Cada botonera lleva a un destino.
    /// </summary>
    public class ElevatorPanel : MonoBehaviour, IInteractable
    {
        public ItemData requiredKey;
        public string flag = "ascensor";
        [Tooltip("Donde aparece el jugador (posicion y hacia donde mira)")] public Transform arrival;
        public string destinationName = "sotano";
        [TextArea] public string lockedMessage = "El ascensor de carga no tiene corriente. La botonera tiene una cerradura.";
        [TextArea] public string unlockedObjective = "";

        static bool riding;
        static float fade;
        static ElevatorPanel drawer;

        public string Prompt => riding ? "" : Progress.Has(flag) ? "E  Ascensor: bajar/subir al " + destinationName : "E  Usar ascensor";

        public void Interact(GameObject who)
        {
            if (riding) return;
            if (!Progress.Has(flag))
            {
                var inv = who.GetComponent<Inventory>();
                if (requiredKey != null && inv != null && inv.Has(requiredKey))
                {
                    inv.Remove(requiredKey);
                    Progress.Set(flag);
                    GameAudio.Play(Sfx.DoorUnlock, transform.position);
                    GameAudio.Play(Sfx.Switch, transform.position);
                    Hud.Message("Giras la llave y la botonera se enciende: el ascensor funciona.");
                    if (!string.IsNullOrEmpty(unlockedObjective)) Objectives.Set(unlockedObjective);
                    return;
                }
                GameAudio.Play(Sfx.DoorLocked, transform.position);
                Hud.Message(requiredKey != null ? lockedMessage + " Necesitas: " + requiredKey.displayName : lockedMessage);
                return;
            }
            if (arrival != null) StartCoroutine(Ride(who));
        }

        IEnumerator Ride(GameObject who)
        {
            riding = true; drawer = this;
            var pc = who.GetComponent<PlayerController>();
            var cc = who.GetComponent<CharacterController>();
            if (pc != null) pc.enabled = false;
            GameAudio.Play(Sfx.DoorClose, transform.position);
            for (float f = 0f; f < 1f; f += Time.deltaTime * 2.5f) { fade = f; yield return null; }
            fade = 1f;
            GameAudio.Play(Sfx.BossStep, transform.position, 0.5f, 0.5f, false);       // la maquinaria
            yield return new WaitForSeconds(1.6f);
            if (cc != null) cc.enabled = false;
            who.transform.SetPositionAndRotation(arrival.position, Quaternion.Euler(0f, arrival.eulerAngles.y, 0f));
            if (cc != null) cc.enabled = true;
            var cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.SetYaw(arrival.eulerAngles.y);
            GameAudio.Play(Sfx.DoorOpen, arrival.position);
            yield return new WaitForSeconds(0.3f);
            for (float f = 1f; f > 0f; f -= Time.deltaTime * 2f) { fade = f; yield return null; }
            fade = 0f;
            if (pc != null) pc.enabled = true;
            riding = false;
        }

        void OnGUI()
        {
            if (drawer != this || fade <= 0.001f) return;
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        void OnDisable() { if (drawer == this) { riding = false; fade = 0f; } }
    }
}
