using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Cambia el modelo del protagonista por el personaje elegido en el menu oculto (GameSettings.Character, 2026-10-17).
    /// El prefab del jugador lleva el soldado como hijo "Model"; si se eligio otro, se instancia su prefab de modelo
    /// (Animator + WeaponHolder y LongWeaponHolder en la mano derecha) en su lugar antes de que lo lean la animacion y las armas.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerModelSwap : MonoBehaviour
    {
        [Tooltip("Prefabs de modelo por indice de GameSettings.Character; el 0 (soldado) es el que ya lleva el prefab")]
        public GameObject[] models;

        void Awake()
        {
            int i = GameSettings.Character;
            if (i <= 0 || models == null || i >= models.Length || models[i] == null) return;
            var old = transform.Find("Model");
            var m = Instantiate(models[i], transform);
            m.name = "Model";
            if (old != null)
            {
                m.transform.localPosition = old.localPosition;
                m.transform.localRotation = old.localRotation;
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }
            var an = m.GetComponent<Animator>();
            var pa = GetComponent<PlayerAnimation>();
            if (pa != null && an != null) pa.animator = an;
            var wc = GetComponent<WeaponController>();
            if (wc != null)
            {
                var hand = Find(m.transform, "WeaponHolder");
                var lng = Find(m.transform, "LongWeaponHolder");
                if (hand != null) wc.handSocket = hand;
                if (lng != null) wc.longSocket = lng;
            }
        }

        static Transform Find(Transform root, string n)
        {
            if (root.name == n) return root;
            foreach (Transform ch in root) { var r = Find(ch, n); if (r != null) return r; }
            return null;
        }
    }
}
