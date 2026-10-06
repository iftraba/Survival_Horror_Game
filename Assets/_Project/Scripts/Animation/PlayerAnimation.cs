using System.Collections.Generic;
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
        static readonly int LongGunId = Animator.StringToHash("LongGun");
        static readonly int MoveXId = Animator.StringToHash("MoveX");
        static readonly int MoveYId = Animator.StringToHash("MoveY");

        // parametros que tiene el controlador actual (el de Mixamo no tiene recarga ni golpe hasta que haya esos clips)
        readonly HashSet<int> has = new HashSet<int>();
        RuntimeAnimatorController hasFor;

        void CacheParams()
        {
            if (animator == null || hasFor == animator.runtimeAnimatorController) return;
            hasFor = animator.runtimeAnimatorController;
            has.Clear();
            foreach (var p in animator.parameters) has.Add(p.nameHash);
        }

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
            CacheParams();
            var v = body.velocity;
            v.y = 0f;
            animator.SetFloat(SpeedId, v.magnitude, 0.15f, Time.deltaTime);   // amortiguado: arranques y paradas suaves
            if (has.Contains(MoveXId))
            {
                // velocidad local (derecha / delante) normalizada a la de apuntar: mueve el mapa de pasos de 8 direcciones
                float aimSpeed = Mathf.Max(0.1f, player.aimSpeed);
                var local = transform.InverseTransformDirection(v) / aimSpeed;
                local = Vector3.ClampMagnitude(local, 1f);
                animator.SetFloat(MoveXId, local.x, 0.1f, Time.deltaTime);
                animator.SetFloat(MoveYId, local.z, 0.1f, Time.deltaTime);
            }
            if (has.Contains(LongGunId)) animator.SetBool(LongGunId, longGun);
            animator.SetBool(AimingId, player.IsAiming);
            animator.SetBool(ArmedId, weapons != null && weapons.Equipped != null);
        }

        void OnFired() { CacheParams(); if (animator != null && has.Contains(ShootId)) animator.SetTrigger(ShootId); }
        void OnReload() { CacheParams(); if (animator != null && has.Contains(ReloadId)) animator.SetTrigger(ReloadId); }
        void OnDamaged(Vector3 _) { CacheParams(); if (animator != null && has.Contains(HitId)) animator.SetTrigger(HitId); }
        void OnDied() => animator?.SetBool(DeadId, true);
    }
}
