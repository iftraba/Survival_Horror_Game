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
        [Tooltip("Volumen del disparo. Por encima de 1 suma una segunda fuente: asi los disparos destacan sobre los zombis")]
        [Range(0.5f, 3f)] public float fireVolume = 2f;
        float FireVolume => fireVolume;
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

        void Update()
        {
            if (Equipped == null || GameState.InputBlocked || (health != null && health.IsDead)) return;
            var kb = Keyboard.current;
            var ms = Mouse.current;
            if (kb == null || ms == null) return;

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

        void Fire()
        {
            nextShot = Time.time + 1f / Equipped.fireRate;
            mags[Equipped]--;
            Fired?.Invoke();
            GameAudio.PlayClip(Equipped.fireSound, transform.position, FireVolume, Random.Range(0.96f, 1.04f), false);
            ZombieAI.Noise(transform.position, 14f);

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
                    break;
                }
                Debug.DrawLine(ray.origin, end, Color.yellow, 0.1f);
            }
        }
    }
}
