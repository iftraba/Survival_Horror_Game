using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    [RequireComponent(typeof(Inventory))]
    public class WeaponController : MonoBehaviour
    {
        public Camera aimCamera;
        public PlayerController player;
        [Tooltip("Hueso/objeto de la mano derecha donde se coloca el arma")] public Transform handSocket;
        [Tooltip("Agarre de las armas largas (calculado con la pose de apuntar a dos manos)")] public Transform longSocket;

        public event System.Action Fired;
        public event System.Action ReloadStarted;
        /// <summary>Un proyectil ha dado en la cabeza de un zombi (punto de impacto).</summary>
        public static event System.Action<Vector3> HeadshotLanded;

        public WeaponData Equipped { get; private set; }
        public bool Reloading { get; private set; }
        public int MagAmmo => Equipped != null && mags.TryGetValue(Equipped, out var m) ? m : 0;
        public int ReserveAmmo => Equipped != null ? inventory.CountAmmo(Equipped.ammoType) : 0;

        readonly Dictionary<WeaponData, int> mags = new Dictionary<WeaponData, int>();
        readonly List<ZombieAI> blasted = new List<ZombieAI>();
        Inventory inventory;
        Health health;
        float nextShot;

        void Awake()
        {
            inventory = GetComponent<Inventory>();
            health = GetComponent<Health>();
            if (player == null) player = GetComponent<PlayerController>();
        }

        void Start()
        {
            if (aimCamera == null) aimCamera = Camera.main;
        }

        // Para guardar/cargar: balas que hay en el cargador de cada arma
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<WeaponData, int>> Magazines => mags;
        public void SetMagazine(WeaponData weapon, int ammo) { if (weapon != null) mags[weapon] = ammo; }

        GameObject heldInstance;

        public void Equip(WeaponData weapon)
        {
            StopAllCoroutines();
            Reloading = false;
            Equipped = weapon;
            if (weapon != null && !mags.ContainsKey(weapon)) mags[weapon] = 0;
            ShowHeldModel(weapon);
        }

        void ShowHeldModel(WeaponData weapon)
        {
            if (heldInstance != null) Destroy(heldInstance);
            var socket = weapon != null && weapon.twoHanded && longSocket != null ? longSocket : handSocket;
            if (weapon == null || weapon.heldPrefab == null || socket == null) return;
            // Se conserva la rotacion del prefab: la importacion del FBX gira el nodo para
            // convertir ejes de Blender, y si se borra el arma queda tumbada.
            heldInstance = Instantiate(weapon.heldPrefab, socket);
            heldInstance.transform.localPosition = Vector3.zero;
            heldInstance.transform.localScale = weapon.heldPrefab.transform.localScale * weapon.heldScale;
            foreach (var c in heldInstance.GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        /// <summary>Muestra u oculta el arma de la mano (al abrir una puerta con la mano la guarda un momento).</summary>
        public void SetHeldVisible(bool visible)
        {
            if (heldInstance != null) heldInstance.SetActive(visible);
        }

        void Update()
        {
            if (Equipped == null && (health == null || !health.IsDead)) EnsureWeapon();
            if (GameState.InputBlocked || (health != null && health.IsDead) || PlayerActions.Locked) return;
            var kb = Keyboard.current;
            var ms = Mouse.current;
            if (kb == null || ms == null) return;

            // atajos de arma 1-4 (las asigna el jugador en el inventario): equipa, o guarda si ya esta en la mano
            WeaponHotkeys.EnsureDefaults(inventory);
            var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key };
            for (int k = 0; k < keys.Length; k++)
            {
                if (!keys[k].wasPressedThisFrame) continue;
                var item = WeaponHotkeys.Resolve(inventory, k);
                if (item == null) continue;
                if (Equipped == item.weapon) continue;                       // siempre se lleva un arma: no se guarda
                Equip(item.weapon); Hud.Message(item.displayName + " equipada");
            }
            if (Equipped == null) return;

            if (kb.rKey.wasPressedThisFrame) TryReload();

            if (!player.IsAiming || Reloading) return;
            bool pressed = Equipped.automatic ? ms.leftButton.isPressed : ms.leftButton.wasPressedThisFrame;
            if (!pressed || Time.time < nextShot) return;

            if (MagAmmo <= 0)
            {
                nextShot = Time.time + 0.4f;
                if (ReserveAmmo > 0) TryReload();
                else
                {
                    Hud.Message("Sin municion");
                    GameAudio.Play(Sfx.DryFire, transform.position, 0.8f, 1f, false);
                }
                return;
            }
            Fire();
        }

        void TryReload()
        {
            if (Equipped == null || Reloading) return;
            if (MagAmmo >= Equipped.magazineSize || ReserveAmmo <= 0) return;
            StartCoroutine(ReloadRoutine());
        }

        IEnumerator ReloadRoutine()
        {
            Reloading = true;
            ReloadStarted?.Invoke();
            GameAudio.PlayClip(Equipped.reloadSound, transform.position, 0.8f, 1f, false);
            var weapon = Equipped;
            yield return new WaitForSeconds(weapon.reloadTime);
            int needed = weapon.magazineSize - mags[weapon];
            mags[weapon] += inventory.ConsumeAmmo(weapon.ammoType, needed);
            Reloading = false;
        }

        /// <summary>Siempre se lleva un arma en la mano: si no hay ninguna equipada, se equipa la de la tecla 1-4 (o la primera del inventario).</summary>
        void EnsureWeapon()
        {
            ItemData pick = null;
            for (int k = 0; k < WeaponHotkeys.Count && pick == null; k++) pick = WeaponHotkeys.Resolve(inventory, k);
            if (pick == null) foreach (var s in inventory.slots) if (!s.IsEmpty && s.item.type == ItemType.Weapon && s.item.weapon != null) { pick = s.item; break; }
            if (pick != null && pick.weapon != null) Equip(pick.weapon);
        }

        // bombeo de la escopeta: suena un momento despues del disparo si sigues con esa arma y no has empezado a recargar
        IEnumerator CycleRoutine(WeaponData weapon)
        {
            yield return new WaitForSeconds(weapon.cycleDelay);
            if (Equipped != weapon) yield break;
            if (weapon.ejectAtCycle) EjectCasing(weapon);          // el cartucho vacio sale al bombear, aunque fuera el ultimo
            if (!Reloading && mags[weapon] > 0)
                GameAudio.PlayClip(weapon.cycleSound, transform.position, weapon.cycleVolume, 1f, false);
        }

        // Casquillo o cartucho vacio: sale de la mano del arma hacia la derecha de la camara, con fisica
        void EjectCasing(WeaponData w)
        {
            if (w.casingPrefab == null || heldInstance == null || aimCamera == null) return;
            var ct = aimCamera.transform;
            Vector3 pos = heldInstance.transform.position + ct.right * w.ejectOffset.x + Vector3.up * w.ejectOffset.y + ct.forward * w.ejectOffset.z;
            var go = Instantiate(w.casingPrefab, pos, Random.rotation);
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = ct.right * (w.ejectSpeed * Random.Range(0.75f, 1.2f)) + Vector3.up * Random.Range(1.1f, 1.9f) - ct.forward * Random.Range(0f, 0.5f);
                rb.angularVelocity = Random.insideUnitSphere * 25f;
            }
            // no choca con el jugador (si no se le atascaria en los pies)
            var cc = player != null ? player.GetComponent<CharacterController>() : null;
            if (cc != null) foreach (var col in go.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(col, cc);
        }

        void Fire()
        {
            nextShot = Time.time + 1f / Equipped.fireRate;
            mags[Equipped]--;
            Fired?.Invoke();
            Hud.CrosshairKick();
            GameAudio.PlayClip(Equipped.fireSound, transform.position, Equipped.fireVolume, Random.Range(0.96f, 1.04f), false);
            ZombieAI.Noise(transform.position, 14f);
            if (Equipped.cycleSound != null || Equipped.ejectAtCycle) StartCoroutine(CycleRoutine(Equipped));
            if (!Equipped.ejectAtCycle) EjectCasing(Equipped);

            // zombis alcanzados por este disparo (la escopeta los derriba o aturde una vez por disparo, no por perdigon)
            blasted.Clear();
            for (int p = 0; p < Equipped.pellets; p++)
            {
                var spread = Quaternion.Euler(Random.Range(-Equipped.spread, Equipped.spread),
                                              Random.Range(-Equipped.spread, Equipped.spread), 0f);
                var ray = new Ray(aimCamera.transform.position, aimCamera.transform.rotation * spread * Vector3.forward);
                var end = ray.origin + ray.direction * Equipped.range;

                var hits = Physics.RaycastAll(ray, Equipped.range, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider.transform.IsChildOf(transform)) continue;
                    if (hit.collider.GetComponentInParent<Pickup>() != null) continue;   // los objetos del suelo no paran las balas
                    float damage = Equipped.damage;
                    Vector3 point = hit.point;
                    // Zombis: la capsula solo avisa de que la bala llega; las zonas del esqueleto deciden si toca y donde
                    var zones = hit.collider.GetComponentInParent<ZombieHitZones>();
                    if (zones != null)
                    {
                        if (!zones.Test(ray, Equipped.range, out float mult, out point, out bool headshot)) continue;   // roza la capsula pero no el cuerpo
                        damage *= mult;
                        if (headshot) HeadshotLanded?.Invoke(point);
                    }
                    end = point;
                    var target = hit.collider.GetComponentInParent<IDamageable>();
                    target?.TakeDamage(damage, point);
                    var zai = hit.collider.GetComponentInParent<ZombieAI>();
                    if (zai != null && !blasted.Contains(zai)) blasted.Add(zai);
                    break;
                }
                Debug.DrawLine(ray.origin, end, Color.yellow, 0.1f);
            }
            if (Equipped.pellets > 1)
                foreach (var z in blasted)
                    if (z != null && !z.Hp.IsDead) z.ShotgunBlast(Vector3.Distance(transform.position, z.transform.position));
        }
    }
}
