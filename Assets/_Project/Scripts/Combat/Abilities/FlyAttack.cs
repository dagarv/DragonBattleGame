using System;
using System.Collections;
using DragonBattle.Audio;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.Combat.Abilities
{
    public class FlyAttack : Ability
    {
        [Header("Flight")]
        [SerializeField, Min(0f)] private float flySpeed = 9f;
        [SerializeField, Min(0f)] private float hoverDistance = 6f;
        [SerializeField, Min(0f)] private float arrivalSharpness = 3f;
        [SerializeField, Min(0f)] private float maxChaseTime = 1.2f;
        [SerializeField, Range(0f, 1f)] private float liftOffTime = 0.45f;

        [Header("Altitude")]
        [SerializeField, Min(0f)] private float extraAltitude = 2.5f;
        [SerializeField, Min(0f)] private float altitudeSharpness = 6f;
        [SerializeField, Min(0f)] private float hoverBobHeight = 0.25f;
        [SerializeField, Min(0f)] private float hoverBobSpeed = 2.2f;

        [Header("Fireball")]
        [SerializeField, Range(0f, 1f)] private float launchTime = 0.4f;
        [SerializeField, Min(0.1f)] private float fireballSpeed = 22f;
        [SerializeField] private GameObject explosionEffect;
        [SerializeField] private AudioClip launchSound;
        [SerializeField, Range(0f, 3f)] private float launchVolume = 1.2f;
        [SerializeField, Range(0f, 3f)] private float impactVolume = 2f;

        [Header("Landing")]
        [SerializeField, Range(0f, 1f)] private float touchdownTime = 0.55f;
        [SerializeField] private GameObject landingEffect;
        [SerializeField] private AudioClip landingSound;
        [SerializeField, Range(0f, 3f)] private float landingVolume = 2f;
        [SerializeField, Range(0f, 1f)] private float landingDuckDepth = 0.35f;

        [Header("Wings")]
        [SerializeField] private SoundCue wingFlap = new SoundCue();
        [SerializeField, Min(0.05f)] private float flapInterval = 0.55f;
        [SerializeField, Min(0f)] private float flapMinAltitude = 0.4f;

        private const float StageTimeout = 5f;
        private const float HoverTolerance = 1f;

        private Transform model;
        private Vector3 modelRestPosition;
        private float altitude;
        private float nextFlapTime;

        public override void WarmUpEffects()
        {
            base.WarmUpEffects();
            EffectWarmup.Request(explosionEffect);
            EffectWarmup.Request(landingEffect);
        }

        private void LateUpdate()
        {
            if (Caster == null)
            {
                return;
            }

            if (model == null)
            {
                model = Caster.DragonAnimator.Animator.transform;
                modelRestPosition = model.localPosition;
            }

            float target = TargetAltitude();
            if (target <= 0f && altitude <= 0.001f)
            {
                if (altitude != 0f)
                {
                    altitude = 0f;
                    model.localPosition = modelRestPosition;
                }
                return;
            }

            altitude = Mathf.Lerp(altitude, target, 1f - Mathf.Exp(-altitudeSharpness * Time.deltaTime));
            if (Caster.DragonAnimator.IsInState(DragonAnimator.LandStateHash))
            {
                altitude = Mathf.Min(altitude, target);
            }
            model.localPosition = modelRestPosition + model.parent.InverseTransformVector(Vector3.up * altitude);

            if (altitude >= flapMinAltitude && Time.time >= nextFlapTime)
            {
                nextFlapTime = Time.time + flapInterval;
                wingFlap.Play(transform.position);
            }
        }

        private float TargetAltitude()
        {
            if (!IsAlive)
            {
                return 0f;
            }

            DragonAnimator dragonAnimator = Caster.DragonAnimator;
            float bob = Mathf.Sin(Time.time * hoverBobSpeed * Mathf.PI * 2f) * hoverBobHeight;

            if (dragonAnimator.TryGetStateTime(DragonAnimator.FlyShootStateHash, out _)
                || dragonAnimator.TryGetStateTime(DragonAnimator.FlyStateHash, out _))
            {
                return extraAltitude + bob;
            }
            if (dragonAnimator.TryGetStateTime(DragonAnimator.TakeOffStateHash, out float takeOff))
            {
                return extraAltitude * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(liftOffTime * 0.5f, 1f, takeOff));
            }
            if (dragonAnimator.TryGetStateTime(DragonAnimator.LandStateHash, out float land))
            {
                return extraAltitude * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, touchdownTime * 0.85f, land)));
            }
            return 0f;
        }

        protected override IEnumerator Perform()
        {
            Health target = FindClosestTarget(Data.Range);
            DragonAnimator dragonAnimator = Caster.DragonAnimator;

            try
            {
                dragonAnimator.Trigger(DragonAnimator.TakeOffHash);
                yield return SteerUntil(target, () => dragonAnimator.IsInState(DragonAnimator.FlyStateHash), StageTimeout);
                yield return SteerUntil(target, () => IsAtHoverPoint(target), maxChaseTime);
                if (!IsAlive)
                {
                    yield break;
                }

                dragonAnimator.Trigger(DragonAnimator.FlyShootHash);
                yield return SteerUntil(target, () => HasReached(DragonAnimator.FlyShootStateHash, launchTime), StageTimeout);
                if (!IsAlive)
                {
                    yield break;
                }

                StartCoroutine(LaunchFireball(Caster.MouthPosition, AimPoint(target)));
                yield return SteerUntil(target, () => dragonAnimator.IsInState(DragonAnimator.FlyStateHash), StageTimeout);
                if (!IsAlive)
                {
                    yield break;
                }

                dragonAnimator.Trigger(DragonAnimator.LandHash);
                Caster.Motor.Steer(Vector3.zero, transform.forward);
                yield return WaitForStateTime(DragonAnimator.LandStateHash, touchdownTime);
                if (!IsAlive)
                {
                    yield break;
                }

                EffectSpawner.Spawn(landingEffect, transform.position, Quaternion.identity, Data.EffectLifetime);
                Caster.PlaySound(landingSound, landingVolume);
                SoundPlayer.Duck(landingDuckDepth, 0.2f, 0.8f);
                yield return WaitForLocomotion();
            }
            finally
            {
                Caster.Motor.StopSteering();
            }
        }

        private IEnumerator SteerUntil(Health target, Func<bool> isDone, float timeout)
        {
            float deadline = Time.time + timeout;
            while (IsAlive && !isDone() && Time.time < deadline)
            {
                Steer(target);
                yield return null;
            }
        }

        private void Steer(Health target)
        {
            Vector3 facing = transform.forward;
            Vector3 velocity = Vector3.zero;

            if (target != null && !target.IsDead)
            {
                Vector3 toTarget = Flatten(target.transform.position - transform.position);
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    facing = toTarget.normalized;
                }
                if (IsAirborne)
                {
                    velocity = Vector3.ClampMagnitude(ToHoverPoint(target) * arrivalSharpness, flySpeed);
                }
            }

            Caster.Motor.Steer(velocity, facing);
        }

        private bool IsAirborne =>
            !Caster.DragonAnimator.TryGetStateTime(DragonAnimator.TakeOffStateHash, out float time) || time >= liftOffTime;

        private bool IsAtHoverPoint(Health target)
        {
            return target == null || target.IsDead || ToHoverPoint(target).sqrMagnitude <= HoverTolerance * HoverTolerance;
        }

        private bool HasReached(int stateHash, float normalizedTime)
        {
            return Caster.DragonAnimator.TryGetStateTime(stateHash, out float time) && time >= normalizedTime;
        }

        private Vector3 ToHoverPoint(Health target)
        {
            Vector3 fromTarget = Flatten(transform.position - target.transform.position);
            Vector3 away = fromTarget.sqrMagnitude > 0.01f ? fromTarget.normalized : -transform.forward;
            return Flatten(target.transform.position + away * hoverDistance - transform.position);
        }

        private Vector3 AimPoint(Health target)
        {
            return target != null && !target.IsDead
                ? target.transform.position
                : transform.position + transform.forward * hoverDistance;
        }

        private IEnumerator LaunchFireball(Vector3 start, Vector3 end)
        {
            Caster.PlaySound(launchSound, launchVolume);
            float duration = Vector3.Distance(start, end) / fireballSpeed;
            GameObject fireball = EffectSpawner.Spawn(Data.CastEffect, start, Quaternion.LookRotation(end - start), duration + Data.EffectLifetime);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                if (fireball != null)
                {
                    fireball.transform.position = Vector3.Lerp(start, end, elapsed / duration);
                }
                yield return null;
            }

            EffectSpawner.Release(fireball, 0.5f);
            EffectSpawner.Spawn(explosionEffect, end, Quaternion.identity, Data.EffectLifetime);
            Caster.PlaySoundAt(Data.ImpactSound, end, impactVolume);

            foreach (Health target in CollectTargets(end, Data.Radius))
            {
                ApplyHit(target, end, false);
            }
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }
    }
}
