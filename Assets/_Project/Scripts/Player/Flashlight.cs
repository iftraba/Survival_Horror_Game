using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>Linterna del jugador. F la enciende y apaga; parpadea muy ligeramente.</summary>
    [RequireComponent(typeof(Light))]
    public class Flashlight : MonoBehaviour
    {
        public bool startOn = true;
        [Range(0f, 0.3f)] public float flicker = 0.04f;

        Light spot;
        float baseIntensity;
        bool on;

        void Awake()
        {
            spot = GetComponent<Light>();
            baseIntensity = spot.intensity;
            on = startOn;
            spot.enabled = on;
        }

        void Update()
        {
            if (!GameState.InputBlocked && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                on = !on;
                spot.enabled = on;
                GameAudio.Play(Sfx.Flashlight, transform.position, 0.7f, on ? 1f : 0.9f, false);
            }
            if (on && flicker > 0f)
                spot.intensity = baseIntensity * (1f - flicker * Mathf.PerlinNoise(Time.time * 9f, 0.3f));
        }
    }
}
