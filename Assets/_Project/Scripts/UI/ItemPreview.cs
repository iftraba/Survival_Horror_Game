using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Horror
{
    /// <summary>
    /// "Examinar objeto" del inventario: un pequeno estudio fotografico lejos del nivel (camara, luces y el modelo
    /// del objeto girando) que se renderiza en una textura que dibuja el HUD.
    /// </summary>
    public class ItemPreview : MonoBehaviour
    {
        static ItemPreview instance;

        Camera cam;
        Transform pivot;
        GameObject model;
        ItemData shown;
        public RenderTexture Texture { get; private set; }

        static readonly Vector3 StudioPos = new Vector3(0f, -400f, 0f);

        public static ItemPreview Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("ItemPreviewStudio");
            go.transform.position = StudioPos;
            instance = go.AddComponent<ItemPreview>();
            instance.Build();
            return instance;
        }

        void Build()
        {
            Texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32) { name = "ItemPreview", antiAliasing = 4 };
            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.12f, -1.1f);
            camGo.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 24f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 4f;
            cam.targetTexture = Texture;
            cam.enabled = false;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;

            Light L(string n, Vector3 pos, float intensity, Color c)
            {
                var lgo = new GameObject(n);
                lgo.transform.SetParent(transform, false);
                lgo.transform.localPosition = pos;
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Point; l.range = 4f; l.intensity = intensity; l.color = c; l.shadows = LightShadows.None;
                return l;
            }
            L("Key", new Vector3(-0.6f, 0.7f, -0.8f), 3.5f, new Color(1f, 0.95f, 0.88f));
            L("Fill", new Vector3(0.8f, 0.1f, -0.6f), 1.2f, new Color(0.7f, 0.8f, 1f));
            L("Rim", new Vector3(0.2f, 0.6f, 0.9f), 2.5f, Color.white);

            pivot = new GameObject("Pivot").transform;
            pivot.SetParent(transform, false);
        }

        /// <summary>Activa el estudio mostrando 'item' (null lo apaga).</summary>
        public void Show(ItemData item)
        {
            cam.enabled = item != null;
            if (item == shown) return;
            shown = item;
            if (model != null) Destroy(model);
            model = null;
            if (item == null || item.worldPrefab == null) return;

            model = Instantiate(item.worldPrefab, pivot);
            foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in model.GetComponentsInChildren<Rigidbody>()) Destroy(r);
            // encuadre: centrado en el pivote y escalado para que su lado mayor mida 0.42 m
            var b = Bounds(model);
            float k = 0.42f / Mathf.Max(0.001f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            model.transform.localScale *= k;
            b = Bounds(model);
            model.transform.position += pivot.position - b.center;
        }

        void Update()
        {
            if (pivot != null && cam.enabled)
                pivot.localRotation = Quaternion.Euler(18f * Mathf.Sin(Time.unscaledTime * 0.7f), Time.unscaledTime * 40f, 0f);
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        void OnDestroy()
        {
            if (Texture != null) Texture.Release();
            if (instance == this) instance = null;
        }
    }
}
