using UnityEngine;

namespace Horror
{
    /// <summary>Luz de un fuego: intensidad y alcance que bailan (ruido) alrededor de sus valores.</summary>
    [RequireComponent(typeof(Light))]
    public class FireLight : MonoBehaviour
    {
        public float flicker = 0.35f, speed = 9f;
        Light l; float baseI, baseR, seed;

        void Awake() { l = GetComponent<Light>(); baseI = l.intensity; baseR = l.range; seed = Random.value * 100f; }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
            l.intensity = baseI * (1f + n * flicker);
            l.range = baseR * (1f + n * flicker * 0.3f);
        }
    }
}
