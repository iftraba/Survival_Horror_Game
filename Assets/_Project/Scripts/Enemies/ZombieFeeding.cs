using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Zombi que empieza en el suelo comiendose un cadaver (2026-10-08). Vale para cualquier modelo: el componente va en todos los
    /// prefabs de zombi de pie y se activa marcando 'feeding' en la instancia de la escena. Al empezar cambia su reposo por el bucle
    /// de morder y pone un cadaver (con el pecho justo bajo su cabeza, medido con los huesos: vale para cualquier modelo) y un charco
    /// de sangre. No le distrae nada: ni verte ni los ruidos; solo se levanta cuando le disparas (letargo que solo rompe un disparo).
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
            ai.dormant = true;                                  // sigue comiendo pase lo que pase...
            ai.wakeOnlyWhenShot = true;                         // ...hasta que le disparan
            if (spawnCorpse) StartCoroutine(SpawnCorpse());
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

        void StandUp()
        {
            if (done || anim == null) return;
            done = true;
            if (health.IsDead) return;
            anim.runtimeAnimatorController = original;          // de pie con su juego de siempre
        }

        // cadaver y sangre: el pecho del cadaver queda justo bajo la cabeza del zombi mientras muerde
        System.Collections.IEnumerator SpawnCorpse()
        {
            Vector3 ground = transform.position;
            var cap = GetComponent<CapsuleCollider>();
            if (cap != null) ground.y += cap.center.y - cap.height * 0.5f;
            var parent = transform.parent;
            Vector3 chestAt = ground + transform.forward * 0.6f;
            if (corpsePrefab != null)
            {
                // tumbado de traves delante del zombi
                var c = Instantiate(corpsePrefab, ground + transform.forward * 0.6f, transform.rotation * Quaternion.Euler(0f, 90f, 0f), parent);
                c.name = name + "_Cadaver";
                var ca0 = c.GetComponentInChildren<Animator>();
                if (ca0 != null) { ca0.Play(0, 0, 1f); ca0.Update(0f); }   // ya tumbado (ultimo fotograma de su caida), sin verle caer
                var cull = anim.cullingMode;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;    // fuera de camara tambien hay que medir los huesos
                yield return null;                                       // ya animados: el zombi mordiendo y el cadaver tumbado
                anim.Update(0f);
                anim.cullingMode = cull;
                var za = anim.GetBoneTransform(HumanBodyBones.Head);
                var ca = c.GetComponentInChildren<Animator>();
                var chest = ca != null && ca.isHuman ? (ca.GetBoneTransform(HumanBodyBones.Chest) ?? ca.GetBoneTransform(HumanBodyBones.Spine)) : null;
                if (za != null && chest != null)
                {
                    Vector3 d = za.position - chest.position; d.y = 0f;
                    c.transform.position += d;                           // pecho bajo la boca
                    chestAt = chest.position;
                }
            }
            chestAt.y = ground.y;
            if (bloodMaterial != null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = name + "_Sangre";
                Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(parent);
                q.transform.SetPositionAndRotation(chestAt + Vector3.up * 0.015f, Quaternion.Euler(90f, transform.eulerAngles.y + 20f, 0f));
                q.transform.localScale = new Vector3(3.4f, 3.4f, 1f);
                var r = q.GetComponent<Renderer>();
                r.sharedMaterial = bloodMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
