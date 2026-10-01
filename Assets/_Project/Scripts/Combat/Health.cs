using System;
using UnityEngine;

namespace DragonBattle.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        public event Action<Health, float> OnDamaged;
        public event Action<Health> OnDied;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float Normalized => CurrentHealth / maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            float applied = Mathf.Min(amount, CurrentHealth);
            CurrentHealth -= applied;
            OnDamaged?.Invoke(this, applied);

            if (IsDead)
            {
                OnDied?.Invoke(this);
            }
        }
    }
}
