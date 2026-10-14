using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Brillo suave y pulsante de un objeto clave (cizalla, tarjetas, medallones, fusibles, llaves) para que no se confunda con el
    /// desorden. Es una luz puntual pequena, sin sombra, que solo se enciende cuando el jugador esta a menos de showDistance.
    /// Se anade sola desde Pickup.Spawn cuando ItemData.highlight es true.
    /// </summary>
    [RequireComponent(typeof(Pickup))]
    public class PickupGlint : MonoBehaviour
    {
        public float showDistance = 10f;
        public float range = 2.4f;
        public float minIntensity = 1.5f, maxIntensity = 4.5f;
        [Tooltip("Pulsos por segundo")] public float speed = 0.5f;

        Light glow;
        Transform player;
        float nextCheck, phase;

        void Awake()
        {
            var p = GetComponent<Pickup>();
            var c = p != null && p.item != null ? Color.Lerp(p.item.tint, Color.white, 0.55f) : new Color(1f, 0.9f, 0.6f);
            var go = new GameObject("Brillo");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.18f;
            glow = go.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = c;
            glow.range = range;
            glow.intensity = 0f;
            glow.shadows = LightShadows.None;
            glow.enabled = false;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + 0.25f;
                if (player == null) { var pl = GameObject.FindWithTag("Player"); if (pl != null) player = pl.transform; }
                glow.enabled = player != null && (player.position - transform.position).sqrMagnitude < showDistance * showDistance;
            }
            if (glow.enabled)
                glow.intensity = Mathf.Lerp(minIntensity, maxIntensity, 0.5f + 0.5f * Mathf.Sin(phase + Time.time * speed * Mathf.PI * 2f));
        }
    }
}
