using UnityEngine;

namespace Horror
{
    /// <summary>Conecta el estado del jugador con el Animator (PlayerAnimator.controller).</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimation : MonoBehaviour
    {
        public Animator animator;
        [Tooltip("Controlador con pistola / sin arma")] public RuntimeAnimatorController handgunController;
        [Tooltip("Override con las animaciones de arma larga a dos manos (escopeta)")] public RuntimeAnimatorController longGunController;

        const int UpperBodyLayer = 1;

        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int AimingId = Animator.StringToHash("Aiming");
        static readonly int ArmedId = Animator.StringToHash("Armed");
        static readonly int ShootId = Animator.StringToHash("Shoot");
        static readonly int ReloadId = Animator.StringToHash("Reload");
        static readonly int HitId = Animator.StringToHash("Hit");
        static readonly int DeadId = Animator.StringToHash("Dead");

        PlayerController player;
        CharacterController body;
        Health health;
        WeaponController weapons;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            body = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            weapons = GetComponent<WeaponController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
        }

        void OnEnable()
        {
            if (weapons != null) { weapons.Fired += OnFired; weapons.ReloadStarted += OnReload; }
            if (health != null) { health.Damaged += OnDamaged; health.Died += OnDied; }
        }

        void OnDisable()
        {
            if (weapons != null) { weapons.Fired -= OnFired; weapons.ReloadStarted -= OnReload; }
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; }
        }

        void Update()
        {
            if (animator == null) return;
            // Arma larga equipada: juego de animaciones a dos manos (el cambio de controlador se hace solo al cambiar)
            bool longGun = weapons != null && weapons.Equipped != null && weapons.Equipped.twoHanded;
            var wanted = longGun && longGunController != null ? longGunController : handgunController;
            if (wanted != null && animator.runtimeAnimatorController != wanted)
                animator.runtimeAnimatorController = wanted;
            // Al morir manda la animacion de cuerpo completo; la capa del torso se apaga
            if (animator.layerCount > UpperBodyLayer)
                animator.SetLayerWeight(UpperBodyLayer, health != null && health.IsDead ? 0f : 1f);
            var v = body.velocity;
            v.y = 0f;
            animator.SetFloat(SpeedId, v.magnitude, 0.15f, Time.deltaTime);   // amortiguado: arranques y paradas suaves
            animator.SetBool(AimingId, player.IsAiming);
            animator.SetBool(ArmedId, weapons != null && weapons.Equipped != null);
        }

        void OnFired() => animator?.SetTrigger(ShootId);
        void OnReload() => animator?.SetTrigger(ReloadId);
        void OnDamaged(Vector3 _) => animator?.SetTrigger(HitId);
        void OnDied() => animator?.SetBool(DeadId, true);
    }
}
