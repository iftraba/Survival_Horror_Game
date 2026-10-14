using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Brillo suave y pulsante de los objetos recogibles para que no se pierdan en el suelo oscuro (2026-10-14: ahora todos, no solo los clave).
    /// Es una luz puntual pequena sin sombra mas un halo aditivo que mira a la camara, y solo se encienden cuando el jugador esta a menos de showDistance.
    /// Los objetos clave (ItemData.highlight) brillan mas y desde mas lejos que el resto (municion, curas...). Se anade sola desde Pickup.Spawn.
    /// </summary>
    [RequireComponent(typeof(Pickup))]
    public class PickupGlint : MonoBehaviour
    {
        public float showDistance = 10f;
        public float range = 2.4f;
        public float minIntensity = 1.5f, maxIntensity = 4.5f;
        [Tooltip("Pulsos por segundo")] public float speed = 0.5f;

        Light glow;
        Transform player, halo, cam;
        Renderer haloRenderer;
        Color haloColor;
        bool key;
        float nextCheck, phase;

        void Awake()
        {
            var p = GetComponent<Pickup>();
            key = p != null && p.item != null && p.item.highlight;
            var c = p != null && p.item != null ? Color.Lerp(p.item.tint, Color.white, 0.55f) : new Color(1f, 0.9f, 0.6f);
            if (!key) { showDistance = 12f; range = 1.7f; minIntensity = 0.6f; maxIntensity = 1.8f; }
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
            // halo aditivo (Resources/PickupHalo: material sin iluminar con la textura suave de particulas)
            var mat = Resources.Load<Material>("PickupHalo");
            if (mat != null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Halo"; Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(transform, false); q.transform.localPosition = Vector3.up * 0.16f;
                float size = key ? 0.8f : 0.5f; q.transform.localScale = new Vector3(size, size, 1f);
                haloRenderer = q.GetComponent<Renderer>(); haloRenderer.sharedMaterial = mat; haloRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                haloRenderer.enabled = false; halo = q.transform; haloColor = Color.Lerp(c, Color.white, 0.2f);
            }
        }

        void Update()
        {
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + 0.25f;
                if (player == null) { var pl = GameObject.FindWithTag("Player"); if (pl != null) player = pl.transform; }
                glow.enabled = player != null && (player.position - transform.position).sqrMagnitude < showDistance * showDistance;
                if (haloRenderer != null) haloRenderer.enabled = glow.enabled;
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase + Time.time * speed * Mathf.PI * 2f);
            if (glow.enabled) glow.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
            if (halo != null && haloRenderer.enabled)
            {
                if (cam == null && Camera.main != null) cam = Camera.main.transform;
                if (cam != null) halo.rotation = Quaternion.LookRotation(halo.position - cam.position);
                var hc = haloColor * Mathf.Lerp(key ? 0.55f : 0.32f, key ? 1.1f : 0.6f, pulse); hc.a = 1f;
                haloRenderer.material.SetColor("_BaseColor", hc);
                haloRenderer.material.SetColor("_Color", hc);
            }
        }
    }
}
