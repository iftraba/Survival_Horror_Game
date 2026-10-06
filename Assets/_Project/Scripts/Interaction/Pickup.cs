using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Objeto recogible del mundo. Es un cuerpo rigido: cae, rebota, se apoya donde puede, y el jugador
    /// puede empujarlo al caminar. Se muestra con el modelo del objeto (ItemData.worldPrefab).
    /// </summary>
    public class Pickup : MonoBehaviour, IInteractable
    {
        public ItemData item;
        public int count = 1;
        [Tooltip("Sin modelo propio: se tine el cubo de reserva con el color del objeto")] public bool tintFallback;

        void Start()
        {
            // Color solo en ejecucion: evita materiales sueltos que no se guardan con la escena
            if (!tintFallback || item == null) return;
            var r = GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = item.tint;
        }

        public string Prompt => item != null ? $"E  Recoger {item.displayName}" + (count > 1 ? $" x{count}" : "") : "";

        public void Interact(GameObject who)
        {
            var inv = who.GetComponent<Inventory>();
            if (inv == null || item == null) return;
            if (item.type == ItemType.Bag)
            {
                // La bolsa no se guarda en el inventario: amplia su capacidad y desaparece
                int added = inv.AddBagSlots(item.extraSlots);
                if (added <= 0) { Hud.Message("No puedes llevar mas bolsas"); return; }
                GameAudio.Play(Sfx.Pickup, who.transform.position, 0.8f, 1f, false);
                if (!string.IsNullOrEmpty(item.pickupObjective)) Objectives.Set(item.pickupObjective);
                ItemShowcase.Open(item, $"+{added} casillas de inventario (permanente)");
                Destroy(gameObject);
                return;
            }
            int left = inv.TryAdd(item, count);
            if (left == count)
            {
                Hud.Message("Inventario lleno");
                return;
            }
            Hud.Message($"Recogiste {item.displayName}");
            GameAudio.Play(Sfx.Pickup, who.transform.position, 0.8f, 1f, false);
            if (!string.IsNullOrEmpty(item.pickupObjective)) Objectives.Set(item.pickupObjective);
            if (left > 0) count = left; else Destroy(gameObject);
        }

        /// <summary>Crea el objeto en el mundo con fisicas. impulse: velocidad inicial (por ejemplo al tirarlo).</summary>
        public static Pickup Spawn(ItemData item, int count, Vector3 position, Vector3 impulse = default)
        {
            var root = new GameObject("Pickup_" + item.displayName);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            bool fallback = item.worldPrefab == null;
            GameObject visual;
            if (!fallback)
            {
                // Se conserva la rotacion del prefab (la importacion del FBX gira el nodo)
                visual = Instantiate(item.worldPrefab, root.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = item.worldPrefab.transform.localScale * item.worldScale;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * 0.3f;
            }
            // El collider lo pone FitBoxCollider; se quitan los del modelo (Destroy en juego, DestroyImmediate en editor)
            foreach (var c in visual.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }

            var p = root.AddComponent<Pickup>();
            p.item = item;
            p.count = count;
            p.tintFallback = fallback;

            FitBoxCollider(root);
            LieFlat(root);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = item.mass;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.6f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (impulse != default) rb.linearVelocity = impulse;
            return p;
        }

        /// <summary>
        /// Coloca el objeto apoyado sobre su cara mas ancha (el eje mas corto de su caja queda vertical) con un
        /// giro aleatorio sobre la vertical. Asi una pistola cae tumbada, no de pie sobre la empunadura.
        /// </summary>
        static void LieFlat(GameObject root)
        {
            var bc = root.GetComponent<BoxCollider>();
            if (bc == null) return;
            var s = bc.size;
            int axis = (s.x <= s.y && s.x <= s.z) ? 0 : (s.y <= s.z ? 1 : 2);
            var local = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
            var t = root.transform;
            t.rotation = Quaternion.FromToRotation(t.TransformDirection(local), Vector3.up) * t.rotation;
            t.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * t.rotation;
        }

        /// <summary>Ajusta un BoxCollider a la malla del modelo, medido en el espacio local de la raiz.</summary>
        public static void FitBoxCollider(GameObject root)
        {
            var bounds = new Bounds();
            bool any = false;
            var toRoot = root.transform.worldToLocalMatrix;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                var m = toRoot * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var pt = m.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(pt, Vector3.zero); any = true; }
                    else bounds.Encapsulate(pt);
                }
            }
            var col = root.AddComponent<BoxCollider>();
            if (any)
            {
                col.center = bounds.center;
                col.size = Vector3.Max(bounds.size, Vector3.one * 0.04f);
            }
        }
    }
}
