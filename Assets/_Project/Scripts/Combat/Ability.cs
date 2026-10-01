using System.Collections;
using System.Collections.Generic;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.Combat
{
    public abstract class Ability : MonoBehaviour
    {
        private const float StateTimeout = 4f;
        private static readonly Collider[] OverlapBuffer = new Collider[16];

        [SerializeField] private AbilityData data;

        private readonly List<Health> targetBuffer = new List<Health>();
        private float readyTime;

        public AbilityData Data => data;
        public bool IsReady => Time.time >= readyTime;
        public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.time);
        public float CooldownNormalized => data.Cooldown > 0f ? CooldownRemaining / data.Cooldown : 0f;

        protected AbilityCaster Caster { get; private set; }
        protected bool IsAlive => !Caster.Health.IsDead;

        public void Initialize(AbilityCaster caster)
        {
            Caster = caster;
        }

        public virtual void WarmUpEffects()
        {
            EffectWarmup.Request(data.CastEffect);
            EffectWarmup.Request(data.ImpactEffect);
        }

        public IEnumerator Cast()
        {
            readyTime = Time.time + data.Cooldown;
            Caster.PlaySound(data.CastSound);
            yield return Perform();
        }

        protected abstract IEnumerator Perform();

        protected IEnumerator WaitForStateTime(int stateHash, float normalizedTime)
        {
            float deadline = Time.time + StateTimeout;
            while (IsAlive && Time.time < deadline)
            {
                if (Caster.DragonAnimator.TryGetStateTime(stateHash, out float time) && time >= normalizedTime)
                {
                    yield break;
                }
                yield return null;
            }
        }

        protected IEnumerator WaitForLocomotion()
        {
            yield return new WaitUntil(() => !IsAlive || !Caster.DragonAnimator.IsBusy);
        }

        protected List<Health> CollectTargets(Vector3 center, float radius)
        {
            targetBuffer.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, radius, OverlapBuffer, Caster.TargetMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Health target = OverlapBuffer[i].GetComponentInParent<Health>();
                if (target != null && target != Caster.Health && !target.IsDead && !targetBuffer.Contains(target))
                {
                    targetBuffer.Add(target);
                }
            }
            return targetBuffer;
        }

        protected Health FindClosestTarget(float radius)
        {
            Health closest = null;
            float closestDistance = float.MaxValue;
            foreach (Health target in CollectTargets(transform.position, radius))
            {
                float distance = (target.transform.position - transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = target;
                }
            }
            return closest;
        }

        protected void ApplyHit(Health target, Vector3 sourcePoint, bool playFeedback = true)
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            target.TakeDamage(data.Damage);

            Vector3 direction = target.transform.position - sourcePoint;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.forward;
            }
            if (data.Knockback > 0f && target.TryGetComponent(out DragonMotor targetMotor))
            {
                targetMotor.ApplyKnockback(direction.normalized * data.Knockback);
            }

            if (!playFeedback)
            {
                return;
            }

            Vector3 hitPoint = target.TryGetComponent(out Collider targetCollider)
                ? targetCollider.bounds.center
                : target.transform.position;
            EffectSpawner.Spawn(data.ImpactEffect, hitPoint, Quaternion.identity, data.EffectLifetime);
            Caster.PlaySoundAt(data.ImpactSound, hitPoint);
        }
    }
}
