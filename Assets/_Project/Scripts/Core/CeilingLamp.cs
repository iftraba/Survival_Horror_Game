using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Lampara de techo: se puede apagar desde un interruptor, puede parpadear y apagarse a ratos,
    /// y su panel emisivo (objeto hijo "Bulb") y su zumbido siguen siempre a la luz.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class CeilingLamp : MonoBehaviour
    {
        public bool flicker;
        [Range(0f, 1f)] public float amount = 0.5f;
        public float speed = 6f;
        [Tooltip("Intensidad encendida. Se guarda aparte porque la de la Light queda a 0 si la escena se guardo con la lampara apagada")]
        public float ratedIntensity;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Light lamp;
        Light[] fills;
        float[] fillIntensity;
        Renderer bulb;
        MaterialPropertyBlock block;
        AudioSource hum;
        Color baseEmission;
        float baseIntensity;
        float seed;
        bool wasBlackout;

        public bool Powered { get; private set; } = true;

        bool initialized;

        void Awake() => EnsureInit();

        // Tambien se llama desde SetPowered/Apply: asi funciona aunque algo lo use antes del Awake
        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            lamp = GetComponent<Light>();
            if (ratedIntensity <= 0f) ratedIntensity = lamp.intensity;
            baseIntensity = ratedIntensity;
            // Luces de relleno hijas (sin sombras, iluminan la sala entera): siguen a la lampara principal
            var all = GetComponentsInChildren<Light>(true);
            fills = new Light[all.Length - 1];
            fillIntensity = new float[fills.Length];
            for (int i = 0, j = 0; i < all.Length; i++)
            {
                if (all[i] == lamp) continue;
                fills[j] = all[i];
                var tag = all[i].GetComponent<FillLightRating>();
                fillIntensity[j] = tag != null && tag.rated > 0f ? tag.rated : all[i].intensity;
                j++;
            }
            seed = Random.value * 100f;

            var b = transform.Find("Bulb");
            bulb = b != null ? b.GetComponent<Renderer>() : null;
            if (bulb != null && bulb.sharedMaterial != null && bulb.sharedMaterial.HasProperty(EmissionId))
            {
                block = new MaterialPropertyBlock();
                baseEmission = bulb.sharedMaterial.GetColor(EmissionId);
            }
        }

        void Start()
        {
            var audio = GameAudio.Instance;
            if (audio != null && audio.lampHum != null)
            {
                hum = gameObject.AddComponent<AudioSource>();
                hum.clip = audio.lampHum;
                hum.loop = true;
                hum.spatialBlend = 1f;
                hum.rolloffMode = AudioRolloffMode.Logarithmic;
                hum.minDistance = 1f;
                hum.maxDistance = 9f;
                hum.volume = 0.12f;
                hum.time = Random.value * audio.lampHum.length;
                hum.Play();
            }
        }

        public void SetPowered(bool on)
        {
            EnsureInit();
            Powered = on;
            if (!on) Apply(false, 0f);
        }

        void Update()
        {
            if (!Powered) return;

            float k = 1f;
            bool blackout = false;
            if (flicker)
            {
                float n = Mathf.PerlinNoise(seed, Time.time * speed);
                // Apagones breves: la luz se APAGA del todo (no solo se atenua: con el techo tan
                // cerca, hasta un 10 % dejaria una mancha brillante)
                blackout = n < 0.18f;
                k = blackout ? 0f : Mathf.Lerp(1f, n * 1.6f, amount);
                if (blackout && !wasBlackout) GameAudio.Play(Sfx.LampZap, transform.position, 0.6f);
            }
            wasBlackout = blackout;
            Apply(!blackout, k);
        }

        void Apply(bool lit, float k)
        {
            EnsureInit();
            lamp.enabled = lit;
            lamp.intensity = baseIntensity * k;
            for (int i = 0; i < fills.Length; i++)
            {
                fills[i].enabled = lit;
                fills[i].intensity = fillIntensity[i] * k;
            }

            if (block != null)
            {
                bulb.GetPropertyBlock(block);
                block.SetColor(EmissionId, baseEmission * (lit ? k : 0f));
                bulb.SetPropertyBlock(block);
            }
            if (hum != null)
            {
                if (lit && !hum.isPlaying) hum.UnPause();
                else if (!lit && hum.isPlaying) hum.Pause();
            }
        }
    }
}
