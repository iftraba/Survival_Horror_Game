using System;
using UnityEngine;

namespace Horror
{
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(float amount, Vector3 hitPoint);
    }

    public class Health : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f;
        [Tooltip("Multiplica el dano recibido (el jefe aturdido recibe mas). 1 = normal")]
        public float damageTakenMultiplier = 1f;

        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        public event Action<float, float> Changed;
        public event Action<Vector3> Damaged;
        public event Action Died;

        void Awake() => Current = maxHealth;

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            if (IsDead) return;
            Current = Mathf.Max(0f, Current - amount * damageTakenMultiplier);
            Changed?.Invoke(Current, maxHealth);
            Damaged?.Invoke(hitPoint);
            if (IsDead) Died?.Invoke();
        }

        /// <summary>Fija la vida directamente (al cargar una partida), sin disparar dano ni muerte.</summary>
        public void SetCurrent(float value)
        {
            Current = Mathf.Clamp(value, 0f, maxHealth);
            Changed?.Invoke(Current, maxHealth);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }
    }
}
