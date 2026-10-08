using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Zombi que empieza en el suelo comiendose un cadaver (2026-10-08). Vale para cualquier modelo: el componente va en todos los
    /// prefabs de zombi de pie y se activa marcando 'feeding' en la instancia de la escena. Al empezar cambia su reposo por el bucle
    /// de morder, pone un cadaver y un charco de sangre delante; cuando te ve, le disparan o hay un ruido, se levanta y vuelve a su
    /// controlador normal.
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class ZombieFeeding : MonoBehaviour
    {
        [Tooltip("Empieza comiendose un cadaver")] public bool feeding;
        [Tooltip("Bucle de morder (sustituye al reposo y al golpe mientras come)")] public AnimationClip feedClip;
        [Tooltip("Cadaver que se pone delante (si no hay ya uno en la escena)")] public GameObject corpsePrefab;
        public Material bloodMaterial;
        public bool spawnCorpse = true;

        ZombieAI ai;
        Health health;
        Animator anim;
        RuntimeAnimatorController original;
        bool done;

        void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<Health>();
            anim = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            if (!feeding || anim == null || feedClip == null) { enabled = false; return; }
            original = anim.runtimeAnimatorController;
            var oc = new AnimatorOverrideController(original);
            var pairs = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            oc.GetOverrides(pairs);
            for (int i = 0; i < pairs.Count; i++)
                if (pairs[i].Key.name == "Z_ZombieIdle" || pairs[i].Key.name == "G_ZombieReactionHit")
                    pairs[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, feedClip);
            oc.ApplyOverrides(pairs);
            anim.runtimeAnimatorController = oc;
            anim.Play(0, 0, Random.value);
            if (spawnCorpse) SpawnCorpse();
            ai.Alerted += OnAlerted;
            health.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (ai != null) ai.Alerted -= OnAlerted;
            if (health != null) health.Damaged -= OnDamaged;
        }

        void OnDamaged(Vector3 _) => StandUp();

        // al cambiar de controlador se pierde el disparador del grito que acaba de lanzar la animacion: se repite
        void OnAlerted() { bool was = done; StandUp(); if (!was && anim != null && !health.IsDead) anim.SetTrigger("Alert"); }

        void Update()
        {
            if (!done && ai.IsChasing) StandUp();               // tambien si le despierta un ruido
        }

        void StandUp()
        {
            if (done || anim == null) return;
            done = true;
            if (health.IsDead) return;
            anim.runtimeAnimatorController = original;          // de pie con su juego de siempre
        }

        // cadaver y sangre delante de la boca (las mismas medidas que el carronero de la sala de maquinas)
        void SpawnCorpse()
        {
            Vector3 ground = transform.position;
            var cap = GetComponent<CapsuleCollider>();
            if (cap != null) ground.y += cap.center.y - cap.height * 0.5f;
            var parent = transform.parent;
            if (corpsePrefab != null)
            {
                var c = Instantiate(corpsePrefab, ground + transform.right * -1.1f + transform.forward * 0.58f, transform.rotation * Quaternion.Euler(0f, 90f, 0f), parent);
                c.name = name + "_Cadaver";
            }
            if (bloodMaterial != null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = name + "_Sangre";
                Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(parent);
                q.transform.SetPositionAndRotation(ground + transform.right * -0.2f + transform.forward * 0.5f + Vector3.up * 0.015f, Quaternion.Euler(90f, transform.eulerAngles.y + 20f, 0f));
                q.transform.localScale = new Vector3(3.4f, 3.4f, 1f);
                var r = q.GetComponent<Renderer>();
                r.sharedMaterial = bloodMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
