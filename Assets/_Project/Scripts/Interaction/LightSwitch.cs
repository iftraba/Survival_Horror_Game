using UnityEngine;

namespace Horror
{
    /// <summary>Interruptor de pared ("llave de luz"): enciende y apaga un grupo de lamparas.</summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        public CeilingLamp[] lamps;
        [Tooltip("Palanca que se inclina al accionarlo")] public Transform lever;
        public float leverAngle = 16f;

        [Tooltip("Estado al empezar la partida (las salas apagadas obligan a buscar el interruptor)")]
        public bool startOn = true;

        bool on = true;

        public string Prompt => on ? "E  Apagar luces" : "E  Encender luces";
        public bool IsOn => on;

        /// <summary>Fija el estado sin sonido (al cargar una partida).</summary>
        public void SetOn(bool value)
        {
            on = value;
            restored = true;
            foreach (var l in lamps) if (l != null) l.SetPowered(on);
            ApplyLever();
        }

        void Awake() => on = startOn;

        bool restored;

        void Start()
        {
            if (restored) return;   // una partida cargada ya fijo el estado (SetOn) antes que este Start
            foreach (var l in lamps) if (l != null) l.SetPowered(on);
            ApplyLever();
        }

        public void Interact(GameObject who)
        {
            on = !on;
            foreach (var l in lamps) if (l != null) l.SetPowered(on);
            ApplyLever();
            GameAudio.Play(Sfx.Switch, transform.position, 0.9f, 1f, false);
        }

        void ApplyLever()
        {
            if (lever != null) lever.localRotation = Quaternion.Euler(on ? leverAngle : -leverAngle, 0f, 0f);
        }
    }
}
