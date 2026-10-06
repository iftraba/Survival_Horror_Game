using System.Collections;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Taquilla que se abre con E. Va en la raiz de la taquilla; la puerta gira alrededor de su bisagra.
    /// Lo que hay dentro no se puede coger mientras esta cerrada (el interactor exige linea de vision).
    /// </summary>
    public class LockerDoor : MonoBehaviour, IInteractable
    {
        public Transform hinge;
        public float openAngle = -110f;
        public float openTime = 0.45f;

        [Tooltip("Si no esta vacio, la taquilla esta bloqueada y se abre tecleando este codigo")] public string code;

        [SerializeField] bool open;
        bool moving;
        bool codeSolved;

        /// <summary>Taquilla que aun pide codigo.</summary>
        public bool Locked => !string.IsNullOrEmpty(code) && !codeSolved && !open;

        public bool IsOpen => open;
        public string Prompt => moving || open ? "" : (Locked ? "E  Introducir código" : "E  Abrir taquilla");

        public void Interact(GameObject who)
        {
            if (open || moving || hinge == null) return;
            if (Locked)
            {
                GameAudio.Play(Sfx.DoorLocked, transform.position, 0.6f, 1.2f);
                Keypad.Open(this);
                return;
            }
            GameAudio.Play(Sfx.DoorOpen, transform.position, 0.6f, 1.45f);
            StartCoroutine(Swing());
        }

        /// <summary>Codigo correcto: se desbloquea y se abre.</summary>
        public void Unlock()
        {
            codeSolved = true;
            GameAudio.Play(Sfx.DoorUnlock, transform.position, 0.8f, 1f);
            Interact(null);
        }

        /// <summary>Estado guardado: abierta sin animacion.</summary>
        public void SetOpen(bool value)
        {
            open = value;
            if (value) codeSolved = true;
            if (hinge == null) return;
            hinge.localRotation = Quaternion.Euler(0f, value ? openAngle : 0f, 0f);
            foreach (var c in hinge.GetComponentsInChildren<Collider>(true)) c.enabled = !value;
        }

        IEnumerator Swing()
        {
            moving = true;
            var cols = hinge.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;           // que la puerta no empuje al jugador al girar
            var from = hinge.localRotation;
            var to = Quaternion.Euler(0f, openAngle, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / openTime)
            {
                hinge.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            hinge.localRotation = to;
            // abierta, la puerta no colisiona: asi nunca te deja encajado entre dos taquillas abiertas
            open = true;
            moving = false;
        }
    }
}
