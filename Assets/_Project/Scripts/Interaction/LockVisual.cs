using System.Collections;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Lo que se ve de una cerradura (comisaria grande): el candado con su cadena en la hoja, o el piloto rojo del lector de
    /// tarjetas. Mientras la puerta siga cerrada con llave se ve 'lockedVisual'; al desbloquearla se apaga y se enciende
    /// 'unlockedVisual' (piloto verde). Tambien al cargar una partida con la puerta ya abierta (entonces sin animacion).
    /// Con dropOnUnlock, el candado no desaparece de golpe: se corta, se suelta, cae al suelo con fisica y se desvanece.
    /// </summary>
    public class LockVisual : MonoBehaviour
    {
        public Door door;
        public GameObject lockedVisual;
        public GameObject unlockedVisual;
        [Tooltip("Al desbloquear en juego, el objeto bloqueado (el candado) se suelta y cae con fisica en vez de apagarse")] public bool dropOnUnlock;
        [Tooltip("Pitido al desbloquear en juego (lector de tarjetas)")] public bool beepOnUnlock;
        [Tooltip("Segundos que el candado caido queda en el suelo antes de desvanecerse")] public float lingerSeconds = 5f;
        bool last = true;

        void Start() => Apply(true);
        void Update() => Apply(false);

        void Apply(bool force)
        {
            if (door == null) return;
            bool locked = !door.IsUnlocked;
            if (!force && locked == last) return;
            last = locked;
            bool live = !force && !locked;   // se acaba de desbloquear jugando
            if (lockedVisual != null)
            {
                if (live && dropOnUnlock && lockedVisual.activeSelf) Cut();
                else lockedVisual.SetActive(locked);
            }
            if (unlockedVisual != null) unlockedVisual.SetActive(!locked);
            if (live && beepOnUnlock) GameAudio.Play(Sfx.Switch, transform.position, 0.7f, 1.5f);
        }

        // Corta el candado: chasquido, se desengancha de la puerta, cae con fisica y se desvanece
        void Cut()
        {
            var t = lockedVisual.transform;
            GameAudio.Play(Sfx.LampZap, t.position, 0.6f, 0.6f);
            t.SetParent(null, true);
            Pickup.FitBoxCollider(lockedVisual);
            var col = lockedVisual.GetComponent<BoxCollider>();
            // que no choque con la propia puerta (nace pegado a la hoja)
            foreach (var dc in door.GetComponentsInChildren<Collider>(true)) if (col != null && dc != null) Physics.IgnoreCollision(col, dc);
            var rb = lockedVisual.AddComponent<Rigidbody>();
            rb.mass = 0.6f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = door.Normal * 0.4f + Vector3.up * 0.5f;
            rb.angularVelocity = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));
            StartCoroutine(Fade(lockedVisual));
        }

        IEnumerator Fade(GameObject go)
        {
            yield return new WaitForSeconds(lingerSeconds);
            if (go == null) yield break;
            var s0 = go.transform.localScale;
            for (float k = 0f; k < 0.6f; k += Time.deltaTime)
            {
                if (go == null) yield break;
                go.transform.localScale = s0 * (1f - k / 0.6f);
                yield return null;
            }
            if (go != null) Destroy(go);
        }
    }
}
